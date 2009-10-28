using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Reflection;
using System.Diagnostics;

using RightEdge.Common;

using Microsoft.Win32;
using RightEdge.Common.Internal;
using System.Globalization;

namespace IQFeed
{
	internal delegate void iqFeedCallBack(int value1, int value2);

	internal class IQFeedConnect : IDisposable
	{
		public static readonly IQFeedConnect Instance = new IQFeedConnect();

		public IQFeedStatusTypes IQFeedStatus { get; private set; }

		private bool _initialized = false;
		private iqFeedCallBack _myCallBack;
		private ManualResetEvent _registerDone = new ManualResetEvent(false);

		private string strName = "IQFEED_DEMO";
		private string strVersion = "1.0";	// Picked up dynamically in the constructor
		private string strKey = "1.0";


		int clientId;

		[DllImport("IQ32.dll")]
		public static extern void SetCallbackFunction(iqFeedCallBack callback);
		[DllImport("IQ32.dll")]
		public static extern int RegisterClientApp(int client, string product, string prodKey, string versions);
		[DllImport("IQ32.dll")]
		public static extern void RemoveClientApp(int client);


		private IQFeedConnect()
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			FileVersionInfo info = FileVersionInfo.GetVersionInfo(assembly.Location);
			strVersion = info.FileVersion;
		}

		public void Connect()
		{
			if (!_initialized)
			{
				_myCallBack = new iqFeedCallBack(iqFeedCallBackHandler);
				SetCallbackFunction(_myCallBack);
			}

			_registerDone.Reset();
			int ret = RegisterClientApp(clientId, strName, strKey, strVersion);
			while (!_registerDone.WaitOne(50, true))
			{
				//System.Windows.Forms.Application.DoEvents();
			}
		}

		private void iqFeedCallBackHandler(int value1, int value2)
		{
			switch (value1)
			{
				case 0:
					if (value2 == 0)
					{
						IQFeedStatus = IQFeedStatusTypes.ConnectionOK;
					}
					else if (value2 == 1)
					{
						IQFeedStatus = IQFeedStatusTypes.LoginFailed;
					}
					break;

				case 1:
					if (value2 == 0)
					{
						IQFeedStatus = IQFeedStatusTypes.Offline;
					}
					else if (value2 == 1)
					{
						IQFeedStatus = IQFeedStatusTypes.Terminating;
					}
					break;
			}
			_registerDone.Set();
		}

		#region IDisposable Members

		public void Dispose()
		{
			RemoveClientApp(clientId);
		}

