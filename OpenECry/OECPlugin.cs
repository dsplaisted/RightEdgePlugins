using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using System.Windows.Forms;
using System.Diagnostics;

using RightEdge.Common;

namespace OpenECry
{
	// Historical data is supported by OEC, but not really.  See the thread below.
	// http://www.openecry.com/myaccountmgm/cfbb/index.cfm?page=topic&topicID=106
	public class OECPlugin : IService, ITickRetrieval, /*IBarDataRetrieval,*/ IBroker
	{
		private string serverAddress = "api.openecry.com";
		//private string serverAddress = "prod.openecry.com";

		private int port = 9200;
		private string userName = "";
		private string password = "";
		private OEC.API.OECClient oecClient;
		private Dictionary<Symbol, bool> watchedSymbols = new Dictionary<Symbol, bool>();

		private Dictionary<Symbol, OEC.API.Contract> knownSymbols = new Dictionary<Symbol, OEC.API.Contract>();
		//	The pending orders are orders which were submitted, but the symbol wasn't being watched.  Once the
		//	OnContractsChanged is called and we have the actual contract, then they will be submitted
		private List<BrokerOrder> _pendingOrders = new List<BrokerOrder>();
		GotTickData tickListener = null;
		string lastError = "";
		private bool connected = false;
		private bool hadError = false;
		private bool watching = false;
		private bool connectCompleted = false;
		private bool bGettingHistData = false;

		#region IService Members

		public string ServiceName()
		{
			return "Open E-Cry";
		}

		public string Author()
		{
			return "Yye Software";
		}

		public string Description()
		{
			return "Retrieves real time data from Open E-Cry.  For Open E-Cry subscribers only.";
		}

		public string CompanyName()
		{
			return "Yye Software";
		}

		public string Version()
		{
			return "1.0";
		}

		public string id()
		{
			return "{76551084-E3CD-409b-9BEE-9645CF0A428F}";
		}

		public bool NeedsServerAddress()
		{
			return true;
		}

		public bool NeedsPort()
		{
			return true;
		}

		public bool NeedsAuthentication()
		{
			return true;
		}

		public bool SupportsMultipleInstances()
		{
			return false;
		}

		public string ServerAddress
		{
			get
			{
				return serverAddress;
			}
			set
			{
				serverAddress = value;
			}
		}

		public int Port
		{
			get
			{
				return port;
			}
			set
			{
				port = value;
			}
		}

		public string UserName
		{
			get
			{
				return userName;
			}
			set
			{
				userName = value;
			}
		}

		public string Password
		{
			get
			{
				return password;
			}
			set
			{
				password = value;
			}
		}

		public bool BarDataAvailable
		{
			get
			{
				//return true;
				return false;
			}
		}

		public bool TickDataAvailable
		{
			get
			{
				return true;
			}
		}

		public bool BrokerFunctionsAvailable
		{
			get
			{
				//return false;
				return true;
			}
		}

		public IBarDataRetrieval GetBarDataInterface()
		{
			return null;
			//return this;
		}

		public ITickRetrieval GetTickDataInterface()
		{
			return this;
		}

		public IBroker GetBrokerInterface()
		{
			//return null;
			return this;
		}

		public bool Initialize(SerializableDictionary<string, string> settings)
		{
			return true;
		}

		public bool HasCustomSettings()
		{
			return false;
		}

		public bool ShowCustomSettingsForm(ref SerializableDictionary<string, string> settings)
		{
			return true;
		}

		public bool Connect(ServiceConnectOptions connectOptions)
		{
			ClearError();

			if (!CreateControl())
				return false;

			if (connected)
				return true;

			foreach (Symbol symbol in new List<Symbol>(watchedSymbols.Keys))
			{
				watchedSymbols[symbol] = false;
			}
			foreach (Symbol symbol in new List<Symbol>(knownSymbols.Keys))
			{
				knownSymbols[symbol] = null;
			}

			try
			{
				connectCompleted = false;
				oecClient.Connect(serverAddress, port, userName, password, false);
				for (int index = 0; index < 10000; index += 100)
				{
					Thread.Sleep(100);
					Application.DoEvents();

					if (connectCompleted)
					{
						break;
					}
				}
			}
			catch (Exception e)
			{
				lastError = e.ToString();
				return false;
			}

			return CheckError();
		}

		public bool Disconnect()
		{
			ClearError();
			if (connected)
			{
				oecClient.Disconnect();
			}

			return true;
		}

		public string GetError()
		{
			return lastError;
		}

		#endregion

