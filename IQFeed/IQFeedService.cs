using System;
using System.Net;
using System.IO;
using System.Text;
using System.Globalization;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Forms;

using RightEdge.Common;

namespace IQFeed
{
	public class IQFeedService : IService, ITickRetrieval, IBarDataRetrieval
	{
		private string lastError = "";
		bool connected = false;
		bool watching = false;
		bool hadError = false;
		GotTickData tickListener = null;
		private List<Symbol> watchedSymbols = new List<Symbol>();
		private IQFeed iqFeed;
		//private ManualResetEvent connectDone = new ManualResetEvent(false);
		private Dictionary<string, Symbol> symbolMapping = new Dictionary<string, Symbol>();
		private DateTime lastGoodTickTime = DateTime.MinValue;
		private DateTime _currentTickTime;
		private bool dropLastHistBar = false;

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
			ClearError();
			watchedSymbols = symbols;

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
			symbolMapping.Clear();

			foreach (Symbol symbol in watchedSymbols)
			{
				SymbolSubscribe(symbol);
			}

			if (CheckError())
			{
				watching = true;
			}
			return CheckError();
		}

		public bool StopWatching()
		{
			ClearError();
			watching = false;

			foreach (Symbol symbol in watchedSymbols)
			{
				SymbolUnsubscribe(symbol);
			}

			return CheckError();
		}

		public IService GetService()
		{
			return this;
		}

		#endregion

		#region IService Members

		public string ServiceName()
		{
			return "IQFeed";
		}

		public string Author()
		{
			return "Yye Software";
		}

		public string Description()
		{
			return "Real-time data feed for IQFeed subscribers";
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
			return "{8876A28E-D7B4-4844-B4AE-026BA7C33248}";
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
			get
			{
				return "";
			}
			set
			{
			}
		}

		public int Port
		{
			get
			{
				return 0;
			}
			set
			{
			}
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
			get
			{
				return true;
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
				return false;
			}
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
			return null;
		}

		public bool HasCustomSettings()
		{
			return true;
		}

		public bool ShowCustomSettingsForm(ref SerializableDictionary<string, string> settings)
		{
			IQFeedSettings dlg = new IQFeedSettings();
			string ignorelast = "";

			if (settings.TryGetValue("IgnoreLastHistBar", out ignorelast))
			{
				dlg.IgnoreLastHistBar = Convert.ToBoolean(ignorelast);
			}

			if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				settings["IgnoreLastHistBar"] = dlg.IgnoreLastHistBar.ToString();
			}