		#endregion
	}

	public class IQFeed
	{
		
		//private string strName = "IQFEED_DEMO";
		private string strVersion = "1.0";	// Picked up dynamically in the constructor
		private string strKey = "1.0";
		private Socket clientSocket;
		private Socket historicalSocket;
		private Socket lookupSocket;
		private ManualResetEvent receiveDone = new ManualResetEvent(false);
		private EventWaitHandle histDoneHandle = new EventWaitHandle(true, EventResetMode.ManualReset);
		private ManualResetEvent connectDone = new ManualResetEvent(false);
		string _histRequest = "";
		private string _histError = null;
		

		private string realTimeError = null;

		private EventWaitHandle lookupDoneHandle = new EventWaitHandle(true, EventResetMode.ManualReset);
		private List<BarData> historicalBars = new List<BarData>();
		private TimeSpan _histFrequency;
		private List<LookupMessage> lookupResponse = new List<LookupMessage>();


		private bool connected = false;
		private int clientId = 0;

		//private static event EventHandler<IQFeedEventArgs> IQStatusEvent;
		//public event EventHandler<IQFeedEventArgs> IQStatusChanged;
		public event EventHandler<IQServerEventArgs> IQServerMessage;
		public event EventHandler<IQFundamentalEventArgs> IQFundamentalMessage;
		public event EventHandler<IQSummaryEventArgs> IQUpdateMessage;
		public event EventHandler<IQSummaryEventArgs> IQSummaryMessage;
		public event EventHandler<IQTimeEventArgs> IQTimeMessage;

		//private IQFeedStatusTypes iqFeedStatus;
		//public IQFeedStatusTypes IQFeedStatus
		//{
		//    get
		//    {
		//        return iqFeedStatus;
		//    }
		//}

		public IQFeed()
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			FileVersionInfo info = FileVersionInfo.GetVersionInfo(assembly.Location);
			strVersion = info.FileVersion;
			
			//iqFeedStatus = IQFeedStatusTypes.Uninitialized;
			//IQStatusEvent += new EventHandler<IQFeedEventArgs>(IQFeed_IQStatusEvent);
		}

		//private void IQFeed_IQStatusEvent(object sender, IQFeedEventArgs e)
		//{
		//    iqFeedStatus = e.IQFeedStatus;

		//    if (IQStatusChanged != null)
		//    {
		//        IQStatusChanged(this, e);
		//    }
		//}

		public bool Connect()
		{
			//IQFeedConnect.SetCallbackFunction(new iqFeedCallBack(iqFeedCallBackHandler));
			//int ret = IQFeedConnect.RegisterClientApp(clientId, strName, strKey, strVersion);

			IQFeedConnect.Instance.Connect();
			if (IQFeedConnect.Instance.IQFeedStatus != IQFeedStatusTypes.ConnectionOK)
			{
				return false;
			}

			//if (ret == 0)
			{
				RealtimeSocketConnect();
				connected = true;
				//StateObject state = new StateObject();
				//clientSocket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
				//    new AsyncCallback(ReceiveCallback), state);

				return true;
			}
			//else
			//{
			//    return false;
			//}
		}

		public bool Disconnect()
		{
			if (connected)
			{
				if (historicalSocket != null)
				{
					if (historicalSocket.Connected)
					{
						//historicalSocket.Shutdown(SocketShutdown.Both);
						//historicalSocket.Disconnect(false);
						historicalSocket.Close();
					}
				}

				if (clientSocket != null)
				{
					if (clientSocket.Connected)
					{
						//clientSocket.Shutdown(SocketShutdown.Both);
						//clientSocket.Disconnect(false);
						clientSocket.Close();
					}
				}

				IQFeedConnect.RemoveClientApp(clientId);
			}

			return true;
		}

		private void RealtimeSocketConnect()
		{
			clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			int port = GetPort();
			System.Net.IPAddress ipAdd = System.Net.IPAddress.Parse("127.0.0.1");

			IPEndPoint endPoint = new IPEndPoint(ipAdd, port);

			// Connect to the remote endpoint.
			clientSocket.Connect(endPoint);
			new SocketReader(clientSocket) { Callback = ReceiveCallback }.Begin();
		}

		public string SymbolSubscribe(Symbol symbol)
		{
			if (!connected)
			{
				return "";
			}

			if (clientSocket == null || !clientSocket.Connected)
			{
				RealtimeSocketConnect();
			}

			string symbolName = FormatSymbol(symbol);

			string request = "w" + symbolName + "\r\n";
			byte[] bytes = GetBytes(request);

			clientSocket.Send(bytes);

			return symbolName;
		}

		public void SymbolUnsubscribe(Symbol symbol)
		{
			if (!connected)
			{
				return;
			}

			if (clientSocket == null || !clientSocket.Connected)
			{
				RealtimeSocketConnect();
			}

			string symbolName = FormatSymbol(symbol);

			string request = "r" + symbolName + "\r\n";
			byte[] bytes = GetBytes(request);

			clientSocket.Send(bytes);
		}

		public string LookupSymbol(Symbol symbol)
		{
			// If an option asset type, finds the correct root symbol
			string optionSymbol = "";
			lookupResponse.Clear();

			if (!connected)
			{
				return optionSymbol;
			}

			//if (lookupSocket != null)
			//{
			//    lookupSocket.Disconnect(false);
			//    lookupSocket = null;
			//}

			if (lookupSocket == null)
			{
				lookupSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				int port = GetHistoricalPort();
				System.Net.IPAddress ipAdd = System.Net.IPAddress.Parse("127.0.0.1");

				IPEndPoint endPoint = new IPEndPoint(ipAdd, port);

				// Connect to the remote endpoint.
				lookupSocket.Connect(endPoint);
				StateObject state = new StateObject();
				lookupSocket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
					new AsyncCallback(LookupCallback), state);
			}

			string request = "";

			if (symbol.AssetClass == AssetClass.Option)
			{
				request = "SSO," + symbol.Name;
			}

			if (symbol.AssetClass == AssetClass.Future)
			{
				request = "SSF," + symbol.Name;
			}

			request += ";";

			byte[] bytes = GetBytes(request);
			lookupDoneHandle.Reset();
			lookupSocket.Send(bytes);
			lookupDoneHandle.WaitOne();
			string symbolName = symbol.Name;

			foreach (LookupMessage lookupMessage in lookupResponse)
			{
				// For now, we just handle options, I think for futures, we've
				// nailed it down without a lookup.  Although the customers will
				// likely determine otherwise.
				if (CompareLookupToSymbol(lookupMessage, symbol))
				{
					symbolName = lookupMessage.OptionSymbol + " " + lookupMessage.ExpirationSymbol;
					break;
				}
			}

			return symbolName;
		}

		/// <summary>
		/// Takes a LookupMessage object and see if it's the same as the corresponding symbol
		/// </summary>
		/// <param name="message"></param>
		/// <param name="symbol"></param>
		/// <returns>true if it matches to the best of our knowledge.  Otherwise false.</returns>
		/// <remarks>Typically used in an options lookup to match a RightEdge symbol to a
		/// symbol that's been looked up at IQFeed.</remarks>
		private bool CompareLookupToSymbol(LookupMessage message, Symbol symbol)
		{
			if (message.MessageEmpty)
			{
				return false;
			}

			if (symbol.AssetClass == AssetClass.Option)
			{
				if (message.RootSymbol == symbol.Name &&
					message.ExpirationYear == symbol.ExpirationDate.Year &&
					message.ExpirationMonth == symbol.ExpirationDate.Month &&
					message.StrikePrice == symbol.StrikePrice &&
					message.Contract == symbol.ContractType)
				{
					return true;
				}
			}

			return false;
		}

		public ReturnValue<List<BarData>> GetHistoricalBarData(Symbol symbol, int frequency, DateTime startDate, DateTime endDate)
		{
			historicalBars = new List<BarData>();
			_histRequest = "";
			//return historicalBars;

			if (!connected)
			{
				return ReturnCode.Fail("Not connected.");
			}

			_histFrequency = TimeSpan.FromMinutes(frequency);

			//if (historicalSocket != null)
			//{
			//    historicalSocket.Disconnect(false);
			//    historicalSocket = null;
			//}

			if (historicalSocket == null)
			{
				historicalSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				int port = GetHistoricalPort();
				System.Net.IPAddress ipAdd = System.Net.IPAddress.Parse("127.0.0.1");

				IPEndPoint endPoint = new IPEndPoint(ipAdd, port);

				// Connect to the remote endpoint.
				historicalSocket.Connect(endPoint);

				//StateObject state = new StateObject();
				//historicalSocket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
				//    new AsyncCallback(HistoricalCallback), state);
			}

			new SocketReader(historicalSocket) { Callback = HistoricalCallback }.Begin();

			//	IQFeed doesn't support start and end dates, just the amount of history you want going back
			//	from the current time.  So set the end date to close to the current date.
			endDate = DateTime.Now.Date.AddDays(2);

			TimeSpan ts = endDate - startDate;
			string symbolName = FormatSymbol(symbol);

			if (frequency < 1440)
			{
				_histRequest = "HM," + symbolName + "," + ts.Days.ToString() + "," + frequency.ToString();
			}
			else if (frequency == 1440)
			{
				_histRequest = "HD," + symbolName + "," + ts.Days.ToString();
			}
			else if (frequency == 10080)
			{
				int weeks = ts.Days / 7;
				_histRequest = "HW," + symbolName + "," + weeks.ToString();
			}
			else if (frequency == 43200)
			{
				int months = Months(startDate, endDate);
				_histRequest = "HN" + symbolName + "," + months.ToString();
			}
			else
			{
				return ReturnCode.Fail("Frequency not supported.");
			}

			_histRequest += ";";
			byte[] bytes = GetBytes(_histRequest);

			_histError = null;
			histDoneHandle.Reset();
			historicalSocket.Send(bytes);
			histDoneHandle.WaitOne();

			if (_histError != null)
			{
				return ReturnCode.Fail(_histError);
			}

			//	Bars are supplied most recent first by IQFeed
			historicalBars.Reverse();

			return historicalBars;
		}

		private string FormatSymbol(Symbol symbol)
		{
			// See this page for some clues on how to format futures symbols for IQFeed.
			// http://www.iqfeed.net/symbolguide/index.cfm?symbolguide=guide&displayaction=support&section=guide&web=iqfeed&guide=commod&web=IQFeed&symbolguide=guide&displayaction=support&section=guide&type=cme
			string symbolString = "";

			if (symbol.AssetClass == AssetClass.Future)
			{
				// For combined session symbols, the first character is "+".
				// For Night/Electronic sessions, the first character is "@". 
				symbolString = "@" + symbol.Name + GetMonthCode(symbol.ExpirationDate) + symbol.ExpirationDate.Year.ToString().Substring(3, 1);
			}
			else if (symbol.AssetClass == AssetClass.Option)
			{
				symbolString = LookupSymbol(symbol);
			}
			else if (symbol.AssetClass == AssetClass.Forex)
			{
				symbolString = "B" + symbol.Name;
				symbolString = symbolString.Replace("/", "");
			}
			else
			{
				symbolString = symbol.Name;
			}

			return symbolString;
		}

		private string GetMonthCode(DateTime expirationDate)
		{
			string monthCode = "";

			switch (expirationDate.Month)
			{
				case 1:
					monthCode = "F";
					break;

				case 2:
					monthCode = "G";
					break;

				case 3:
					monthCode = "H";
					break;

				case 4:
					monthCode = "J";
					break;

				case 5:
					monthCode = "K";
					break;

				case 6:
					monthCode = "M";
					break;

				case 7:
					monthCode = "N";
					break;

				case 8:
					monthCode = "Q";
					break;

				case 9:
					monthCode = "U";
					break;

				case 10:
					monthCode = "V";
					break;

				case 11:
					monthCode = "X";
					break;

				case 12:
					monthCode = "Z";
					break;
			}

			return monthCode;
		}

		private ReturnValue<BarData> ParseBar(string message)
		{
			string[] elements = message.Split(',');
			BarData bar = new BarData();

			if (elements.Length < 7)
			{
				// We got an unexpected message length
				return ReturnCode.Fail("Bad bar message: " + message);
			}

			for (int index = 0; index < 7; index++)
			{
				string value = elements[index];
				value = value.TrimEnd(new char[] {'\r', '\n', ' '});

				switch (index)
				{
					case 0:
						try
						{
							bar.BarStartTime = DateTime.ParseExact(value, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
							if (_histFrequency.TotalDays >= 1)
							{
								//	For daily bars they seem to send the correct date but the current time
								bar.BarStartTime = bar.BarStartTime.Date;
							}
							else
							{
								//	They send us the timestamp at the end of the bar.
								bar.BarStartTime = bar.BarStartTime.Subtract(_histFrequency);
							}
						}
						catch (FormatException ex)
						{
							return ReturnCode.Fail("Error parsing bar date/time: " + value);
						}
						break;

					case 1:
						bar.High = double.Parse(value, NumberFormatInfo.InvariantInfo);
						break;

					case 2:
						bar.Low = double.Parse(value, NumberFormatInfo.InvariantInfo);
						break;

					case 3:
						bar.Open = double.Parse(value, NumberFormatInfo.InvariantInfo);
						break;

					case 4:
						bar.Close = double.Parse(value, NumberFormatInfo.InvariantInfo);
						break;

					case 5:
						if (!_histRequest.StartsWith("HM"))
						{
							bar.Volume = ulong.Parse(value, NumberFormatInfo.InvariantInfo);
						}
						break;

					case 6:
						if (_histRequest.StartsWith("HM"))
						{
							bar.Volume = uint.Parse(value, NumberFormatInfo.InvariantInfo);
						}
						else
						{
							bar.OpenInterest = int.Parse(value, NumberFormatInfo.InvariantInfo);
						}
						break;
				}
			}

			return bar;
		}

		public int Months(DateTime startDate, DateTime endDate)
		{
			int months = 0;
			DateTime incMonth = startDate;

			do
			{
				months++;
				incMonth = incMonth.AddMonths(1);
			} while (startDate < endDate);

			return months;
		}

		public void ForceTimeMessage()
		{
			if (!connected)
			{
				return;
			}

			string request = "T\r\n";
			byte[] bytes = GetBytes(request);

			clientSocket.Send(bytes);
		}

		/// <summary>
		/// Returns a byte array of a specified string
		/// </summary>
		/// <param name="text">The text to go into the byte array</param>
		/// <returns>A byte array of text</returns>
		private byte[] GetBytes(string text)
		{
			return ASCIIEncoding.UTF8.GetBytes(text);
		}

		private int GetHistoricalPort()
		{
			int port = 9100;

			try
			{
				using (RegistryKey regKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\DTN\IQFEED"))
				{
					object value = regKey.GetValue("LookupPort");

					if (value != null && value.ToString().Length > 0)
					{
						port = Convert.ToInt32(value);
					}
				}
			}
			catch (Exception exception)
			{
				// Don't really care about an exception here.  If there's a problem
				// reading the registry, we'll just default to 9100.
				string m = exception.Message;
			}

			return port;
		}

		private int GetPort()
		{
			int port = 5009;

			try
			{
				using (RegistryKey regKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\DTN\IQFEED\Startup"))
				{
					object value = regKey.GetValue("Level1Port");

					if (value != null && value.ToString().Length > 0)
					{
						port = Convert.ToInt32(value);
					}
				}
			}
			catch (Exception exception)
			{
				// Don't really care about an exception here.  If there's a problem
				// reading the registry, we'll just default to 5009.
				string m = exception.Message;
			}

			return port;
		}

		//private static void iqFeedCallBackHandler(int value1, int value2)
		//{
		//    IQFeedStatusTypes status = IQFeedStatusTypes.Uninitialized;

		//    // According to the IQFeed docs
		//    // value 1     value 2   Description
		//    // 0           0         Connection OK
		//    // 0           1         Login failed
		//    // 1           0         Offline Notification
		//    // 1           1         IQFeed is terminating
		//    switch (value1)
		//    {
		//        case 0:
		//            if (value2 == 0)
		//            {
		//                status = IQFeedStatusTypes.ConnectionOK;
		//            }
		//            else if (value2 == 1)
		//            {
		//                status = IQFeedStatusTypes.LoginFailed;
		//            }
		//            break;

		//        case 1:
		//            if (value2 == 0)
		//            {
		//                status = IQFeedStatusTypes.Offline;
		//            }
		//            else if (value2 == 1)
		//            {
		//                status = IQFeedStatusTypes.Terminating;
		//            }
		//            break;
		//    }

		//    if (IQFeed.IQStatusEvent != null)
		//    {
		//        IQStatusEvent(null, new IQFeedEventArgs(status, "Startup"));
		//    }
		//}

		private void LookupCallback(IAsyncResult ar)
		{
			if (lookupSocket != null && lookupSocket.Connected)
			{
				StateObject state = (StateObject)ar.AsyncState;
				// Read data from the remote device.
				int bytesRead = lookupSocket.EndReceive(ar);
				if (bytesRead > 0)
				{
					byte[] data = new Byte[bytesRead];
					CopyTo(state.buffer, data, 0, data.Length);
					string msg = Encoding.ASCII.GetString(data, 0, data.Length);
					string[] messages = msg.Split('\n');

					foreach (string message in messages)
					{
						if (message.Contains("!ENDMSG!") || message.Contains("ERROR!"))
						{
							lookupDoneHandle.Set();
							break;
						}
						lookupResponse.Add(new LookupMessage(message));
					}

					lookupSocket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
						new AsyncCallback(LookupCallback), state);
				}
			}
		}

		private bool HistoricalCallback(string s)
		{
			try
			{
				string message = s.TrimEnd('\r', '\n');

				if (message.Contains("!ENDMSG!") || message.Contains("ERROR!"))
				{
					if (message.Contains("ERROR!"))
					{
						_histError = message;
					}
					histDoneHandle.Set();
					return false;
				}

				if (message.Trim().Length == 0)
				{
					return true;
				}

				ReturnValue<BarData> ret = ParseBar(message);

				if (!ret.Success)
				{
					_histError = ret.ReturnCode.Message;
					histDoneHandle.Set();
					return false;
				}

				historicalBars.Add(ret.Value);
				return true;
			}
			catch (Exception ex)
			{
				TraceHelper.DumpExceptionToTrace(ex);
				_histError = ex.ToString();
				histDoneHandle.Set();
				return false;
			}
		}

		private bool ReceiveCallback(string s)
		{
			s = s.TrimEnd('\r', '\n');
			string[] messages = s.Split('\n');

			try
			{
				foreach (string message in messages)
				{
					if (message.StartsWith("S,"))
					{
						if (IQServerMessage != null)
						{
							IQServerMessage(this, new IQServerEventArgs(new ServerMessage(message)));
						}
					}
					else if (message.StartsWith("T,"))
					{
						if (IQTimeMessage != null)
						{
							IQTimeMessage(this, new IQTimeEventArgs(new TimeMessage(message)));
						}
					}
					else if (message.StartsWith("F,"))
					{
						if (IQFundamentalMessage != null)
						{
							IQFundamentalMessage(this, new IQFundamentalEventArgs(new FundamentalMessage(message)));
						}
					}
					else if (message.StartsWith("Q,"))
					{
						if (IQUpdateMessage != null)
						{
							// First message back when a request for a symbol is made.
							IQUpdateMessage(this, new IQSummaryEventArgs(new SummaryMessage(message)));
						}
					}
					else if (message.StartsWith("P,"))
					{
						if (IQSummaryMessage != null)
						{
							IQSummaryMessage(this, new IQSummaryEventArgs(new SummaryMessage(message)));
						}
					}
					else
					{
						// uh oh
						Console.WriteLine("Unknown or unhandled message type: " + message);
					}
				}
				receiveDone.Set();
			}
			catch (Exception ex)
			{
				TraceHelper.DumpExceptionToTrace(ex);
				realTimeError = ex.ToString();
				receiveDone.Set();
				return true;
			}

			return true;
		}

		void CopyTo(byte[] source_bytes, byte[] destination_bytes, int start, int length)
		{
			for (int i = 0; i < length; i++)
			{
				destination_bytes[i] = source_bytes[start + i];
			}
		}
	}

	public class IQServerEventArgs : EventArgs
	{
		private ServerMessage serverMessage;
		public ServerMessage ServerMessage
		{
			get
			{
				return serverMessage;
			}
		}

		public IQServerEventArgs(ServerMessage serverMessage)
		{
			this.serverMessage = serverMessage;
		}
	}

	public class IQFundamentalEventArgs : EventArgs
	{
		public FundamentalMessage FundamentalMessage { get; private set; }
		public IQFundamentalEventArgs(FundamentalMessage message)
		{
			this.FundamentalMessage = message;
		}
	}

	public class IQSummaryEventArgs : EventArgs
	{
		public SummaryMessage SummaryMessage { get; private set; }
		public IQSummaryEventArgs(SummaryMessage message)
		{
			this.SummaryMessage = message;
		}
	}

	public class IQTimeEventArgs : EventArgs
	{
		public TimeMessage TimeMessage { get; private set; }
		public IQTimeEventArgs(TimeMessage message)
		{
			this.TimeMessage = message;
		}
	}

	public class IQFeedEventArgs : EventArgs
	{
		private IQFeedStatusTypes iqFeedStatus;
		public IQFeedStatusTypes IQFeedStatus
		{
			get
			{
				return iqFeedStatus;
			}
		}

		private string iqFeedMessage;
		public string IQFeedMessage
		{
			get
			{
				return iqFeedMessage;
			}
		}

		public IQFeedEventArgs(IQFeedStatusTypes statusType, string message)
		{
			this.iqFeedStatus = statusType;
			this.iqFeedMessage = message;
		}
	}

	public enum IQMessageType
	{
		ServerMessage,
		FundamentalMessage,
		SummaryMessage,
		UpdateMessage,
		TimeMessage
	}

	public enum IQFeedStatusTypes
	{
		Uninitialized,
		ConnectionOK,
		LoginFailed,
		Offline,
		Terminating
	}

	public class StateObject
	{
		public const int BufferSize = 8192;
		public byte[] buffer = new byte[BufferSize];
	}
}