		private bool CreateControl()
		{
			if (oecClient == null)
			{
				oecClient = new OEC.API.OECClient();
				oecClient.OnAccountSummaryChanged += new OEC.API.OnAccountSummaryChangedEvent(oecClient_OnAccountSummaryChanged);
				oecClient.OnBalanceChanged += new OEC.API.OnBalanceChangedEvent(oecClient_OnBalanceChanged);
				oecClient.OnBarsReceived += new OEC.API.OnBarsReceivedEvent(oecClient_OnBarsReceived2);
				oecClient.OnDisconnected += new OEC.API.OnDisconnectedEvent(oecClient_OnDisconnected);
				oecClient.OnError += new OEC.API.OnErrorEvent(oecClient_OnError);
				oecClient.OnHistoryReceived += new OEC.API.OnHistoryReceivedEvent(oecClient_OnHistoryReceived);
				oecClient.OnLoginComplete += new OEC.API.OnLoginCompleteEvent(oecClient_OnLoginComplete);
				oecClient.OnLoginFailed += new OEC.API.OnLoginFailedEvent(oecClient_OnLoginFailed);
				oecClient.OnOrderConfirmed += new OEC.API.OnOrderConfirmedEvent(oecClient_OnOrderConfirmed);
				oecClient.OnOrderFilled += new OEC.API.OnOrderFilledEvent(oecClient_OnOrderFilled);
				oecClient.OnOrderStateChanged += new OEC.API.OnOrderStateChangedEvent(oecClient_OnOrderStateChanged);
				oecClient.OnPriceChanged += new OEC.API.OnPriceChangedEvent(oecClient_OnPriceChanged);
				oecClient.OnPriceTick += new OEC.API.OnPriceChangedEvent(oecClient_OnPriceTick);
				oecClient.OnTicksReceived += new OEC.API.OnTicksReceivedEvent(oecClient_OnTicksReceived);
				oecClient.OnContractsChanged += new OEC.API.OnContractsChangedEvent(oecClient_OnContractsChanged);
			}

			return true;
		}

		#region Open E-Cry event handlers
		void oecClient_OnDisconnected(bool Unexpected)
		{
			watching = false;
			connected = false;
		}

		void oecClient_OnTicksReceived(OEC.API.Subscription Subscription, OEC.API.Ticks Ticks)
		{
		}

		void oecClient_OnPriceTick(OEC.API.Contract Contract, OEC.API.Price Price)
		{
			//Symbol symbol = GetSymbolFromContract(Contract);
			//if (symbol != null)
			//{
			//    Trace.WriteLine(Contract.ToString() + " Tick (" + symbol.ToString() + ")");
			//}
			//else
			//{
			//    Trace.WriteLine(Contract.ToString() + " Tick (unwatched)");
			//}
		}

		private class SymbolPrice
		{
			public Symbol symbol;
			public int TotalVol;
			public double LastPrice;
			public double BidPrice;
			public int BidSize;
			public double AskPrice;
			public int AskSize;
			public double HighPrice;
			public double LowPrice;
		}

		private Dictionary<Symbol, SymbolPrice> _symbolPrices = new Dictionary<Symbol, SymbolPrice>();

