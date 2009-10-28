//#define RE1_1

using System;
using System.Collections.Generic;
using System.Text;
using RightEdge.Common;
using Krs.Ats.IBNet;

using TickType = RightEdge.Common.TickType;
using KRSTickType = Krs.Ats.IBNet.TickType;
using System.Diagnostics;
using System.Threading;
using System.Globalization;
#if RE1_1
using TransactionType = RightEdge.Common.BrokerTransactionType;
#endif


namespace RightEdge.TWSCSharpPlugin
{
	public sealed class TWSPlugin : IService, ITickRetrieval, IBarDataRetrieval, IBroker
	{
		//public static EventHandler OutOfBandCallback;

		IBClient client;
		private object _lockObject = new object();

		private bool _connected = false;
		private bool hadError = false;
		private bool _watching = false;

		//private bool _firstHistRequest = true;
		
		private bool _useRTH = false;
		private string accountCode = "";
		private FinancialAdvisorAllocationMethod _FAMethod = FinancialAdvisorAllocationMethod.None;
		private string _FAPercentage = string.Empty;
		private string _FAProfile = string.Empty;
		private bool dropLastHistBar = false;

		private int _clientIDBroker = 1001;
		private int _clientIDLiveData = 1002;
		private int _clientIDHist = 1003;

		public int nextID = 0;
		int nextOrderId = -1;
		string lastError = "";

		//	Reconnection data
		EventWaitHandle _connectWaitHandle;
		private bool _gettingReconnectData = false;
		private Dictionary<string, object> _potentiallyCancelledOrders = new Dictionary<string, object>();
		//	When we reconnect shortly after getting disconnected, we don't seem to get the order status updates, but we do get
		//	an error 1102 *after* we get the account time update.  So we have to wait a little bit longer to see if we get an
		//	error 1102 to know if the orders are actually cancelled.
		private bool _hadError1102 = false;

		private Dictionary<Symbol, int?> watchedSymbols = new Dictionary<Symbol, int?>();
		GotTickData tickListener = null;

		//	We have to store this data because TWS sends us the price and size seperately
		private Dictionary<Symbol, double> lastPrices = new Dictionary<Symbol, double>();
		private Dictionary<Symbol, UInt64> lastVolumes = new Dictionary<Symbol, ulong>();
		private Dictionary<Symbol, double> lastBidPrices = new Dictionary<Symbol, double>();
		private Dictionary<Symbol, UInt64> lastBidSizes = new Dictionary<Symbol, ulong>();
		private Dictionary<Symbol, double> lastAskPrices = new Dictionary<Symbol, double>();
		private Dictionary<Symbol, UInt64> lastAskSizes = new Dictionary<Symbol, ulong>();
		//private Dictionary<Symbol, double> lastHigh = new Dictionary<Symbol, double>();
		//private Dictionary<Symbol, double> lastLow = new Dictionary<Symbol, double>();
		//private Dictionary<Symbol, double> lastClose = new Dictionary<Symbol, double>();
		
		//	We don't get the time with each tick, but we do get an account time update every so often
		//	So we store the difference here
		TimeSpan accountTimeDiff = new TimeSpan(0);
		bool bGotAccountTime = false;

		private double buyingPower = 0.0;
#if RE1_1
		private Dictionary<string, RightEdge.Common.Order> openOrders = new Dictionary<string, RightEdge.Common.Order>();
#else
		private Dictionary<string, RightEdge.Common.BrokerOrder> openOrders = new Dictionary<string, RightEdge.Common.BrokerOrder>();
#endif

		//	This dictionary keeps track of orders that have been "placed", but for which
		//	we have not received the "Submitted" order status.  If these orders are
		//	canceled, we will probably not receive any cancellation confirmation.
		//	(How this works is mostly guesswork)
		private Dictionary<string, bool> unSubmittedOrders = new Dictionary<string, bool>();

		//	Keep track of order IDs that were filled so if a cancellation is requested we don't error
		private FilledOrders _filledOrders = new FilledOrders();

		//private Dictionary<Symbol, ulong> sharesLong = new Dictionary<Symbol, ulong>();
		//private Dictionary<Symbol, ulong> sharesShort = new Dictionary<Symbol, ulong>();

		// These are shares that already exist at IB on startup.
		private Dictionary<Symbol, int> openShares = new Dictionary<Symbol, int>();

		public event OrderUpdatedDelegate OrderUpdated;
		//public event PositionUpdatedDelegate PositionUpdated;
		//public event AccountUpdatedDelegate AccountUpdated;
		public event PositionAvailableDelegate PositionAvailable;


		private HistRetrieval _histRetrieval = null;

		public TWSPlugin()
		{
			ServerAddress = "127.0.0.1";
			Port = 7496;
		}

		private void ClearError()
		{
			lastError = "";
			hadError = false;
		}

		private bool CheckError()
		{
			return !hadError;
		}

		private Symbol idToSymbol(int id)
		{
			foreach (var kvp in watchedSymbols)
			{
				if (kvp.Value == id)
				{
					return kvp.Key;
				}
			}
			//foreach (Symbol symbol in watchedSymbols.Keys)
			//{
			//    if (watchedSymbols[symbol] == id)
			//    {
			//        return symbol;
			//    }
			//}
			return null;
		}

		private DateTime GetAccountTime(string desc)
		{
			if (bGotAccountTime)
			{
				return DateTime.Now.Add(accountTimeDiff);
			}
			else
			{
				System.Diagnostics.Trace.WriteLine("Got " + desc + " before account time update.");
				return DateTime.Now;
			}
		}

		public static ulong AdjustVolume(Symbol symbol, ulong TWSVol)
		{
			if (symbol.AssetClass == AssetClass.Stock)
			{
				return TWSVol * 100;
			}
			else
			{
				return TWSVol;
			}
		}

		void client_TickPrice(object sender, TickPriceEventArgs e)
		{
			Symbol symbol = null;
			TickData data;
			GotTickData listener = null;
			lock (_lockObject)
			{
				symbol = idToSymbol(e.TickerId);
				if (symbol == null)
				{
					//	Not a watched symbol
					return;
				}

				data = new TickData();
				data.time = GetAccountTime("price tick");
				data.price = (double)e.Price;

				if (data.price == 0)
				{
					//	GBP/USD was getting bid and ask ticks with a price of zero, so ignore these.
					return;
				}

				if (e.TickType == KRSTickType.BidPrice)
				{
					//	Bid price
					data.tickType = TickType.Bid;
					lastBidSizes.TryGetValue(symbol, out data.size);
					lastBidPrices[symbol] = (double)e.Price;
				}
				else if (e.TickType == KRSTickType.AskPrice)
				{
					//	Ask price
					data.tickType = TickType.Ask;
					lastAskSizes.TryGetValue(symbol, out data.size);
					lastAskPrices[symbol] = (double)e.Price;
				}
				else if (e.TickType == KRSTickType.LastPrice)
				{
					//	Last price;
					lastPrices[symbol] = (double)e.Price;

					if (symbol.AssetClass == AssetClass.Index)
					{
						// Indexes don't come with volume ticks, so we can
						// force this tick through instead of trying to match
						// it up with a volume tick.
						data.tickType = TickType.Trade;
					}
					else
					{
						return;
					}
				}
				else if (e.TickType == KRSTickType.HighPrice)
				{
					//	High price
					data.tickType = TickType.HighPrice;
				}
				else if (e.TickType == KRSTickType.LowPrice)
				{
					//	Low price
					data.tickType = TickType.LowPrice;
				}
				else if (e.TickType == KRSTickType.ClosePrice)
				{
					//	Close price
					data.tickType = TickType.PreviousClose;
				}
				else if (e.TickType == Krs.Ats.IBNet.TickType.OpenPrice)
				{
					data.tickType = TickType.OpenPrice;
				}
				else
				{
					//	Unknown tick type
					return;
				}
				listener = tickListener;
			}
			if (data.tickType != TickType.NotSet && listener != null)
			{
				listener(symbol, data);
			}

		}

