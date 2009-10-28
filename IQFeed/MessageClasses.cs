using System;
using System.Collections.Generic;
using System.Text;

using RightEdge.Common;
using System.Diagnostics;
using System.Globalization;

namespace IQFeed
{
	public class ServerMessage
	{
		public string Message { get; private set; }
		public ServerMessage(string message)
		{
			this.Message = message;
		}
	}

	public class FundamentalMessage
	{
		public string Message { get; private set; }
		public FundamentalMessage(string message)
		{
			this.Message = message;
		}
	}

	public class SummaryMessage
	{
		public string Message { get; private set; }
		public Level1Data Level1 { get; private set; }
		public SummaryMessage(string message)
		{
			this.Message = message;
			this.Level1 = ParseIncomingData(message);
		}

		private Level1Data ParseIncomingData(string data)
		{
			string[] items = data.Split(',');
			Level1Data level1Data = new Level1Data();
			TimeSpan tsTradeTime = new TimeSpan();
			double outVal = 0.0;
			uint uIntVal = 0;

			for (int index = 0; index < items.Length; index++)
			{
				string value = items[index];
				try
				{
					switch (index)
					{
						case 0:
							if (value.ToUpper() == "P")
							{
								level1Data.UpdateMessage = false;
							}
							else
							{
								level1Data.UpdateMessage = true;
							}
							break;

						case 1:
							level1Data.SymbolString = value;
							break;

						case 2:
							level1Data.ExchangeCode = value;
							break;

						case 3:
							if (!string.IsNullOrEmpty(value))
							{
								if (double.TryParse(value, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out outVal))
								{
									level1Data.LastPrice = outVal;
								}
							}
							break;

						case 4:
							if (!string.IsNullOrEmpty(value))
							{
								if (double.TryParse(value, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out outVal))
								{
									level1Data.TodaysChange = outVal;
								}
							}
							break;

						case 5:
							if (!string.IsNullOrEmpty(value))
							{
								if (double.TryParse(value, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out outVal))
								{
									level1Data.TodaysChangePercent = outVal;
								}
							}
							break;

						case 6:
							if (!string.IsNullOrEmpty(value))
							{
								if (uint.TryParse(value, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out uIntVal))
								{
									level1Data.TotalVolume = uIntVal;
								}
							}
							break;

						case 7:
							if (!string.IsNullOrEmpty(value))
							{
								if (uint.TryParse(value, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out uIntVal))
								{
									level1Data.LastSize = uIntVal;
								}
							}
							break;

						case 8:
							if (!string.IsNullOrEmpty(value))
							{
								if (double.TryParse(value, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out outVal))
								{
									level1Data.High = outVal;
								}
							}
							break;

						case 9:
							if (!string.IsNullOrEmpty(value))
							{
								if (double.TryParse(value, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out outVal))
								{
									level1Data.Low = outVal;
								}
							}
							break;

						case 10:
							if (!string.IsNullOrEmpty(value))
							{
								if (double.TryParse(value, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out outVal))
								{
									level1Data.Bid = outVal;
								}
							}
							break;

						case 11:
							if (!string.IsNullOrEmpty(value))
							{
								if (double.TryParse(value, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out outVal))
								{
									level1Data.Ask = outVal;
								}
							}
							break;

						case 12:
							if (!string.IsNullOrEmpty(value))
							{
								if (uint.TryParse(value, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out uIntVal))
								{
									level1Data.BidSize = (int)uIntVal;
								}
							}
							break;

						case 13:
							if (!string.IsNullOrEmpty(value))
							{
								if (uint.TryParse(value, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out uIntVal))
								{
									level1Data.AskSize = (int)uIntVal;
								}
							}
							break;

						case 14:
						case 15:
						case 16:
							// Don't care about these values
							break;
						case 17:
							if (!string.IsNullOrEmpty(value))
							{
								if (value.Length > 8)
								{
									char update = value[value.Length - 1];

									switch (update)
									{
										case 't':
											level1Data.UpdateType = UpdateType.TradeUpdate;
											break;

										case 'T':
											level1Data.UpdateType = UpdateType.ExtendedTradeUpdate;
											break;

										case 'b':
											level1Data.UpdateType = UpdateType.BidUpdate;
											break;

										case 'a':
											level1Data.UpdateType = UpdateType.AskUpdate;
											break;

										case 'o':
											level1Data.UpdateType = UpdateType.OtherUpdate;
											break;
									}
								}

								if (value.Length > 7)
								{
									tsTradeTime = TimeSpan.Parse(value.Substring(0, 8));
								}
							}
							break;

						case 18:
							if (!string.IsNullOrEmpty(value))
							{
								if (uint.TryParse(value, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out uIntVal))
								{
									level1Data.OpenInterest = (int) uIntVal;
								}
							}
							break;

						case 19:
							if (!string.IsNullOrEmpty(value))
							{
								if (double.TryParse(value, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out outVal))
								{
									level1Data.Open = outVal;
								}
							}
							break;

						case 20:
							if (!string.IsNullOrEmpty(value))
							{
								if (double.TryParse(value, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out outVal))
								{
									level1Data.Close = outVal;
								}
							}
							break;

						case 30:
							if (!string.IsNullOrEmpty(value))
							{
								DateTime tradeDate = DateTime.MinValue;

								//if (DateTime.TryParse(value, out tradeDate))
								tradeDate = DateTime.ParseExact(value, "MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture);
								{
									//tradeDate = DateTime.Parse(value);
									tradeDate = tradeDate.Add(tsTradeTime);
									level1Data.LastTradeTime = tradeDate;
								}
							}

							break;
					}
				}
				catch (FormatException ex)
				{
					string msg = "IQFeed: Error parsing value: " + value + "   Message: " + ex.Message;
					Trace.WriteLine(msg);
					Console.WriteLine(msg);
				}
			}

			return level1Data;
		}
	}