		void oecClient_OnPriceChanged(OEC.API.Contract Contract, OEC.API.Price Price)
		{
			Symbol symbol = GetSymbolFromContract(Contract);

			if (symbol != null)
			{
				Trace.WriteLine(Contract.ToString() + " Price (" + symbol.ToString() + ")");

				SymbolPrice symbolPrice = null;
				if (!_symbolPrices.TryGetValue(symbol, out symbolPrice))
				{
					symbolPrice = new SymbolPrice();
					symbolPrice.symbol = symbol;
					symbolPrice.TotalVol = Price.TotalVol;
					_symbolPrices[symbol] = symbolPrice;
				}

				TickData data;

				if (symbolPrice.AskPrice != Price.AskPrice || symbolPrice.AskSize != Price.AskVol)
				{
					symbolPrice.AskPrice = Price.AskPrice;
					symbolPrice.AskSize = Price.AskVol;

					data = new TickData();
					data.time = Price.LastDateTime;
					data.tickType = TickType.Ask;
					data.price = Price.AskPrice;
					data.size = (UInt64)Price.AskVol;

					if (tickListener != null)
					{
						tickListener(symbol, data);
					}
				}

				if (symbolPrice.BidPrice != Price.BidPrice || symbolPrice.BidSize != Price.BidVol)
				{
					symbolPrice.BidPrice = Price.BidPrice;
					symbolPrice.BidSize = Price.BidVol;

					data = new TickData();
					data.time = Price.LastDateTime;
					data.tickType = TickType.Bid;
					data.price = Price.BidPrice;
					data.size = (UInt64)Price.BidVol;

					if (tickListener != null)
					{
						tickListener(symbol, data);
					}
				}

				if (symbolPrice.TotalVol != Price.TotalVol || symbolPrice.LastPrice != Price.LastPrice)
				{
					data = new TickData();
					data.time = Price.LastDateTime;
					data.tickType = TickType.Trade;
					data.price = Price.LastPrice;
					if (symbolPrice.TotalVol > Price.TotalVol)
					{
						data.size = 0;
					}
					else
					{
						data.size = (UInt64)(Price.TotalVol - symbolPrice.TotalVol);
					}

					symbolPrice.LastPrice = Price.LastPrice;
					symbolPrice.TotalVol = Price.TotalVol;

					if (tickListener != null)
					{
						tickListener(symbol, data);
					}
				}

				//data = new TickData();
				//data.time = DateTime.Now;
				//data.tickType = TickType.LastSize;
				//data.value = Price.LastVol;

				//if (tickListener != null)
				//{
				//    tickListener(symbol, data);
				//}

				if (symbolPrice.LowPrice != Price.LowPrice)
				{
					symbolPrice.LowPrice = Price.LowPrice;

					data = new TickData();
					data.time = Price.LastDateTime;
					data.tickType = TickType.LowPrice;
					data.price = Price.LowPrice;

					if (tickListener != null)
					{
						tickListener(symbol, data);
					}
				}

				if (symbolPrice.HighPrice != Price.HighPrice)
				{
					symbolPrice.HighPrice = Price.HighPrice;

					data = new TickData();
					data.time = Price.LastDateTime;
					data.tickType = TickType.HighPrice;
					data.price = Price.HighPrice;

					if (tickListener != null)
					{
						tickListener(symbol, data);
					}
				}

				//data = new TickData();
				//data.time = DateTime.Now;
				//data.tickType = TickType.OpenPrice;
				//data.value = Price.OpenPrice;

				//if (tickListener != null)
				//{
				//    tickListener(symbol, data);
				//}

				//data = new TickData();
				//data.time = DateTime.Now;
				//data.tickType = TickType.AskSize;
				//data.value = Price.AskVol;

				//if (tickListener != null)
				//{
				//    tickListener(symbol, data);
				//}

				//data = new TickData();
				//data.time = DateTime.Now;
				//data.tickType = TickType.BidSize;
				//data.value = Price.BidVol;

				//if (tickListener != null)
				//{
				//    tickListener(symbol, data);
				//}

				//data = new TickData();
				//data.time = DateTime.Now;
				//data.tickType = TickType.Change;
				//data.value = Price.Change;

				//if (tickListener != null)
				//{
				//    tickListener(symbol, data);
				//}
			}
			else
			{
				//Trace.WriteLine(Contract.ToString() + " Price (unwatched)");
			}
		}

		void oecClient_OnLoginFailed(OEC.Data.FailReason Reason)
		{
			string error = "Login Failed: " + Reason.ToString();
			lastError = error;
			hadError = true;
			connectCompleted = true;
			connected = false;
			//DisplayError(Reason.ToString());
		}

		void oecClient_OnLoginComplete()
		{
			connectCompleted = true;
			connected = true;
		}

		void oecClient_OnError(Exception ex)
		{
			string error = "OEC Error: " + ex.ToString() + "\r\n" + ex.StackTrace;
			Trace.WriteLine(error);
			if (bGettingHistData)
			{
				lastError = error;
				//histDoneHandle.Set();
				Application.ExitThread();
			}
			else
			{
				//MessageBox.Show(error);
			}

			//DisplayError(ex.Message);
		}

		void oecClient_OnBalanceChanged(OEC.API.Account Account, OEC.API.Currency Currency)
		{
		}

		void oecClient_OnAccountSummaryChanged(OEC.API.Account Account, OEC.API.Currency Currency)
		{
		}

		#endregion

		private Symbol GetSymbolFromContract(OEC.API.Contract contract)
		{
			foreach (Symbol symbol in knownSymbols.Keys)
			{
				if (symbol.Name == contract.BaseSymbol &&
					//symbol.Exchange == contract.Exchange.Name &&
					symbol.ExpirationDate.Month == contract.ExpirationDate.Month &&
					symbol.ExpirationDate.Year == contract.ExpirationDate.Year)
				{
					return symbol;
				}
			}

			return null;
		}