		void client_TickSize(object sender, TickSizeEventArgs e)
		{
			Symbol symbol = null;
			TickData data;
			GotTickData listener = null;

			lock (_lockObject)
			{
				symbol = idToSymbol(e.TickerId);
				if (symbol == null)
				{
					//	Not a watched symbol
					return;
				}

				data = new TickData();
				data.time = GetAccountTime("size tick");
				data.size = AdjustVolume(symbol, (UInt64)e.Size);
				if (e.TickType == KRSTickType.BidSize)
				{
					//	Bid size
					data.tickType = TickType.Bid;
					lastBidPrices.TryGetValue(symbol, out data.price);
					lastBidSizes[symbol] = data.size;
				}
				else if (e.TickType == KRSTickType.AskSize)
				{
					//	Ask size
					data.tickType = TickType.Ask;
					lastAskPrices.TryGetValue(symbol, out data.price);
					lastAskSizes[symbol] = data.size;
				}
				else if (e.TickType == KRSTickType.LastSize)
				{
					//	Last Size
					return;
					//data.tickType = TickType.LastSize;
				}
				else if (e.TickType == KRSTickType.Volume)
				{
					//	Volume
					bool bSend = true;
					UInt64 lastVolume;
					TickData tradeTick = new TickData();
					if (!lastVolumes.TryGetValue(symbol, out lastVolume))
					{
						bSend = false;
					}
					else if ((UInt64)e.Size <= lastVolume)
					{
						bSend = false;
					}
					else if (!lastPrices.TryGetValue(symbol, out tradeTick.price))
					{
						bSend = false;
					}
					//if (lastVolume == -1 || data.value <= lastVolume)
					//{
					//    bSend = false;
					//}

					if (bSend)
					{
						tradeTick.time = data.time;
						tradeTick.tickType = TickType.Trade;
						tradeTick.size = AdjustVolume(symbol, (UInt64)e.Size - lastVolume);

						//lastVolume = e.size * 100;


						if (tickListener != null)
						{
							tickListener(symbol, tradeTick);
						}
					}

					if (e.Size > 0)
					{
						lastVolumes[symbol] = (UInt64)e.Size;
					}

					data.tickType = TickType.DailyVolume;


				}
				else
				{
					//	Unknown tick type
					return;
				}
				listener = tickListener;
			}

			if (listener != null)
			{
				listener(symbol, data);
			}
		}

		void client_UpdateAccountValue(object sender, UpdateAccountValueEventArgs e)
		{
			lock (_lockObject)
			{
				if (e.Key == "BuyingPower")
				{
					buyingPower = Convert.ToDouble(e.Value);
				}
			}
		}

		void client_UpdatePortfolio(object sender, UpdatePortfolioEventArgs e)
		{
			PositionAvailableDelegate del;
			Symbol symbol;
			lock (_lockObject)
			{
				symbol = TWSAssetArgs.SymbolFromContract(e.Contract);
				openShares[symbol] = e.Position;
				del = PositionAvailable;
			}
			if (del != null)
			{
				del(symbol, e.Position);
			}
		}

		void client_OrderStatus(object sender, OrderStatusEventArgs e)
		{
			lock (_lockObject)
			{
				string msg = "IB order status: " + e.OrderId + " " + e.Status;
				//Console.WriteLine(msg);
				Trace.WriteLine(msg);

				if (openOrders.ContainsKey(e.OrderId.ToString()))
				{
#if RE1_1
				RightEdge.Common.Order openOrder = openOrders[e.OrderId.ToString()];
#else
					RightEdge.Common.BrokerOrder openOrder = openOrders[e.OrderId.ToString()];
#endif
					bool orderProcessed = false;
					Fill fill = null;
					string information = "";

					switch (e.Status)
					{
						case OrderStatus.Filled:
							//	Handle fills with ExecDetails event
//                            fill = new Fill();
//                            fill.FillDateTime = GetAccountTime("fill");
//#if RE1_1
//                        fill.Price = new Price() { AccountPrice = e.LastFillPrice, SymbolPrice = e.LastFillPrice };
//#else
//                            fill.Price = new Price(e.LastFillPrice, e.LastFillPrice);
//#endif

//                            int alreadyFilled = 0;
//                            foreach (Fill existingFill in openOrder.Fills)
//                            {
//                                alreadyFilled += fill.Quantity;
//                            }

//                            //	Don't know whether e.filled is cumulative or not
//                            if (e.Filled + e.Remaining == (int)openOrder.Shares)
//                            {
//                                fill.Quantity = e.Filled - alreadyFilled;
//                            }
//                            else
//                            {
//                                fill.Quantity = e.Filled;
//                            }

//                            //	Apparently IB doesn't send commissions
//                            fill.Commission = 0;

//                            openOrder.Fills.Add(fill);

//                            if (e.Remaining == 0)
//                            {
//                                openOrder.OrderState = BrokerOrderState.Filled;
//                            }
//                            else
//                            {
//                                openOrder.OrderState = BrokerOrderState.PartiallyFilled;
//                                information = "Partial fill";
//                            }
//                            orderProcessed = true;
							break;

						//	Apparently, stop orders don't get "Submitted", they get "presubmitted" instead
						case OrderStatus.Submitted:
						case OrderStatus.PreSubmitted:
						case OrderStatus.ApiPending:
							if (e.Status == OrderStatus.ApiPending)
							{
								//	Not sure what the ApiPending status is used for
								int b = 0;
							}

							Trace.WriteLine("IB " + e.Status.ToString() + ": " + openOrder.ToString());
							if (_gettingReconnectData)
							{
								if (_potentiallyCancelledOrders.ContainsKey(openOrder.OrderId))
								{
									_potentiallyCancelledOrders.Remove(openOrder.OrderId);
								}
							}
							else
							{
								openOrder.OrderState = BrokerOrderState.Submitted;
								orderProcessed = true;
							}
							if (unSubmittedOrders.ContainsKey(openOrder.OrderId))
							{
								unSubmittedOrders.Remove(openOrder.OrderId);
							}

							break;

						case OrderStatus.Canceled:
							openOrder.OrderState = BrokerOrderState.Cancelled;
							information = "Cancelled";
							orderProcessed = true;
							break;
						
					}

					if (orderProcessed)
					{
						OrderUpdatedDelegate tmp = OrderUpdated;
						if (tmp != null)
						{
							tmp(openOrder, fill, information);
						}

						if (openOrder.OrderState == BrokerOrderState.Filled)
						{
							//	TODO: AddShares
							//AddShares(openOrder);
						}
					}

					if (openOrder.OrderState == BrokerOrderState.Filled || openOrder.OrderState == BrokerOrderState.Rejected ||
						openOrder.OrderState == BrokerOrderState.Cancelled)
					{
						openOrders.Remove(openOrder.OrderId);
					}
				}
			}
		}