	public class LookupMessage
	{
		public string Message { get; private set; }

		public string OptionSymbol { get; private set; }
		public string ExpirationSymbol { get; private set; }
		public string RootSymbol { get; private set; }
		public int ExpirationMonth { get; private set; }
		public int ExpirationYear { get; private set; }
		public ContractType Contract { get; private set; }
		public double StrikePrice { get; private set; }
		public bool MessageEmpty { get; private set; }

		public LookupMessage(string message)
		{
			// Parse this
			string[] values = message.Split(' ');
			this.MessageEmpty = true;

			if (values.Length > 6)
			{
				this.MessageEmpty = false;
				for (int index = 0; index < 7; index++)
				{
					string value = values[index];

					switch (index)
					{
						case 0:
							this.OptionSymbol = value;
							break;

						case 1:
							this.ExpirationSymbol = value;
							break;

						case 2:
							this.RootSymbol = value;
							break;

						case 3:
							this.ExpirationMonth = ConvertToMonth(value);
							break;

						case 4:
							this.ExpirationYear = int.Parse(value, NumberFormatInfo.InvariantInfo);
							break;

						case 5:
							if (value.ToUpper() == "C")
							{
								this.Contract = ContractType.Call;
							}
							else
							{
								this.Contract = ContractType.Put;
							}
							break;

						case 6:
							this.StrikePrice = double.Parse(value, NumberFormatInfo.InvariantInfo);
							break;
					}
				}
			}
		}

		private int ConvertToMonth(string strMonth)
		{
			int month = 1;
			switch (strMonth.ToUpper())
			{
				case "JAN":
					month = 1;
					break;

				case "FEB":
					month = 2;
					break;

				case "MAR":
					month = 3;
					break;

				case "APR":
					month = 4;
					break;

				case "MAY":
					month = 5;
					break;

				case "JUN":
					month = 6;
					break;

				case "JUL":
					month = 7;
					break;

				case "AUG":
					month = 8;
					break;

				case "SEP":
					month = 9;
					break;

				case "OCT":
					month = 10;
					break;

				case "NOV":
					month = 11;
					break;

				case "DEC":
					month = 12;
					break;
			}

			return month;
		}
	}

	public class TimeMessage
	{
		public string Message { get; private set; }
		public TimeMessage(string message)
		{
			this.Message = message;
		}
	}
}