		private OEC.API.Contract GetContractFromSymbol(Symbol symbol)
		{
			OEC.API.Contract found = null;

			if (connected)
			{
				foreach (OEC.API.Contract contract in oecClient.Contracts)
				{
					//if (contract.Exchange.Name != "CME")
					//{
					//    Trace.WriteLine("Contract: " + contract.ToString() + " Exchange: " + contract.Exchange.ToString());
					//}

					if (symbol.Name == contract.BaseSymbol &&
						symbol.ExpirationDate.Month == contract.ExpirationDate.Month &&
						symbol.ExpirationDate.Year == contract.ExpirationDate.Year)
					{
						if (symbol.Exchange == contract.Exchange.Name || true)
						{
							found = contract;
							break;
						}
					}
				}
			}

			return found;
		}

		


		//private void DisplayError(string errorText)
		//{
		//    lastError = errorText;
		//    hadError = true;
		//    MessageBox.Show(errorText);
		//}

		private void ClearError()
		{
			lastError = "";
			hadError = false;
		}

		private bool CheckError()
		{
			return !hadError;
		}

		#region IDisposable Members

		public void Dispose()
		{
			Disconnect();
			if (oecClient != null)
			{
				oecClient.Dispose();
				oecClient = null;
			}
			//throw new Exception("The method or operation is not implemented.");
		}

		#endregion

		#region ITickRetrieval Members

		public bool RealTimeDataAvailable
		{
			get
			{
				return true;
			}
		}

		public GotTickData TickListener
		{
			set
			{
				tickListener = value;
			}
		}

		public bool SetWatchedSymbols(List<Symbol> symbols)
		{
			foreach (Symbol symbol in symbols)
			{
				if (!knownSymbols.ContainsKey(symbol))
				{
					knownSymbols[symbol] = null;
				}
				if (!watchedSymbols.ContainsKey(symbol))
				{
					watchedSymbols[symbol] = false;
				}
			}
			foreach (Symbol symbol in new List<Symbol>(watchedSymbols.Keys))
			{
				if (!symbols.Contains(symbol))
				{
					if (watching)
					{
						OEC.API.Contract contract = GetContractFromSymbol(symbol);

						if (contract != null)
						{
							oecClient.Unsubscribe(contract);
						}
					}
					watchedSymbols.Remove(symbol);
					
					if (_symbolPrices.ContainsKey(symbol))
					{
						_symbolPrices.Remove(symbol);
					}
				}
			}

			//	Check error here because StartWatching() will clear error status
			if (!CheckError())
			{
				return false;
			}

			if (watching)
			{
				StartWatching();
			}
			return CheckError();
		}

		public bool IsWatching()
		{
			return watching;
		}

		public bool StartWatching()
		{
			ClearError();
			foreach (Symbol symbol in new List<Symbol>(knownSymbols.Keys))
			{
				if (knownSymbols[symbol] == null)
				{
					foreach (OEC.API.BaseContract contract in oecClient.BaseContracts)
					{
						if (symbol.Name == contract.Symbol)
						{
							oecClient.RequestContracts(contract);
						}
					}
					//oecClient.RequestContracts(oecClient.BaseContracts[symbol.Name]);

					//OEC.API.Contract contract = GetContractFromSymbol(symbol);

					//if (contract != null)
					//{
					//    oecClient.Subscribe(contract);
					//}
				}
				else if (watchedSymbols.ContainsKey(symbol) && watchedSymbols[symbol] == false)
				{
					oecClient.Subscribe(knownSymbols[symbol]);
					watchedSymbols[symbol] = true;
				}
			}
			//foreach (OEC.API.Contract contract in oecClient.Contracts)
			//{
			//    if (contract.CurrentPrice == null)
			//    {
			//        oecClient.Subscribe(contract);
			//    }
			//}

			watching = true;

			return true;
		}

		void oecClient_OnContractsChanged(OEC.API.BaseContract bc)
		{
			bool foundSymbol = false;
			foreach (Symbol symbol in new List<Symbol>(knownSymbols.Keys))
			{
				if (knownSymbols[symbol] != null)
				{
					//	Already got contract for this symbol
					continue;
				}
				foreach (OEC.API.Contract contract in bc.ContractGroup.Contracts)
				{
					if (symbol.Name == contract.BaseSymbol &&
						symbol.ExpirationDate.Month == contract.ExpirationDate.Month &&
						symbol.ExpirationDate.Year == contract.ExpirationDate.Year)
					{
						if (symbol.Exchange == contract.Exchange.Name || true)
						{
							//Console.WriteLine("Got contract for " + symbol);
							knownSymbols[symbol] = contract;
							foundSymbol = true;
							if (watchedSymbols.ContainsKey(symbol))
							{
								oecClient.Subscribe(contract);
								watchedSymbols[symbol] = true;
							}
						}
					}
				}
			}
			if (foundSymbol)
			{
				ProcessPendingOrders();
			}
		}