		void client_ExecDetails(object sender, ExecDetailsEventArgs e)
		{
			lock (_lockObject)
			{
				Symbol symbol = TWSAssetArgs.SymbolFromContract(e.Contract);

				string msg = "IB ExecDetails: " + e.Execution.Time + " " + e.Execution.Side + " " + symbol + " Order ID: " + e.Execution.OrderId +
					" Size: " + e.Execution.Shares + " Price: " + e.Execution.Price;
				Trace.WriteLine(msg);
				//Console.WriteLine(msg);

				bool bIgnore = false;

				if (!string.IsNullOrEmpty(accountCode) && e.Execution.AccountNumber != accountCode)
				{
					Trace.WriteLine("### Execution ignored - Wrong Account");
					bIgnore = true;
				}
				else if (e.Execution.Shares < 0)
				{
					Trace.WriteLine("### Execution Ignored - Negative Fill");
					bIgnore = true;
				}

				if (!bIgnore)
				{

					string datePattern = "yyyyMMdd hh:mm:ss";
					DateTimeFormatInfo dateFormat = new DateTimeFormatInfo();
					dateFormat.SetAllDateTimePatterns(new[] { "yyyyMMdd" }, 'd');
					dateFormat.SetAllDateTimePatterns(new[] { "HH:mm:ss" }, 't');

					string[] dateSplit = e.Execution.Time.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

					DateTime execDateTime = DateTime.ParseExact(dateSplit[0], "d", dateFormat).Date;
					TimeSpan execTime = DateTime.ParseExact(dateSplit[1], "t", dateFormat).TimeOfDay;
					execDateTime += execTime;
#if RE1_1
					RightEdge.Common.Order openOrder;
#else
					BrokerOrder openOrder;
#endif

					if (openOrders.TryGetValue(e.OrderId.ToString(), out openOrder))
					{
						//	If we are reconnecting after a disconnect, we may get execDetails for partial fills that
						//	we already know about.  So loop through the fills through this order and see if
						//	we already know about this execution.
						bool alreadyReported = false;
						foreach (Fill fill in openOrder.Fills)
						{
							string fillTimeString = fill.FillDateTime.ToString(datePattern);
							if (fillTimeString == e.Execution.Time && fill.Price.SymbolPrice == e.Execution.Price && fill.Quantity == e.Execution.Shares)
							{
								alreadyReported = true;
							}
						}

						if (!alreadyReported)
						{
							string information = "";

							Fill fill = new Fill();
							fill.FillDateTime = execDateTime;
#if RE1_1
						fill.Price = new Price();
						fill.Price.AccountPrice = e.Execution.Price;
						fill.Price.SymbolPrice = e.Execution.Price;
#else
							fill.Price = new Price(e.Execution.Price, e.Execution.Price);
#endif
							fill.Quantity = e.Execution.Shares;

							//	Apparently IB doesn't send commissions
							fill.Commission = 0;

							openOrder.Fills.Add(fill);

							int totalFilled = 0;
							foreach (Fill f in openOrder.Fills)
							{
								totalFilled += f.Quantity;
							}
#if RE1_1
						if (totalFilled < (int)openOrder.Shares)
#else
							if (totalFilled < openOrder.Shares)
#endif
							{
								openOrder.OrderState = BrokerOrderState.PartiallyFilled;
								information = "Partial fill";
							}
							else
							{
								openOrder.OrderState = BrokerOrderState.Filled;
								openOrders.Remove(openOrder.OrderId);

								//	Only remove from the potentially cancelled orders if it was completely filled
								//	It may have been partially filled while disconnected and then cancelled
								if (_gettingReconnectData)
								{
									if (_potentiallyCancelledOrders.ContainsKey(openOrder.OrderId))
									{
										_potentiallyCancelledOrders.Remove(openOrder.OrderId);
									}
								}
							}

							_filledOrders.RecordFill(openOrder.OrderId, fill.FillDateTime);

							//var callback = OutOfBandCallback;
							//if (callback != null)
							//{
							//    callback(this, EventArgs.Empty);
							//}

							OrderUpdatedDelegate tmp = OrderUpdated;
							if (tmp != null)
							{
								tmp(openOrder, fill, information);
							}
						}
					}
				}
			}
		}

		void client_Error(object sender, ErrorEventArgs e)
		{
			lock (_lockObject)
			{
				try
				{
					string errorText = "Error! id=" + e.TickerId + " errorCode=" + e.ErrorCode + "\r\n" + e.ErrorMsg;

					int errorCode = (int)e.ErrorCode;

					if (errorCode == 165)
					{
						return;
					}

					//if (errorCode == 2106 && _histRetrieval != null && !_histRetrieval.ReceivedData)
					//{
					//    System.Diagnostics.Trace.WriteLine("Historical data available.  Resending historical data request...");
					//    _histRetrieval.SendRequest();
					//}

					if (errorCode == 2107 && _histRetrieval != null && _histRetrieval.bPaused)
					{
						//	Resume historical data collection
						System.Diagnostics.Trace.WriteLine("Historical data available.  Resuming data collection...");
						_histRetrieval.bPaused = false;
						_histRetrieval.SendRequest();
					}

					// error code 202 is a cancelled order ... we want to know about these!
#if RE1_1
			RightEdge.Common.Order order;
#else
					RightEdge.Common.BrokerOrder order;
#endif
					string information = "";
					if (openOrders.TryGetValue(e.TickerId.ToString(), out order))
					{
						if (errorCode >= 2100 && errorCode <= 3000)
						{
							//	It's probably just a warning, and the order may continue
							Console.WriteLine("IB Warning code " + errorCode + " for order ID " + e.TickerId + ": " + e.ErrorMsg);
							return;
						}

						if (errorCode == 202)
						{
							order.OrderState = BrokerOrderState.Cancelled;
							information = e.ErrorMsg;
						}
						else
						{
							order.OrderState = BrokerOrderState.Rejected;
							information = e.ErrorMsg;
						}
						OrderUpdatedDelegate tmp = OrderUpdated;
						if (tmp != null)
						{
							tmp(order, null, information);
						}
						return;
					}

					System.Diagnostics.Trace.WriteLine(errorText);

					if (errorCode == 1100)		//	Connectivity has been lost
					{
						_watching = false;
						_connected = false;
						errorText = "Disconnected: " + errorText;
					}
					else if (errorCode == 1102)
					{
						_hadError1102 = true;
						TimeSpan diff = DateTime.Now.Subtract(_waitHandleTime);
						Trace.WriteLine("Time from waithandle set to errorCode 1102: " + diff.ToString());
						int b = 0;
					}

					if (errorCode < 2000 && errorCode != 202)
					{
						if (_histRetrieval != null)
						{
							//	Currently retrieving historical data
							if (errorCode == 162 && e.ErrorMsg.Contains("Historical data request pacing violation"))
							{
								_histRetrieval.bPaused = true;
								System.Diagnostics.Trace.WriteLine("Historical data pacing violation.  Waiting for data to become available...");
								_histRetrieval.waitEvent.Set();
								//	Need to wait for this:
								//	Error! id=-1 errorCode=2107
								//	HMDS data farm connection is inactive but should be available upon demand.:ushmds2a
							}
							else if ((errorCode == 321 && e.ErrorMsg.Contains("Historical data queries on this contract requesting any data earlier than")) ||
									(errorCode == 162 && e.ErrorMsg.Contains("query returned no data")))
							{
								//	Error code 321
								//	Error validating request:-'qb' : cause - Historical data queries on this contract requesting any data earlier than one year back from now which is 20060218 12:34:47 EST are rejected.  Your query would have run from 20060214 00:00:00 EST to 20060221 00:00:00 EST.

								//	Error! id=34 errorCode=162
								//	Historical Market Data Service error message:HMDS query returned no data: ESU8@GLOBEX Trades

								//	We will not treat this as an error.  We will simply return the data that we could get.

								_histRetrieval.Done = true;
								_histRetrieval.waitEvent.Set();
							}
							else
							{

								System.Diagnostics.Trace.WriteLine("Error ended historical data retrieval: " + errorText);
								lastError = errorText;
								hadError = true;
								_histRetrieval.waitEvent.Set();
							}
						}
						else
						{
							lastError = errorText;
							hadError = true;
						}
					}
				}
				catch (Exception ex)
				{
					RightEdge.Common.Internal.TraceHelper.DumpExceptionToTrace(ex);
				}
			}
		}

		void client_NextValidId(object sender, NextValidIdEventArgs e)
		{
			lock (_lockObject)
			{
				nextOrderId = e.OrderId;
			}
		}

		private DateTime _waitHandleTime;

		void client_CurrentTime(object sender, CurrentTimeEventArgs e)
		{
			lock (_lockObject)
			{
				DateTime accountTime = e.Time.ToLocalTime();
				accountTimeDiff = accountTime.Subtract(DateTime.Now);

				bGotAccountTime = true;

				System.Diagnostics.Trace.WriteLine("Account time updated: " + accountTime.ToString() + "  Current time: " + DateTime.Now +
					" Diff: " + accountTimeDiff.ToString());
				//Console.WriteLine("IB Current Time: " + accountTime.ToString());

				if (_gettingReconnectData)
				{
					_waitHandleTime = DateTime.Now;
					_connectWaitHandle.Set();
				}
			}
		}

		#region IService Members

		public string ServiceName()
		{
			return "Interactive Brokers Plugin";
		}

