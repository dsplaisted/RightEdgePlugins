using System;
using System.Collections.Generic;
using System.Text;

namespace IQFeed
{
	public class Level1Data
	{
		public Level1Data()
		{
			updateType = UpdateType.NotSet;
		}

		private bool updateMessage;
		/// <summary>
		/// Indicates whether or not this is an update or summary message
		/// </summary>
		public bool UpdateMessage
		{
			get
			{
				return updateMessage;
			}
			set
			{
				updateMessage = value;
			}
		}

		private string symbolString;
		public string SymbolString
		{
			get
			{
				return symbolString;
			}
			set
			{
				symbolString = value;
			}
		}

		private string exchangeCode;
		public string ExchangeCode
		{
			get
			{
				return exchangeCode;
			}
			set
			{
				exchangeCode = value;
			}
		}

		private double lastPrice;
		public double LastPrice
		{
			get
			{
				return lastPrice;
			}
			set
			{
				lastPrice = value;
			}
		}

		private double todaysChange;
		public double TodaysChange
		{
			get
			{
				return todaysChange;
			}
			set
			{
				todaysChange = value;
			}
		}

		private double todaysChangePercent;
		public double TodaysChangePercent
		{
			get
			{
				return todaysChangePercent;
			}
			set
			{
				todaysChangePercent = value;
			}
		}

		private UInt32 totalVolume;
		public UInt32 TotalVolume
		{
			get
			{
				return totalVolume;
			}
			set
			{
				totalVolume = value;
			}
		}

		private UInt32 lastSize;
		public UInt32 LastSize
		{
			get
			{
				return lastSize;
			}
			set
			{
				lastSize = value;
			}
		}

		private double high;
		public double High
		{
			get
			{
				return high;
			}
			set
			{
				high = value;
			}
		}

		private double low;
		public double Low
		{
			get
			{
				return low;
			}
			set
			{
				low = value;
			}
		}

		private double bid;
		public double Bid
		{
			get
			{
				return bid;
			}
			set
			{
				bid = value;
			}
		}

		private double ask;
		public double Ask
		{
			get
			{
				return ask;
			}
			set
			{
				ask = value;
			}
		}

		private int bidSize;
		public int BidSize
		{
			get
			{
				return bidSize;
			}
			set
			{
				bidSize = value;
			}
		}

		private int askSize;
		public int AskSize
		{
			get
			{
				return askSize;
			}
			set
			{
				askSize = value;
			}
		}

		private DateTime lastTradeTime;
		public DateTime LastTradeTime
		{
			get
			{
				return lastTradeTime;
			}
			set
			{
				lastTradeTime = value;
			}
		}

		private int openInterest;
		public int OpenInterest
		{
			get
			{
				return openInterest;
			}
			set
			{
				openInterest = value;
			}
		}

		private double open;
		public double Open
		{
			get
			{
				return open;
			}
			set
			{
				open = value;
			}
		}

		private double close;
		public double Close
		{
			get
			{
				return close;
			}
			set
			{
				close = value;
			}
		}

		private UpdateType updateType;
		public UpdateType UpdateType
		{
			get
			{
				return updateType;
			}
			set
			{
				updateType = value;
			}
		}
	}

	public enum UpdateType
	{
		TradeUpdate,
		ExtendedTradeUpdate,
		BidUpdate,
		AskUpdate,
		OtherUpdate,
		NotSet
	}
}