		public bool StopWatching()
		{
			ClearError();

			foreach (Symbol symbol in new List<Symbol>(watchedSymbols.Keys))
			{
				if (watchedSymbols[symbol] == true)
				{
					OEC.API.Contract contract = knownSymbols[symbol];

					if (contract != null)
					{
						oecClient.Unsubscribe(contract);
						watchedSymbols[symbol] = false;
					}

					if (_symbolPrices.ContainsKey(symbol))
					{
						_symbolPrices.Remove(symbol);
					}
				}
			}

			watching = false;

			return true;
		}

		public IService GetService()
		{
			return this;
		}

		#endregion

		#region IBarDataRetrieval Members

		public List<int> GetAvailableFrequencies()
		{
			List<int> ret = new List<int>();
			ret.Add((int)BarFrequency.OneMinute);
			ret.Add((int)BarFrequency.FiveMinute);
			ret.Add((int)BarFrequency.FifteenMinute);
			ret.Add((int)BarFrequency.ThirtyMinute);
			ret.Add((int)BarFrequency.SixtyMinute);
			ret.Add((int)BarFrequency.Daily);
			ret.Add((int)BarFrequency.Weekly);
			ret.Add((int)BarFrequency.Monthly);
			ret.Add((int)BarFrequency.Yearly);
			return ret;
		}

		private EventWaitHandle histDoneHandle = new EventWaitHandle(true, EventResetMode.ManualReset);
		private List<BarData> histBarData = null;
		public List<BarData> RetrieveData(Symbol symbol, int frequency, AssetClass assetClass, double? strikePrice, DateTime? expirationDate, ContractType? contract, DateTime startDate, DateTime endDate)
		{
			ClearError();
			bool bWasConnected = connected;
			try
			{
				if (!connected)
				{
					if (!Connect(ServiceConnectOptions.HistoricalData))
						return null;
				}

				TimeSpan interval = new TimeSpan(0, frequency, 0);

				OEC.API.Contract oecContract = GetContractFromSymbol(symbol);
				if (oecContract == null)
				{
					lastError = "Historical data is not available for the specified symbol: " + symbol;
					return null;
				}

				histDoneHandle.Reset();
				bGettingHistData = true;

				if (endDate > DateTime.Today)
				{
					endDate = DateTime.Today;
				}

				Trace.WriteLine("Requesting OEC historical data...");
				oecClient.RequestBars(oecContract, startDate, endDate, interval);
				//oecClient.RequestHistory(oecContract, startDate, endDate);

				Application.Run();
				//if (!histDoneHandle.WaitOne(new TimeSpan(0, 1, 0), true))
				//{
				//    lastError = "Timed out.";
				//    return null;
				//}

				if (!CheckError())
				{
					return null;
				}

				List<BarData> ret = histBarData;
				histBarData = null;
				return ret;
			}
			finally
			{
				bGettingHistData = false;
				if (!bWasConnected && connected)
				{
					string tempError = lastError;
					Disconnect();
					if (CheckError())
					{
						lastError = tempError;
					}
				}
			}
		}

		#endregion

		void oecClient_OnBarsReceived2(OEC.API.Subscription Subscription, OEC.API.Bar[] Bars)
		{
			OEC.API.Contract Contract = Subscription.Contract;
			//TimeSpan Interval = Subscription.Interval;
		//}


		//void oecClient_OnBarsReceived(OEC.API.Contract Contract, TimeSpan Interval, OEC.API.Bar[] Bars)
		//{
			Trace.WriteLine("" + Bars.Length + " OEC bars received.");
			histBarData = new List<BarData>(Bars.Length);

			foreach (OEC.API.Bar oecBar in Bars)
			{
				BarData bar = new BarData(oecBar.Timestamp, oecBar.Open, oecBar.Close, oecBar.High, oecBar.Low);
				bar.Volume = (ulong) oecBar.Volume;
				histBarData.Add(bar);
			}

			//histDoneHandle.Set();
			Application.ExitThread();


		}

		//void oecClient_OnHistoryReceived(OEC.API.Contract Contract, OEC.API.Bar[] Bars)
		void oecClient_OnHistoryReceived(OEC.API.Subscription Subscription, OEC.API.Bar[] Bars)
		{
			oecClient_OnBarsReceived2(Subscription, Bars);
		}

		private void VerifyConnected()
		{
			if (!connected)
			{
				throw new RightEdgeError("Not connected.");
			}
		}