		public string Author()
		{
			return "Yye Software";
		}

		public string Description()
		{
			return "Retrieves data and provides broker functions for Interactive Brokers.  " + 
				"This is a pure .NET plugin and does not require the IB API to be installed.";
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
			return "{B03427B2-5405-4686-A922-F888836C19BC}";
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
			return false;
		}

		public bool SupportsMultipleInstances()
		{
			return true;
		}

		public string ServerAddress
		{
			get;
			set;
		}

		public int Port
		{
			get;
			set;
		}

		public string UserName
		{
			get
			{
				return "";
			}
			set
			{

			}
		}

		public string Password
		{
			get
			{
				return "";
			}
			set
			{

			}
		}

		public bool BarDataAvailable
		{
			get { return true; }
		}

		public bool TickDataAvailable
		{
			get { return true; }
		}

		public bool BrokerFunctionsAvailable
		{
			get { return true; }
		}

		public IBarDataRetrieval GetBarDataInterface()
		{
			return this;
		}

		public ITickRetrieval GetTickDataInterface()
		{
			return this;
		}

		public IBroker GetBrokerInterface()
		{
			return this;
		}

		public bool HasCustomSettings()
		{
			return true;
		}

		public bool ShowCustomSettingsForm(ref SerializableDictionary<string, string> settings)
		{
			TWSSettings dlg = new TWSSettings();

			string rth = "";
			string ignorelast = "";
			
			string clientIDBroker = _clientIDBroker.ToString();
			string clientIDLiveData = _clientIDLiveData.ToString();
			string clientIDHist = _clientIDHist.ToString();

			string acctCode = "";
			string faMethod = "";
			string faPercentage = "";
			string faProfile = "";


			if (settings.TryGetValue("UseRTH", out rth))
			{
				dlg.UseRTH = Convert.ToBoolean(rth);
			}

			if (settings.TryGetValue("IgnoreLastHistBar", out ignorelast))
			{
				dlg.IgnoreLastHistBar = Convert.ToBoolean(ignorelast);
			}

			if (settings.TryGetValue("ClientIDBroker", out clientIDBroker))
			{
				dlg.ClientIDBroker = clientIDBroker;
			}
			else
			{
				dlg.ClientIDBroker = _clientIDBroker.ToString();
			}

			if (settings.TryGetValue("ClientIDLiveData", out clientIDLiveData))
			{
				dlg.ClientIDLiveData = clientIDLiveData;
			}
			else
			{
				dlg.ClientIDLiveData = _clientIDLiveData.ToString();
			}

			if (settings.TryGetValue("ClientIDHist", out clientIDHist))
			{
				dlg.ClientIDHist = clientIDHist;
			}
			else
			{
				dlg.ClientIDHist = _clientIDHist.ToString();
			}

			if (settings.TryGetValue("AccountCode", out acctCode))
			{
				dlg.AccountCode = acctCode;
			}

			if (settings.TryGetValue("FAMethod", out faMethod))
			{
				dlg.FAMethod = GetFAMethod(faMethod); ;
			}

			if (settings.TryGetValue("FAPercentage", out faPercentage))
			{
				dlg.FAPercentage = faPercentage;
			}

			if (settings.TryGetValue("FAProfile", out faProfile))
			{
				dlg.FAProfile = faProfile;
			}

			if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				settings["UseRTH"] = dlg.UseRTH.ToString();
				settings["IgnoreLastHistBar"] = dlg.IgnoreLastHistBar.ToString();

				settings["ClientIDBroker"] = dlg.ClientIDBroker;
				settings["ClientIDLiveData"] = dlg.ClientIDLiveData;
				settings["ClientIDHist"] = dlg.ClientIDHist;

				settings["AccountCode"] = dlg.AccountCode;
				settings["FAMethod"] = dlg.FAMethod.ToString();
				settings["FAPercentage"] = dlg.FAPercentage;
				settings["FAProfile"] = dlg.FAProfile;				
			}

			return true;
		}

		private FinancialAdvisorAllocationMethod GetFAMethod(string s)
		{
			var parsedValue = EnumUtil<FinancialAdvisorAllocationMethod>.Parse(s);
			if (parsedValue.Success)
			{
				return parsedValue.Value;
			}
			else
			{
				return FinancialAdvisorAllocationMethod.None;
			}
		}

		public bool Initialize(SerializableDictionary<string, string> settings)
		{
			string rth = "";
			string ignorelast;

			string clientIDBroker = "";
			string clientIDLiveData = "";
			string clientIDHist = "";


			if (settings.TryGetValue("UseRTH", out rth))
			{
				_useRTH = Convert.ToBoolean(rth);
			}

			if (settings.TryGetValue("IgnoreLastHistBar", out ignorelast))
			{
				dropLastHistBar = Convert.ToBoolean(ignorelast);
			}

			if (settings.TryGetValue("ClientIDBroker", out clientIDBroker))
			{
				int.TryParse(clientIDBroker, out _clientIDBroker);
			}

			if (settings.TryGetValue("ClientIDLiveData", out clientIDLiveData))
			{
				int.TryParse(clientIDLiveData, out _clientIDLiveData);
			}

			if (settings.TryGetValue("ClientIDHist", out clientIDHist))
			{
				int.TryParse(clientIDHist, out _clientIDHist);
			}


			settings.TryGetValue("AccountCode", out accountCode);

			string faMethod;
			if (settings.TryGetValue("FAMethod", out faMethod))
			{
				_FAMethod = GetFAMethod(faMethod);
			}

			settings.TryGetValue("FAPercentage", out _FAPercentage);

			settings.TryGetValue("FAProfile", out _FAProfile);

			return true;
		}
#if RE1_1
		public bool Connect()
#else
		public bool Connect(ServiceConnectOptions connectOptions)
#endif
		{
			ClearError();

			if (_connected)
				return true;

			foreach (Symbol symbol in new List<Symbol>(watchedSymbols.Keys))
			{
				watchedSymbols[symbol] = null;
			}

			if (client == null)
			{
				client = new IBClient();
				client.ThrowExceptions = true;

				client.Error += client_Error;
				client.TickPrice += client_TickPrice;
				client.TickSize += client_TickSize;
				client.UpdateAccountValue += client_UpdateAccountValue;
				client.UpdatePortfolio += client_UpdatePortfolio;
				client.OrderStatus += client_OrderStatus;
				client.ExecDetails += client_ExecDetails;
				client.NextValidId += client_NextValidId;
				client.CurrentTime += client_CurrentTime;
			}

			int clientID = -1;
#if !RE1_1
			bool brokerConnect = ((connectOptions & ServiceConnectOptions.Broker) == ServiceConnectOptions.Broker);
			
			if ((connectOptions & ServiceConnectOptions.Broker) == ServiceConnectOptions.Broker)
			{
				clientID = _clientIDBroker;
			}
			else if ((connectOptions & ServiceConnectOptions.LiveData) == ServiceConnectOptions.LiveData)
			{
				clientID = _clientIDLiveData;
			}
			else if ((connectOptions & ServiceConnectOptions.HistoricalData) == ServiceConnectOptions.HistoricalData)
			{
				clientID = _clientIDHist;
			}
#endif
			if (clientID < 0)
			{
				clientID = new Random().Next();
			}

			client.Connect(string.IsNullOrEmpty(ServerAddress) ? "127.0.0.1" : ServerAddress, (Port == 0) ? 7496 : Port, clientID);
			lock (_lockObject)
			{
				_connected = true;
			}
#if !RE1_1
			if (brokerConnect)
			{
#endif
				lock (_lockObject)
				{
					_connectWaitHandle = new ManualResetEvent(false);
					_gettingReconnectData = true;
					_hadError1102 = false;
					foreach (string id in openOrders.Keys)
					{
						_potentiallyCancelledOrders[id] = null;
					}
#if !RE1_1
				}
#endif

				client.RequestAccountUpdates(true, accountCode);
				//client.ReqAllOpenOrders();
				client.RequestOpenOrders();

				ExecutionFilter filter = new ExecutionFilter();
				filter.ClientId = clientID;
				filter.Side = ActionSide.Buy;
				client.RequestExecutions(nextID++, filter);
				filter.Side = ActionSide.Sell;
				client.RequestExecutions(nextID++, filter);

				//	Request the current time so that when we get it, we know that (hopefully)
				//	we have gotten all the results from ReqOpenOrders and ReqExecutions
				client.RequestCurrentTime();

				if (!_connectWaitHandle.WaitOne(TimeSpan.FromSeconds(10.0), true))
				{
					string msg = "Timed out waiting for TWS order and execution data to finish.";
					Trace.WriteLine(msg);
					Console.WriteLine(msg);
				}

				if (_potentiallyCancelledOrders.Count > 0)
				{
					//	Wait a bit longer to check for errorCode 1102
					Thread.Sleep(500);
				}

				lock (_lockObject)
				{


					_gettingReconnectData = false;

					if (!_hadError1102)
					{
						foreach (string orderID in _potentiallyCancelledOrders.Keys)
						{
#if RE1_1
						RightEdge.Common.Order order;
#else
							BrokerOrder order;
#endif
							if (openOrders.TryGetValue(orderID, out order))
							{
								order.OrderState = BrokerOrderState.Cancelled;
								OrderUpdatedDelegate tmp = OrderUpdated;
								if (tmp != null)
								{
									tmp(order, null, "Order cancelled while disconnected.");
								}
								openOrders.Remove(orderID);
							}
							else
							{
								int b = 0;
							}
						}
					}
					_potentiallyCancelledOrders.Clear();
				}
			}

			return true;
		}