			return true;
		}

		public bool Initialize(SerializableDictionary<string, string> settings)
		{
			string ignorelast;
			if (settings.TryGetValue("IgnoreLastHistBar", out ignorelast))
			{
				dropLastHistBar = Convert.ToBoolean(ignorelast);
			}

			return true;
		}

		public bool Connect(ServiceConnectOptions connectOptions)
		{
			hadError = false;

			if (connected)
				return true;

			if (iqFeed == null)
			{
				if (!CreateIQFeed())
				{
					hadError = true;
					lastError = "Error connecting to IQFeed.";
				}
				else
				{
					connected = true;
				}
			}

			return !hadError;
		}

		public bool Disconnect()
		{
			if (connected)
			{
				foreach (Symbol symbol in watchedSymbols)
				{
					SymbolUnsubscribe(symbol);
				}

				iqFeed.Disconnect();
				connected = false;
			}

			return hadError;
		}

		public string GetError()
		{
			return lastError;
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			if (iqFeed != null && connected)
			{
				iqFeed.Disconnect();
			}
		}

		#endregion

		private void SymbolSubscribe(Symbol symbol)
		{
			try
			{
				if (connected)
				{
					string symbolName = iqFeed.SymbolSubscribe(symbol);

					if (symbolName.Length > 0)
					{
						symbolMapping.Add(symbolName, symbol);
					}
				}
			}
			catch (Exception ex)
			{
				lastError = ex.Message;
				hadError = true;
			}
		}

		private void SymbolUnsubscribe(Symbol symbol)
		{
			try
			{
				if (connected)
				{
					iqFeed.SymbolUnsubscribe(symbol);
				}
			}
			catch (Exception ex)
			{
				lastError = ex.Message;
				hadError = true;
			}
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

		void iqFeed_IQTimeMessage(object sender, IQTimeEventArgs e)
		{
		}

		void iqFeed_IQUpdateMessage(object sender, IQSummaryEventArgs e)
		{
			if (!connected)
			{
				return;
			}

			TickData tick = new TickData();
			List<TickData> ticks = new List<TickData>();

			if (e.SummaryMessage.Level1.LastTradeTime == DateTime.MinValue)
			{
				Console.WriteLine("UpdateMessage DateTime.MinValue encountered for tickType " + e.SummaryMessage.Level1.UpdateType.ToString());
			}

			switch (e.SummaryMessage.Level1.UpdateType)
			{
				case UpdateType.AskUpdate:
					tick.tickType = TickType.Ask;
					tick.size = (ulong)e.SummaryMessage.Level1.AskSize;
					tick.price = e.SummaryMessage.Level1.Ask;
					tick.time = e.SummaryMessage.Level1.LastTradeTime;
					ticks.Add(tick);
					break;

				case UpdateType.BidUpdate:
					tick.tickType = TickType.Bid;
					tick.size = (ulong)e.SummaryMessage.Level1.BidSize;
					tick.price = e.SummaryMessage.Level1.Bid;
					tick.time = e.SummaryMessage.Level1.LastTradeTime;
					ticks.Add(tick);

					// I never get an "AskUpdate" it appears, so I'm going to update
					// the bid and ask here
					TickData askTick = new TickData();
					askTick.tickType = TickType.Ask;
					askTick.size = (ulong)e.SummaryMessage.Level1.AskSize;
					askTick.price = e.SummaryMessage.Level1.Ask;
					askTick.time = e.SummaryMessage.Level1.LastTradeTime;
					ticks.Add(askTick);
					break;

				case UpdateType.TradeUpdate:
					tick.tickType = TickType.Trade;
					tick.size = e.SummaryMessage.Level1.LastSize;
					tick.price = e.SummaryMessage.Level1.LastPrice;
					tick.time = e.SummaryMessage.Level1.LastTradeTime;
					ticks.Add(tick);
					break;

				default:
					break;
			}

			if (ticks.Count > 0)
			{
				Symbol symbol = symbolMapping[e.SummaryMessage.Level1.SymbolString];
				ProcessTicks(symbol, ticks);
			}
		}

		void iqFeed_IQSummaryMessage(object sender, IQSummaryEventArgs e)
		{
			if (!connected)
			{
				return;
			}

			TickData bid = new TickData();
			TickData ask = new TickData();
			List<TickData> ticks = new List<TickData>();

			bid.tickType = TickType.Bid;
			ask.tickType = TickType.Ask;

			bid.price = e.SummaryMessage.Level1.Bid;
			ask.price = e.SummaryMessage.Level1.Ask;
			bid.size = (ulong)e.SummaryMessage.Level1.BidSize;
			ask.size = (ulong)e.SummaryMessage.Level1.AskSize;

			if (e.SummaryMessage.Level1.LastTradeTime == DateTime.MinValue)
			{
				Console.WriteLine("Summary Message DateTime.MinValue encountered for tickType " + e.SummaryMessage.Level1.UpdateType.ToString());
			}

			if (e.SummaryMessage.Level1.LastTradeTime != DateTime.MinValue)
			{
				bid.time = ask.time = e.SummaryMessage.Level1.LastTradeTime;
				lastGoodTickTime = e.SummaryMessage.Level1.LastTradeTime;
			}
			else
			{
				bid.time = ask.time = lastGoodTickTime;
			}

			TickData totalVolume = new TickData();
			totalVolume.size = e.SummaryMessage.Level1.TotalVolume;
			totalVolume.time = lastGoodTickTime;
			totalVolume.tickType = TickType.DailyVolume;

			TickData lastPrice = new TickData();
			lastPrice.tickType = TickType.Trade;
			lastPrice.time = lastGoodTickTime;
			lastPrice.price = e.SummaryMessage.Level1.LastPrice;

			TickData prevClose = new TickData();
			prevClose.tickType = TickType.PreviousClose;
			prevClose.time = lastGoodTickTime;
			prevClose.price = e.SummaryMessage.Level1.LastPrice - e.SummaryMessage.Level1.TodaysChange;

			TickData open = new TickData();
			open.tickType = TickType.OpenPrice;
			open.time = lastGoodTickTime;
			open.price = e.SummaryMessage.Level1.Open;

			TickData high = new TickData();
			high.tickType = TickType.HighPrice;
			high.time = lastGoodTickTime;
			high.price = e.SummaryMessage.Level1.High;

			TickData low = new TickData();
			low.tickType = TickType.LowPrice;
			low.price = e.SummaryMessage.Level1.Low;
			low.time = lastGoodTickTime;

			ticks.Add(bid);
			ticks.Add(ask);
			ticks.Add(totalVolume);
			ticks.Add(lastPrice);
			ticks.Add(prevClose);
			ticks.Add(open);
			ticks.Add(high);
			ticks.Add(low);

			Symbol symbol = symbolMapping[e.SummaryMessage.Level1.SymbolString];
			ProcessTicks(symbol, ticks);

		}

		//void iqFeed_IQStatusChanged(object sender, IQFeedEventArgs e)
		//{
		//    if (e.IQFeedStatus == IQFeedStatusTypes.ConnectionOK)
		//    {
		//        connectDone.Set();
		//    }
		//}

		#region IBarDataRetrieval Members

		public List<BarData> RetrieveData(Symbol symbol, int frequency, DateTime startDate, DateTime endDate, BarConstructionType barConstruction)
		{
			ClearError();

			if (iqFeed == null)
			{
				if (!CreateIQFeed())
				{
					lastError = "Unable to connect to IQFeed";
					hadError = true;
					return null;
				}
			}

			ReturnValue<List<BarData>> ret = iqFeed.GetHistoricalBarData(symbol, frequency, startDate, endDate);
			if (!ret.Success)
			{
				lastError = ret.ReturnCode.Message;
				hadError = true;
				return null;
			}

			//	Filter out bars outside of the time frame we requested.
			List<BarData> bars = new List<BarData>(ret.Value.Count);
			foreach (BarData bar in ret.Value)
			{
				if (bar.BarStartTime >= startDate && bar.BarStartTime <= endDate)
				{
					bars.Add(bar);
				}
			}

			if (dropLastHistBar && bars.Count > 0)
			{
				bars.RemoveAt(bars.Count - 1);
			}

			return bars;
		}

		#endregion

		private void ProcessTicks(Symbol symbol, List<TickData> ticks)
		{
			if (tickListener != null)
			{
				foreach (TickData t in ticks)
				{
					TickData tick = t;
					if (tick.time < _currentTickTime)
					{
						//	Avoid sending out of order ticks
						tick.time = _currentTickTime;
					}
					else
					{
						_currentTickTime = tick.time;
					}
					tickListener(symbol, tick);
				}
			}
		}

		private bool CreateIQFeed()
		{
			bool success = true;

			iqFeed = new IQFeed();
			//iqFeed.IQStatusChanged += new EventHandler<IQFeedEventArgs>(iqFeed_IQStatusChanged);
			if (iqFeed.Connect())
			{
				iqFeed.IQSummaryMessage += new EventHandler<IQSummaryEventArgs>(iqFeed_IQSummaryMessage);
				iqFeed.IQUpdateMessage += new EventHandler<IQSummaryEventArgs>(iqFeed_IQUpdateMessage);
				iqFeed.IQTimeMessage += new EventHandler<IQTimeEventArgs>(iqFeed_IQTimeMessage);

				//for (int index = 0; index < 100; index++)
				//{
				//    Application.DoEvents();
				//    if (connectDone.WaitOne(100, false))
				//    {
				//        break;
				//    }
				//}
			}
			else
			{
				lastError = "Unable to connect.";
				success = false;
			}

			return success;
		}
	}
}