		//	Map from OEC order ID to RightEdge orderID
		private Dictionary<int, string> _orderIDMap = new Dictionary<int, string>();

		private Dictionary<string, BrokerOrder> _openOrderMap = new Dictionary<string, BrokerOrder>();

		private event OrderUpdatedDelegate _orderUpdated;

		#region IBroker Members

		public void SetAccountState(BrokerAccountState state)
		{

		}

		public bool SubmitOrder(BrokerOrder order, out string orderId)
		{
			VerifyConnected();

			order.OrderId = Guid.NewGuid().ToString();
			orderId = order.OrderId;

			//	Make sure this symbol is being watched
			if (!knownSymbols.ContainsKey(order.OrderSymbol))
			{
				//Console.WriteLine("Looking up contract for " + order.OrderSymbol.ToString());
				knownSymbols[order.OrderSymbol] = null;
				StartWatching();

				_pendingOrders.Add(order);
				_openOrderMap[order.OrderId] = order;
				return true;
			}

			ReturnCode ret = InternalSubmitOrder(order);
			if (!ret.Success)
			{
				lastError = ret.Message;
				orderId = null;
				return false;
			}
			_openOrderMap[order.OrderId] = order;
			return true;
		}

		private void ProcessPendingOrders()
		{
			foreach (BrokerOrder order in new List<BrokerOrder>(_pendingOrders))
			{
				if (knownSymbols[order.OrderSymbol] != null)
				{
					//Console.WriteLine("Processing pending order: " + order.ToString());
					_pendingOrders.Remove(order);
					ReturnCode ret = InternalSubmitOrder(order);
					if (!ret.Success)
					{
						string msg = "Rejected: " + ret.Message;
						Trace.WriteLine(msg);
						//Console.WriteLine(msg);
						order.OrderState = BrokerOrderState.Rejected;
						OnOrderUpdated(order, null, msg);
					}
				}
			}
		}

		private ReturnCode InternalSubmitOrder(BrokerOrder order)
		{
			OEC.API.OrderDraft draft = oecClient.CreateDraft();
			draft.Account = oecClient.Accounts.First;
			if (order.TransactionType == TransactionType.Buy || order.TransactionType == TransactionType.Cover)
			{
				draft.Side = OEC.Data.OrderSide.Buy;
			}
			else if (order.TransactionType == TransactionType.Sell || order.TransactionType == TransactionType.Short)
			{
				draft.Side = OEC.Data.OrderSide.Sell;
			}
			else
			{
				throw new RightEdgeError("Transaction type " + order.TransactionType.ToString() + " not supported by broker.");
			}
			draft.Quantity = (int)order.Shares;
			draft.Contract = GetContractFromSymbol(order.OrderSymbol);
			if (order.OrderType == OrderType.Market)
			{
				draft.Type = OEC.Data.OrderType.Market;
			}
			else if (order.OrderType == OrderType.MarketOnClose)
			{
				draft.Type = OEC.Data.OrderType.MarketOnClose;
			}
			else if (order.OrderType == OrderType.MarketOnOpen)
			{
				draft.Type = OEC.Data.OrderType.MarketOnOpen;
			}
			else if (order.OrderType == OrderType.Limit)
			{
				draft.Type = OEC.Data.OrderType.Limit;
				draft.Price = order.LimitPrice;
			}
			else if (order.OrderType == OrderType.Stop)
			{
				draft.Type = OEC.Data.OrderType.Stop;
				draft.Price = order.StopPrice;
			}
			else if (order.OrderType == OrderType.StopLimit)
			{
				draft.Type = OEC.Data.OrderType.StopLimit;
				draft.Price = order.StopPrice;
				draft.Price2 = order.LimitPrice;
			}
			//else if (order.OrderType == OrderType.TrailingStop)
			//{
			//    draft.Type = OEC.Data.OrderType.TrailingStopLimit;
			//}
			else
			{
				//throw new RightEdgeError("Order type " + order.OrderType.ToString() + " not supported by broker.");
				return ReturnCode.Fail("Order type " + order.OrderType.ToString() + " not supported by broker.");
			}

			if (order.GoodTillCanceled)
			{
				draft.Flags |= OEC.Data.OrderFlags.GTC;
			}

			//draft.Flags |= OEC.Data.OrderFlags.AON;

			OEC.API.OrderParts res = draft.GetInvalidParts();
			if (res != OEC.API.OrderParts.None)
			{
				string msg = "Invalid order: " + res.ToString();
				Trace.WriteLine(msg);
				//orderId = null;
				return ReturnCode.Fail(msg);
			}


			OEC.API.Order oecOrder = oecClient.SendOrder(draft);

			_orderIDMap[oecOrder.ID] = order.OrderId;

			order.OrderState = BrokerOrderState.Submitted;
			order.SubmittedDate = DateTime.Now;

			return ReturnCode.Succeed;
		}