		public bool Disconnect()
		{
			if (client != null)
			{
				client.Error -= client_Error;
				client.TickPrice -= client_TickPrice;
				client.TickSize -= client_TickSize;
				client.UpdateAccountValue -= client_UpdateAccountValue;
				client.UpdatePortfolio -= client_UpdatePortfolio;
				client.OrderStatus -= client_OrderStatus;
				client.ExecDetails -= client_ExecDetails;
				client.NextValidId -= client_NextValidId;
				client.CurrentTime -= client_CurrentTime;

				client.Disconnect();
			}
			client = null;

			lock (_lockObject)
			{
				watchedSymbols.Clear();
				_watching = false;

				if (!_connected)
				{
					lastError = "Not connected.";
					hadError = true;
					return false;
				}
				_connected = false;
				ClearError();
				return true;
			}
		}

		public string GetError()
		{
			lock (_lockObject)
			{
				return lastError;
			}
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			//	TODO: ???
			Disconnect();
		}

		#endregion

		#region ITickRetrieval Members

		public bool RealTimeDataAvailable
		{
			get { return true; }
		}

		public GotTickData TickListener
		{
			set
			{
				lock (_lockObject)
				{
					tickListener = value;
				}
			}
		}

		public bool SetWatchedSymbols(List<Symbol> symbols)
		{
			bool bNeedsExit = false;
			try
			{
				Monitor.Enter(_lockObject);
				bNeedsExit = true;
				foreach (Symbol symbol in symbols)
				{
					if (!watchedSymbols.ContainsKey(symbol))
					{
						watchedSymbols[symbol] = null;
					}
				}
				foreach (Symbol symbol in new List<Symbol>(watchedSymbols.Keys))
				{
					if (!symbols.Contains(symbol))
					{
						if (_watching && watchedSymbols[symbol].HasValue)
						{
							int tickerToCancel = watchedSymbols[symbol].Value;

							Monitor.Exit(_lockObject);
							bNeedsExit = false;
							client.CancelMarketData(tickerToCancel);
							Monitor.Enter(_lockObject);
							bNeedsExit = true;
						}
						watchedSymbols.Remove(symbol);
						forgetData(symbol);
					}
				}

				//	Check error here because StartWatching() will clear error status
				if (!CheckError())
				{
					return false;
				}
			}
			finally
			{
				if (bNeedsExit)
				{
					Monitor.Exit(_lockObject);
				}
			}
		
			if (_watching)
			{
				StartWatching();
			}
			lock (_lockObject)
			{
				return CheckError();
			}

		}

		public bool IsWatching()
		{
			lock (_lockObject)
			{
				return _watching;
			}
		}

		public bool StartWatching()
		{
			bool bNeedsExit = false;
			try
			{
				Monitor.Enter(_lockObject);
				bNeedsExit = true;

				ClearError();
				foreach (Symbol symbol in new List<Symbol>(watchedSymbols.Keys))
				{
					if (watchedSymbols[symbol] == null)
					{
						int id = nextID++;
						TWSAssetArgs args = TWSAssetArgs.Create(symbol);
						//Contract contract = new Contract(args.Symbol, args.SecType, args.Expiry, args.Strike,
						//    args.Right, args.Multiplier, args.Exchange, args.Currency, "", args.PrimaryExchange);
						Contract contract = args.ToContract();

						Monitor.Exit(_lockObject);
						bNeedsExit = false;
						client.RequestMarketData(id, contract, null, false);
						Monitor.Enter(_lockObject);
						bNeedsExit = true;

						watchedSymbols[symbol] = id;
					}
				}

				if (CheckError())
				{
					_watching = true;
				}
			}
			finally
			{
				if (bNeedsExit)
				{
					Monitor.Exit(_lockObject);
				}
			}

			client.RequestCurrentTime();

			// Request the next valid ID for an order
			client.RequestIds(1);

			lock (_lockObject)
			{
				return CheckError();
			}

		}

		public bool StopWatching()
		{
			bool bNeedsExit = false;
			try
			{
				Monitor.Enter(_lockObject);
				bNeedsExit = true;
	
				ClearError();
				_watching = false;
				foreach (Symbol symbol in new List<Symbol>(watchedSymbols.Keys))
				{
					if (watchedSymbols[symbol] != null)
					{
						int idToCancel = watchedSymbols[symbol].Value;

						Monitor.Exit(_lockObject);
						bNeedsExit = false;
						client.CancelMarketData(idToCancel);
						Monitor.Enter(_lockObject);
						bNeedsExit = true;
						
						watchedSymbols[symbol] = null;
						forgetData(symbol);
					}
				}
				return true;
			}
			finally
			{
				if (bNeedsExit)
				{
					Monitor.Exit(_lockObject);
				}
			}
		}

		public IService GetService()
		{
			return this;
		}

		#endregion

		private void forgetData(Symbol symbol)
		{
			if (lastPrices.ContainsKey(symbol))
			{
				lastPrices.Remove(symbol);
			}
			if (lastVolumes.ContainsKey(symbol))
			{
				lastVolumes.Remove(symbol);
			}
			if (lastBidPrices.ContainsKey(symbol))
			{
				lastBidPrices.Remove(symbol);
			}
			if (lastBidSizes.ContainsKey(symbol))
			{
				lastBidSizes.Remove(symbol);
			}
			if (lastAskPrices.ContainsKey(symbol))
			{
				lastAskPrices.Remove(symbol);
			}
			if (lastAskSizes.ContainsKey(symbol))
			{
				lastAskSizes.Remove(symbol);
			}

		}

		#region IBarDataRetrieval Members
#if RE1_1
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
#endif

#if RE1_1
		public List<BarData> RetrieveData(Symbol symbol, int frequency, AssetClass assetClass, double? strikePrice, DateTime? expirationDate, ContractType? contract, DateTime startDate, DateTime endDate)
#else
		public List<BarData> RetrieveData(Symbol symbol, int frequency, DateTime startDate, DateTime endDate, BarConstructionType barConstruction)
#endif
		{
			if (_histRetrieval != null)
			{
				lastError = "Historical data retrieval already in progress.";
				return null;
			}

			if (Thread.CurrentThread.Name == null)
			{
				Thread.CurrentThread.Name = "RetrieveData thread";
			}
			Trace.WriteLine("RetrieveData: " + startDate.ToString() + "-" + endDate.ToString() + " Thread: " +
				Thread.CurrentThread.ManagedThreadId.ToString("x"));

			ClearError();
			if (!_connected)
			{
#if RE1_1
				if (!Connect())
#else
				if (!Connect(ServiceConnectOptions.HistoricalData))
#endif
				{
					return null;
				}
			}

			EventHandler<HistoricalDataEventArgs> handler = null;
			try
			{
				BarSize barSize;
				//string TWSFreq;
				//	Legal ones are: 1 secs, 5 secs, 15 secs, 30 secs, 1 min, 2 mins, 3 mins, 5 mins, 15 mins, 30 mins, 1 hour, 1 day, 1 week, 1 month, 3 months, 1 year
				if (frequency == (int)BarFrequency.OneMinute)
				{
					//TWSFreq = "1 min";
					barSize = BarSize.OneMinute;
				}
				else if (frequency == 2)
				{
					//TWSFreq = "2 mins";
					barSize = BarSize.TwoMinutes;
				}
				else if (frequency == 3)
				{
					//TWSFreq = "3 mins";
					barSize = BarSize.ThreeMinutes;
				}
				else if (frequency == (int)BarFrequency.FiveMinute)
				{
					//TWSFreq = "5 mins";
					barSize = BarSize.FiveMinutes;
				}
				else if (frequency == (int)BarFrequency.FifteenMinute)
				{
					//TWSFreq = "15 mins";
					barSize = BarSize.FifteenMinutes;
				}
				else if (frequency == (int)BarFrequency.ThirtyMinute)
				{
					//TWSFreq = "30 mins";
					barSize = BarSize.ThirtyMinutes;
				}
				else if (frequency == (int)BarFrequency.SixtyMinute)
				{
					//TWSFreq = "1 hour";
					barSize = BarSize.OneHour;
				}
				else if (frequency == (int)BarFrequency.Daily)
				{
					//TWSFreq = "1 day";
					barSize = BarSize.OneDay;
				}
				else if (frequency == (int)BarFrequency.Weekly)
				{
					//TWSFreq = "1 week";
					barSize = BarSize.OneWeek;
				}
				else if (frequency == (int)BarFrequency.Monthly)
				{
					//TWSFreq = "1 month";
					barSize = BarSize.OneMonth;
				}
				else if (frequency == (int)BarFrequency.Yearly)
				{
					//TWSFreq = "1 year";
					barSize = BarSize.OneYear;
				}
				else
				{
					lastError = "Frequency not supported for historical data retrieval.";
					return null;
				}

				DateTime accountTime = GetAccountTime("RetrieveData call");
				if (endDate > accountTime || endDate == DateTime.MinValue)
				{
					endDate = accountTime.AddHours(12);
				}

				_histRetrieval = new HistRetrieval();
				_histRetrieval.client = client;
				_histRetrieval.twsPlugin = this;
				_histRetrieval.symbol = symbol;
				_histRetrieval.barSize = barSize;
				_histRetrieval.startDate = startDate;
				_histRetrieval.endDate = endDate;
				_histRetrieval.RTHOnly = _useRTH;
#if RE1_1
				_histRetrieval.BarConstruction = BarConstructionType.Last;
#else
				_histRetrieval.BarConstruction = barConstruction;
#endif

				_histRetrieval.waitEvent = new ManualResetEvent(false);

				//if (_firstHistRequest)
				//{
				//    //	The first request for historical data seems to fail.  So we will submit a small request first and then wait a bit
				//    _histRetrieval.barSize = BarSize.OneDay;
				//    _histRetrieval.Duration = new TimeSpan(5, 0, 0, 0);
				//    _histRetrieval.SendRequest();

				//    Trace.WriteLine("TWS Plugin sleeping...");
				//    Thread.Sleep(2500);
				//    Trace.WriteLine("TWS Plugin done sleeping.");

				//    if (hadError)
				//    {
				//        return null;
				//    }

				//    _firstHistRequest = false;
				//}

				_histRetrieval.barSize = barSize;


				//	Requesting a duration which will return more than 2000 bars gives an "invalid step" error message
				//	See http://www.interactivebrokers.com/cgi-bin/discus/board-auth.pl?file=/2/39164.html
				//	So we need to find the largest duration which will return less than 2000 bars

				//	Since the trading day is from 9:30 to 4:00, there are 6.5 hours, or 390 minutes per trading day
				//	This means that 5 days will have a bit less than 2000 bars.

				if (frequency < (int)BarFrequency.Daily)
				{
					_histRetrieval.Duration = new TimeSpan(5, 0, 0, 0);
				}
				else
				{
					//_histRetrieval.Duration = new TimeSpan(7, 0, 0, 0);
					//	Can only request up to 52 weeks at once
					//	Requesting data that is more than a year old will reject the whole request
					_histRetrieval.Duration = new TimeSpan(360, 0, 0, 0);
				}

				

				handler = new EventHandler<HistoricalDataEventArgs>(
					delegate(object sender, HistoricalDataEventArgs args)
					{
						_histRetrieval.GotData(args);
					});

				client.HistoricalData += handler;

				int pacingPause = 1000;

				while (!_histRetrieval.Done)
				{
					_histRetrieval.waitEvent.Reset();
					_histRetrieval.SendRequest();
					_histRetrieval.waitEvent.WaitOne();
					if (!CheckError())
					{
						return null;
					}
					if (_histRetrieval.bPaused)
					{
						Trace.WriteLine("Waiting " + pacingPause + " ms.");
						Thread.Sleep(pacingPause);
						Trace.WriteLine("Attempting to resume data collection.");
						_histRetrieval.bPaused = false;

						if (pacingPause < 30000)
						{
							pacingPause *= 2;
						}
					}
					else
					{
						pacingPause = 1000;
					}
				}

				if (!CheckError())
					return null;

				_histRetrieval.ret.Reverse();
				if (dropLastHistBar && _histRetrieval.ret.Count > 0)
				{
					_histRetrieval.ret.RemoveAt(_histRetrieval.ret.Count - 1);
				}

				return _histRetrieval.ret;
			}
			finally
			{
				if (handler != null)
				{
					client.HistoricalData -= handler;
				}
				_histRetrieval = null;
			}
		}

		#endregion