		public bool CancelOrder(string orderId)
		{
			VerifyConnected();

			int? oecID = GetOECOrder(orderId);
			if (!oecID.HasValue)
			{
				if (_openOrderMap.ContainsKey(orderId))
				{
					BrokerOrder order = _openOrderMap[orderId];
					order.OrderState = BrokerOrderState.Cancelled;
					_pendingOrders.Remove(order);
					_openOrderMap.Remove(orderId);
					OnOrderUpdated(order, null, "Cancelled");
					return true;
				}

				lastError = "Could not find order with the specified ID.";
				return false;
			}

			oecClient.CancelOrder(oecClient.Orders[oecID.Value]);
			_openOrderMap.Remove(orderId);

			return true;
		}

		public bool CancelAllOrders()
		{
			foreach (BrokerOrder order in _openOrderMap.Values)
			{
				if (!CancelOrder(order.OrderId))
				{
					return false;
				}
			}
			return true;
		}

		public double GetBuyingPower()
		{
			VerifyConnected();

			return oecClient.Accounts.First.TotalBalance.NetLiquidatingValue - oecClient.Accounts.First.TotalBalance.SettlePnL;
		}

		public double GetMargin()
		{
			return 0.0;
		}

		public double GetShortedCash()
		{
			return 0.0;
		}

		//public void SetBuyingPower(double value)
		//{
		//    throw new Exception("The method or operation is not implemented.");
		//}

		public List<BrokerOrder> GetOpenOrders()
		{
			return new List<BrokerOrder>(_openOrderMap.Values);
		}

		public BrokerOrder GetOpenOrder(string id)
		{
			return _openOrderMap[id];
		}

		public int GetShares(Symbol symbol)
		{
			VerifyConnected();

			OEC.API.Contract contract = GetContractFromSymbol(symbol);
			if (contract == null)
			{
				return 0;
			}

			return oecClient.Accounts.First.AvgPositions[contract].Net.Volume;
		}

		public void AddOrderUpdatedDelegate(OrderUpdatedDelegate orderUpdated)
		{
			_orderUpdated += orderUpdated;
		}

		public void RemoveOrderUpdatedDelegate(OrderUpdatedDelegate orderUpdated)
		{
			_orderUpdated -= orderUpdated;
		}

		public void AddPositionAvailableDelegate(PositionAvailableDelegate positionAvailable)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void RemovePositionAvailableDelegate(PositionAvailableDelegate positionAvailable)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public bool IsLiveBroker()
		{
			return true;
		}

		//public void SimBar(Dictionary<Symbol, BarData> bars)
		//{
		//    throw new Exception("The method or operation is not implemented.");
		//}

		//public void SimTick(Symbol symbol, double price, DateTime tickTime)
		//{
		//    throw new Exception("The method or operation is not implemented.");
		//}

		#endregion