		#region IBroker Members
#if !RE1_1
		public void SetAccountState(BrokerAccountState state)
		{
			lock (_lockObject)
			{
				foreach (BrokerOrder order in state.PendingOrders)
				{
					openOrders.Add(order.OrderId, order);
				}
			}
		}
#endif

#if RE1_1
		public bool SubmitOrder(RightEdge.Common.Order order, out string orderId)
#else
		public bool SubmitOrder(RightEdge.Common.BrokerOrder order, out string orderId)
#endif
		{
			Krs.Ats.IBNet.Order apiOrder = new Krs.Ats.IBNet.Order();
			Contract contract;
			int intOrderId;

			lock (_lockObject)
			{
				// Before we submit the order, we need to make sure the price is trimmed
				// to something that IB will accept.  In other words, if a price is submitted
				// for 40.1032988923, this will be get rejected.

				if (order.LimitPrice > 0)
				{
					if (order.OrderSymbol.SymbolInformation.TickSize > 0)
					{
						// If a tick size is specified, round to this value.
						order.LimitPrice = SystemUtils.RoundToNearestTick(order.LimitPrice, order.OrderSymbol.SymbolInformation.TickSize);
					}
					else
					{
						// Otherwise, use decimal places specified in symbol setup.
						double multiplier = Math.Pow(10, order.OrderSymbol.SymbolInformation.DecimalPlaces);
						order.LimitPrice = Math.Round(order.LimitPrice * multiplier);
						order.LimitPrice /= multiplier;
					}
				}


				contract = TWSAssetArgs.Create(order.OrderSymbol).ToContract();

				if (order.TransactionType == TransactionType.Buy ||
					order.TransactionType == TransactionType.Cover)
				{
					apiOrder.Action = ActionSide.Buy;
				}
				else if (order.TransactionType == TransactionType.Sell)
				{
					apiOrder.Action = ActionSide.Sell;
				}
				else if (order.TransactionType == TransactionType.Short)
				{
					//	SShort is apparently only used as part of a combo leg, and you get an "Invalid side" error if you try to use it otherwise
					//apiOrder.Action = ActionSide.SShort;
					apiOrder.Action = ActionSide.Sell;
				}
				else
				{
					throw new RightEdgeError("Cannot submit order with transaction type " + order.TransactionType.ToString());
				}

				double limitPrice = 0.0;
				double auxPrice = 0.0;

				switch (order.OrderType)
				{
					case RightEdge.Common.OrderType.Limit:
						apiOrder.OrderType = Krs.Ats.IBNet.OrderType.Limit;
						limitPrice = order.LimitPrice;
						break;

					case RightEdge.Common.OrderType.LimitOnClose:
						apiOrder.OrderType = Krs.Ats.IBNet.OrderType.LimitOnClose;
						limitPrice = order.LimitPrice;
						break;

					case RightEdge.Common.OrderType.Market:
					case RightEdge.Common.OrderType.MarketOnOpen:
						apiOrder.OrderType = Krs.Ats.IBNet.OrderType.Market;
						break;

					case RightEdge.Common.OrderType.MarketOnClose:
						apiOrder.OrderType = Krs.Ats.IBNet.OrderType.MarketOnClose;
						break;

					case RightEdge.Common.OrderType.PeggedToMarket:
						apiOrder.OrderType = Krs.Ats.IBNet.OrderType.PeggedToMarket;
						break;

					case RightEdge.Common.OrderType.Stop:
						apiOrder.OrderType = Krs.Ats.IBNet.OrderType.Stop;
						auxPrice = order.StopPrice;
						break;

					case RightEdge.Common.OrderType.StopLimit:
						apiOrder.OrderType = Krs.Ats.IBNet.OrderType.StopLimit;
						auxPrice = order.StopPrice;
						limitPrice = order.LimitPrice;
						break;

#if !RE1_1
					//	TODO: investigate and add support for trailing stop
					case RightEdge.Common.OrderType.TrailingStop:
						if (order.TrailingStopType != TargetPriceType.RelativePrice)
						{
							lastError = order.TrailingStopType.ToString() + " trailing stops not supported by IB.";
							orderId = null;
							return false;
						}
						apiOrder.OrderType = Krs.Ats.IBNet.OrderType.TrailingStop;
						auxPrice = order.TrailingStop;
						break;
#endif
					default:
						lastError = "Order type not supported by IB service: " + order.OrderType.ToString();
						orderId = null;
						return false;
				}

				if (double.IsNaN(limitPrice))
				{
					throw new RightEdgeError("Limit price for order cannot be NaN");
				}
				if (double.IsNaN(auxPrice))
				{
					throw new RightEdgeError("Stop price for order cannot be NaN");
				}

				apiOrder.LimitPrice = (Decimal)limitPrice;
				apiOrder.AuxPrice = (Decimal)auxPrice;

				if (order.GoodTillCanceled)
				{
					//DateTime gtcDate = GetAccountTime("SubmitOrder").AddMonths(12);
					//apiOrder.GoodTillDate = gtcDate.ToString("yyyyMMdd");
					//apiOrder.GoodTillDate = "";
					apiOrder.GoodTillDate = "GTC";
					//	TODO: is this better?
					//apiOrder.Tif = TimeInForce.GoodTillCancel;
				}

				apiOrder.TotalQuantity = (int)order.Shares;
				apiOrder.FAGroup = accountCode;
				apiOrder.FAMethod = _FAMethod;
				apiOrder.FAPercentage = _FAPercentage;
				apiOrder.FAProfile = _FAProfile;
				//	TODOSOON: Verify that RTH still works after upgrading Krs library
				//if (_useRTH)
				//{
				//    apiOrder.IgnoreRth = true;
				//    apiOrder.RthOnly = true;
				//}
				apiOrder.OutsideRth = !_useRTH;

				orderId = nextOrderId.ToString();
				order.OrderId = orderId;
				openOrders.Add(orderId, order);

				unSubmittedOrders[order.OrderId] = true;

				intOrderId = nextOrderId;

				nextOrderId++;
			}

			client.PlaceOrder(intOrderId, contract, apiOrder);

			Trace.WriteLine("IB Sent: " + order.ToString());

			return true;

		}
#if RE1_1
		private bool internalCancelOrder(int id, RightEdge.Common.Order order)
#else
		private bool internalCancelOrder(int id, RightEdge.Common.BrokerOrder order)
#endif
		{
			Monitor.Exit(_lockObject);
			try
			{
				client.CancelOrder(id);
			}
			finally
			{
				Monitor.Enter(_lockObject);
			}

			if (unSubmittedOrders.ContainsKey(order.OrderId))
			{
				//	Apparently IB will not send us a cancellation confirmation in this case
				unSubmittedOrders.Remove(order.OrderId);
				order.OrderState = BrokerOrderState.Cancelled;

				openOrders.Remove(order.OrderId);

				OrderUpdatedDelegate tmp = OrderUpdated;
				if (tmp != null)
				{
					tmp(order, null, "System Cancelled");
				}
			}
			return true;
		}

		public bool CancelOrder(string orderId)
		{
			lock (_lockObject)
			{
				if (_filledOrders.WasFilled(orderId))
				{
					//	Order was already filled but the fill hasn't been processed by the system yet.
					//	Return true to avoid error.
					return true;
				}

				//	TODO: Perf issue here with many orders
				foreach (var kvp in openOrders)
				{
					if (kvp.Value.OrderId == orderId)
					{
						int id;
						if (!int.TryParse(orderId, out id))
						{
							lastError = "Unable to parse order id: " + orderId;
							return false;
						}

						return internalCancelOrder(id, kvp.Value);
					}
				}
				lastError = "Order not found: " + orderId;
				return false;
			}
		}

		public bool CancelAllOrders()
		{
			lock (_lockObject)
			{
				foreach (var kvp in openOrders)
				{
					int id;
					if (int.TryParse(kvp.Key, out id))
					{
						internalCancelOrder(id, kvp.Value);
					}
				}
				return true;
			}
		}

		public double GetBuyingPower()
		{
			lock (_lockObject)
			{
				return buyingPower;
			}
		}

		public double GetMargin()
		{
			lock (_lockObject)
			{
				return 0.0;
			}
		}

		public double GetShortedCash()
		{
			lock (_lockObject)
			{
				return 0.0;
			}
		}
#if RE1_1
		public List<RightEdge.Common.Order> GetOpenOrders()
		{
			return new List<RightEdge.Common.Order>(openOrders.Values);
		}
#else
		public List<RightEdge.Common.BrokerOrder> GetOpenOrders()
		{
			lock (_lockObject)
			{
				return new List<RightEdge.Common.BrokerOrder>(openOrders.Values);
			}
		}
#endif

#if RE1_1
		public RightEdge.Common.Order GetOpenOrder(string id)
		{
			RightEdge.Common.Order ret;
#else
		public RightEdge.Common.BrokerOrder GetOpenOrder(string id)
		{
			RightEdge.Common.BrokerOrder ret;
#endif
			lock (_lockObject)
			{
				if (openOrders.TryGetValue(id, out ret))
				{
					return ret;
				}
				return null;
			}
		}

		public int GetShares(Symbol symbol)
		{
			lock (_lockObject)
			{
				int shares = 0;
				if (openShares.ContainsKey(symbol))
				{
					shares = openShares[symbol];
				}

				return shares;
			}
		}

		public void AddOrderUpdatedDelegate(OrderUpdatedDelegate orderUpdated)
		{
			lock (_lockObject)
			{
				OrderUpdated += orderUpdated;
			}
		}

		public void RemoveOrderUpdatedDelegate(OrderUpdatedDelegate orderUpdated)
		{
			lock (_lockObject)
			{
				OrderUpdated -= orderUpdated;
			}
		}

		public void AddPositionAvailableDelegate(PositionAvailableDelegate positionAvailable)
		{
			lock (_lockObject)
			{
				PositionAvailable += positionAvailable;
			}
		}

		public void RemovePositionAvailableDelegate(PositionAvailableDelegate positionAvailable)
		{
			lock (_lockObject)
			{
				PositionAvailable -= positionAvailable;
			}
		}

		public bool IsLiveBroker()
		{
			return true;
		}

		#endregion
	}

	public class TWSAssetArgs
	{
		public string Symbol = string.Empty;
		//public string SecType = string.Empty;
		public SecurityType SecType = SecurityType.Undefined;
		public string Expiry = string.Empty;
		public double Strike = 0.0;
		public RightType Right = RightType.Undefined;
		public string Multiplier = string.Empty;
		public string Exchange = "SMART";
		public string PrimaryExchange = "SMART";
		public string Currency = "USD";

		public Contract ToContract()
		{
			Contract ret = new Contract(0, Symbol, SecType, Expiry, Strike, Right, Multiplier, Exchange, Currency, "", PrimaryExchange);
			return ret;
		}

		public static SecurityType GetSecurityType(AssetClass assetClass)
		{
			switch (assetClass)
			{
				case AssetClass.Stock:
					return SecurityType.Stock;
				case AssetClass.Bond:
					return SecurityType.Bond;
				case AssetClass.Forex:
					return SecurityType.Cash;
				case AssetClass.Future:
					return SecurityType.Future;
				case AssetClass.FuturesOption:
					return SecurityType.FutureOption;
				case AssetClass.Index:
					return SecurityType.Index;
				case AssetClass.Option:
					return SecurityType.Option;
				default:
					return SecurityType.Undefined;
			}
		}
		public static AssetClass GetREAssetClass(SecurityType type)
		{
			switch (type)
			{

				case SecurityType.Bond:
					return AssetClass.Bond;
				case SecurityType.Future:
					return AssetClass.Future;
				case SecurityType.FutureOption:
					return AssetClass.FuturesOption;
				case SecurityType.Index:
					return AssetClass.Index;
				case SecurityType.Option:
					return AssetClass.Option;
				case SecurityType.Stock:
					return AssetClass.Stock;
				case SecurityType.Cash:
					return AssetClass.Forex;
				case SecurityType.Bag:
				default:
					//	No corresponding type.  Return stock for now.
					return AssetClass.Stock;
			}
		}

		/// <summary>
		/// Gets an expiration string formatted for IB and only if its an asset
		/// class that wastes, otherwise this string will be empty.
		/// </summary>
		public static string GetExpiration(Symbol symbol)
		{
			string expDate = "";

			if (symbol.AssetClass == AssetClass.Future ||
				symbol.AssetClass == AssetClass.FuturesOption ||
				symbol.AssetClass == AssetClass.Option)
			{
				if (symbol.ExpirationDate != DateTime.MinValue &&
					symbol.ExpirationDate != DateTime.MaxValue)
				{
					expDate = symbol.ExpirationDate.ToString("yyyyMMdd");
				}
			}

			return expDate;
		}



		/// <summary>
		/// Returns a "valid" strike price.  In other words, it checks to make sure
		/// a) that the asset class is one that uses strike prices (i.e. options and
		/// futures options) and then it returns the configured strike price.  Otherwise
		/// it returns 0.0;
		/// </summary>
		public static double GetValidStrikePrice(Symbol symbol)
		{
			double strike = 0.0;

			if (symbol.AssetClass == AssetClass.Option ||
				symbol.AssetClass == AssetClass.FuturesOption)
			{
				strike = symbol.StrikePrice;
			}

			return strike;
		}

		public static RightType GetRightType(ContractType contract)
		{
			switch (contract)
			{
				case ContractType.Call:
					return RightType.Call;
				case ContractType.Put:
					return RightType.Put;
				default:
					return RightType.Undefined;
			}

		}

		public static string GetIBCurrency(CurrencyType currencyType)
		{
			string currency = "USD";

			switch (currencyType)
			{
				case CurrencyType.AUD:
					currency = "AUD";
					break;

				case CurrencyType.BRL:
				case CurrencyType.CNY:
				case CurrencyType.INR:
				case CurrencyType.None:
					currency = "";		// Currency types not supported by IB
					break;

				case CurrencyType.CAD:
					currency = "CAD";
					break;

				case CurrencyType.CHF:
					currency = "CHF";
					break;

				case CurrencyType.EUR:
					currency = "EUR";
					break;

				case CurrencyType.GBP:
					currency = "GBP";
					break;

				case CurrencyType.HKD:
					currency = "HKD";
					break;

				case CurrencyType.JPY:
					currency = "JPY";
					break;

				case CurrencyType.KRW:
					currency = "KRW";
					break;

				case CurrencyType.MXN:
					currency = "MXN";
					break;

				case CurrencyType.NOK:
					currency = "NOK";
					break;

				case CurrencyType.NZD:
					currency = "NZD";
					break;

				case CurrencyType.RUB:
					currency = "RUB";
					break;

				case CurrencyType.SEK:
					currency = "SEK";
					break;

				case CurrencyType.SGD:
					currency = "SGD";
					break;

				case CurrencyType.USD:
					currency = "USD";
					break;

				default:
					currency = currencyType.ToString();
					break;
			}

			return currency;
		}

		public static CurrencyType GetRECurrency(string IBCurrency)
		{
			ReturnValue<CurrencyType> ret = EnumUtil<CurrencyType>.Parse(IBCurrency);
			if (!ret.Success)
			{
				return CurrencyType.None;
			}
			else
			{
				return ret.Value;
			}
		}

		public static Symbol SymbolFromContract(Contract contract)
		{
			//	TODO: finish conversion code
			Symbol ret = new Symbol(contract.Symbol);
			ret.CurrencyType = GetRECurrency(contract.Currency);
			ret.Exchange = contract.Exchange;
			if (ret.Exchange == null)
			{
				ret.Exchange = "";
			}
			if (contract.SecurityType == SecurityType.Future ||
				contract.SecurityType == SecurityType.FutureOption ||
				contract.SecurityType == SecurityType.Option)
			{
				if (contract.Expiry != null && contract.Expiry.Length == 6)
				{
					string year = contract.Expiry.Substring(0, 4);
					string month = contract.Expiry.Substring(4, 2);
					ret.ExpirationDate = new DateTime(Convert.ToInt32(year), Convert.ToInt32(month), 1);
				}
			}
			if (contract.Right == RightType.Call)
			{
				ret.ContractType = ContractType.Call;
			}
			else if (contract.Right == RightType.Put)
			{
				ret.ContractType = ContractType.Put;
			}
			else
			{
				ret.ContractType = ContractType.NoContract;
			}

			ret.AssetClass = GetREAssetClass(contract.SecurityType);
			ret.StrikePrice = contract.Strike;
			ret.Name = contract.Symbol;
			
			return ret;
		}

		public static TWSAssetArgs Create(Symbol symbol)
		{
			TWSAssetArgs ret = new TWSAssetArgs();
			if (symbol.AssetClass == AssetClass.Forex)
			{
				ret.Symbol = GetIBCurrency(symbol.BaseCurrency);
			}
			else
			{
				ret.Symbol = symbol.Name;
			}


			ret.SecType = GetSecurityType(symbol.AssetClass);
			ret.Expiry = GetExpiration(symbol);
			ret.Strike = GetValidStrikePrice(symbol);
			ret.Right = GetRightType(symbol.ContractType);

			//if (symbol.SymbolInformation.TickSize > 0)
			//{
			//    ret.Multiplier = symbol.SymbolInformation.TickSize.ToString();
			//}
			//else
			//{
			//    ret.Multiplier = "";
			//}
			if (symbol.SymbolInformation.ContractSize > 0)
			{
				ret.Multiplier = symbol.SymbolInformation.ContractSize.ToString();
			}
			else
			{
				ret.Multiplier = "";
			}

			if (string.IsNullOrEmpty(symbol.Exchange))
			{
				ret.Exchange = "SMART";
			}
			else
			{
				ret.Exchange = symbol.Exchange;
			}

			if (ret.Exchange.ToUpper() != "SMART")
			{
				ret.PrimaryExchange = ret.Exchange;
			}

			ret.Currency = GetIBCurrency(symbol.CurrencyType);


			return ret;
		}

	}
}