		void oecClient_OnOrderStateChanged(OEC.API.Order oecOrder, OEC.Data.OrderState OldOrderState)
		{
			string orderID;
			string information = "";

			if (!_orderIDMap.TryGetValue(oecOrder.ID, out orderID))
			{
				Trace.WriteLine("Unmapped order completed, ID " + oecOrder.ID);
				information = "Unmapped order completed, ID " + oecOrder.ID;
				return;
			}
			BrokerOrder order;
			if (!_openOrderMap.TryGetValue(orderID, out order))
			{
				Trace.WriteLine("Order completed but not found: " + orderID);
				information = "Order completed but not found: " + orderID;
				return;
			}

			bool bUpdated = false;
			if (oecOrder.CurrentState == OEC.Data.OrderState.Accepted)
			{
				//	Nothing to do
				Trace.WriteLine("OEC Accepted " + oecOrder.ID);
			}
			else if (oecOrder.CurrentState == OEC.Data.OrderState.Cancelled)
			{
				Trace.WriteLine("OEC Cancelled " + oecOrder.ID);
				information = "OEC Cancelled " + oecOrder.Comments;
				order.OrderState = BrokerOrderState.Cancelled;
				Console.WriteLine(information + " " + order.OrderId);
				bUpdated = true;
			}
			else if (oecOrder.CurrentState == OEC.Data.OrderState.Completed)
			{
				
				if (oecOrder.Fills.Count == 0)
				{
					//order.OrderState = BrokerOrderState.Rejected;
					Trace.WriteLine("OEC Completed (No Fills)" + oecOrder.ID);
				}
				else
				{
					Trace.WriteLine("OEC Completed (With Fills)" + oecOrder.ID);
					//order.OrderState = BrokerOrderState.Filled;
					//order.FillPrice = oecOrder.Fills.AvgPrice;
					//order.FillDate = oecOrder.Fills.Last.Timestamp;

					//order.Commission = 0.0;
					//foreach (OEC.API.Fill fill in oecOrder.Fills)
					//{
					//    order.Commission += fill.Commission;
					//}
					//bUpdated = true;
				}

				
			}
			else if (oecOrder.CurrentState == OEC.Data.OrderState.Held)
			{
				Trace.WriteLine("OEC Held " + oecOrder.ID);
			}
			else if (oecOrder.CurrentState == OEC.Data.OrderState.None)
			{
				//	???
				Trace.WriteLine("OEC None " + oecOrder.ID);
			}
			else if (oecOrder.CurrentState == OEC.Data.OrderState.Rejected)
			{
				Trace.WriteLine("OEC Rejected " + oecOrder.ID);
				order.OrderState = BrokerOrderState.Rejected;
				information = "OEC Rejected " + oecOrder.States.Current.Comments;
				bUpdated = true;
			}
			else if (oecOrder.CurrentState == OEC.Data.OrderState.Sent)
			{
				Trace.WriteLine("OEC Sent " + oecOrder.ID);
			}
			else if (oecOrder.CurrentState == OEC.Data.OrderState.Unknown)
			{
				//	???
				Trace.WriteLine("OEC Unknown " + oecOrder.ID);
				information = "OEC Unknown " + oecOrder.Comments;
			}
			else if (oecOrder.CurrentState == OEC.Data.OrderState.Working)
			{
				Trace.WriteLine("OEC Working " + oecOrder.ID);
				information = "OEC Working " + oecOrder.Comments;
			}
			else
			{
				Trace.WriteLine("OEC Other " + oecOrder.ID);
			}

			if (bUpdated)
			{
				OnOrderUpdated(order, null, information);
			}
		}

		private void OnOrderUpdated(BrokerOrder order, Fill fill, string information)
		{
			OrderUpdatedDelegate del = _orderUpdated;
			if (del != null)
			{
				del(order, fill, information);
			}
		}

		void oecClient_OnOrderFilled(OEC.API.Order oecOrder, OEC.API.Fill Fill)
		{
			string information = "";
			Trace.WriteLine("OEC Order Filled: " + oecOrder.ID + " " + oecOrder.CurrentState.ToString());

			string orderID;
			if (!_orderIDMap.TryGetValue(oecOrder.ID, out orderID))
			{
				Trace.WriteLine("Unmapped order filled, ID " + oecOrder.ID);
				information = "Unmapped order filled, ID " + oecOrder.ID;
				return;
			}
			BrokerOrder order;
			if (!_openOrderMap.TryGetValue(orderID, out order))
			{
				Trace.WriteLine("Order not found: " + orderID);
				information = "Order not found: " + orderID;
				return;
			}

			//	Apparently fills can be canceled, not sure how to handle this...

			Fill fill = new Fill();
			fill.FillDateTime = Fill.Timestamp;
			fill.Price = new Price(Fill.Price, Fill.Price);
			fill.Quantity = Fill.Quantity;
			fill.Commission = Fill.Commission;

			order.Fills.Add(fill);

			if (oecOrder.Fills.TotalQuantity == order.Shares)
			{
				order.OrderState = BrokerOrderState.Filled;
				//order.FillPrice = oecOrder.Fills.AvgPrice;
				//order.FillDate = oecOrder.Fills.Last.Timestamp;

				//order.Commission = 0.0;
				//foreach (OEC.API.Fill fill in oecOrder.Fills)
				//{
				//    order.Commission += fill.Commission;
				//}
			}
			else
			{
				order.OrderState = BrokerOrderState.PartiallyFilled;
			}

			OnOrderUpdated(order, fill, information);
		}

		void oecClient_OnOrderConfirmed(OEC.API.Order Order, int OldOrderID)
		{
			//	Order ID changes here
			Trace.WriteLine("Order confirmed: " + OldOrderID + " -> " + Order.ID);
			_orderIDMap[Order.ID] = _orderIDMap[OldOrderID];
			_orderIDMap.Remove(OldOrderID);
		}

		private int? GetOECOrder(string REOrderID)
		{
			foreach (KeyValuePair<int, string> kvp in _orderIDMap)
			{
				if (kvp.Value == REOrderID)
				{
					return kvp.Key;
				}
			}
			return null;
		}
		
	}
}
