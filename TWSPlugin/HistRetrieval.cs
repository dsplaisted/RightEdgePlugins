using System;
using System.Collections.Generic;
using System.Text;
using Krs.Ats.IBNet;
using RightEdge.Common;
using System.Threading;
using System.Diagnostics;

namespace RightEdge.TWSCSharpPlugin
{
	sealed class HistRetrieval
	{
		public IBClient client;
		public TWSPlugin twsPlugin;

		public int id;
		public Symbol symbol;
		//public string TWSFreq;
		public BarSize barSize;
		//public string TWSDuration = "1 W";
		public TimeSpan Duration = new TimeSpan(7, 0, 0, 0);
		public DateTime startDate;
		public DateTime endDate;
		public DateTime cutoffDate = DateTime.MaxValue;
		public List<BarData> ret = new List<BarData>();
		public int requestCount = 0;
		public bool RTHOnly = false;
		public EventWaitHandle waitEvent;
		public bool bPaused = false;
		public BarConstructionType BarConstruction { get; set; }

		public bool Done { get; set; }

		private List<BarData> curBatchBars = new List<BarData>();

		public bool ReceivedData
		{
			get
			{
				return curBatchBars.Count > 0 || ret.Count > 0;
			}
		}


		public void SendRequest()
		{
			TWSAssetArgs args = TWSAssetArgs.Create(symbol);
			Contract contract = args.ToContract();

			HistoricalDataType dataType = HistoricalDataType.Trades;
			if (symbol.AssetClass == AssetClass.Forex)
			{
				//dataType = HistoricalDataType.MidPoint;
				contract.Exchange = "IDEALPRO";
				contract.PrimaryExchange = contract.Exchange;
			}
			//	TODO: Modify RightEdge so that bar construction won't be default by the time it gets here
			if (BarConstruction == BarConstructionType.Trades || BarConstruction == BarConstructionType.Default)
			{
				dataType = HistoricalDataType.Trades;
			}
			else if (BarConstruction == BarConstructionType.Mid)
			{
				dataType = HistoricalDataType.Midpoint;
			}
			else if (BarConstruction == BarConstructionType.Ask)
			{
				dataType = HistoricalDataType.Ask;
			}
			else if (BarConstruction == BarConstructionType.Bid)
			{
				dataType = HistoricalDataType.Bid;
			}

			id = twsPlugin.nextID++;
			client.RequestHistoricalData(id, contract, endDate, Duration, barSize, dataType, RTHOnly ? 1 : 0);
			requestCount++;
		}

		private void BatchEnded()
		{
			DateTime batchStart = DateTime.MinValue;
			DateTime batchEnd = DateTime.MinValue;

			if (curBatchBars.Count > 0)
			{
				batchStart = curBatchBars[0].BarStartTime;
				batchEnd = curBatchBars[curBatchBars.Count - 1].BarStartTime;

				//	Prevent getting multiple bars for the same date/time
				cutoffDate = curBatchBars[0].BarStartTime;

				//	Ignore partial bars that are sometimes sent
				if (curBatchBars[curBatchBars.Count - 1].BarStartTime.Second > 0)
				{
					curBatchBars.RemoveAt(curBatchBars.Count - 1);
				}
			}


			

			curBatchBars.Reverse();
			foreach (BarData bar in curBatchBars)
			{
				if (bar.BarStartTime >= startDate && bar.BarStartTime <= endDate)
				{
					ret.Add(bar);
				}
			}

			curBatchBars.Clear();


			string msg = "TWS Plugin historical data finished.  Total bars: " + ret.Count +
				"  Total Requests: " + requestCount + "   " + batchStart.ToString() + " - " + batchEnd.ToString();

			System.Diagnostics.Debug.WriteLine(msg);

			endDate = endDate - Duration;
			if (endDate >= startDate)
			{
				//SendRequest();
			}
			else
			{
				Done = true;
			}

			if (waitEvent != null)
			{
				waitEvent.Set();
			}
			else
			{
				int b = 0;
			}

		}

		private Thread lastThread = null;

		public void GotData(HistoricalDataEventArgs args)
		{
			if (Thread.CurrentThread != lastThread)
			{
				if (Thread.CurrentThread.Name == null)
				{
					Thread.CurrentThread.Name = "GotData thread";
				}
				lastThread = Thread.CurrentThread;
				Trace.WriteLine("GotData on thread: " + lastThread.ManagedThreadId.ToString("x"));
			}

			if (args.RequestId != id)
			{
				Trace.WriteLine("Got historical data for ID " + args.RequestId + " when expecting " + id + ".");
				return;
			}

			DateTime date = args.Date.ToLocalTime();
			if (date < cutoffDate)
			{
				BarData bar = new BarData();
				bar.BarStartTime = date;
				bar.Open = (double)args.Open;
				bar.Close = (double)args.Close;
				bar.High = (double)args.High;
				bar.Low = (double)args.Low;
				if (args.Volume > 0)
				{
					bar.Volume = TWSPlugin.AdjustVolume(symbol, (ulong)args.Volume);
				}
				else
				{
					bar.Volume = 0;
				}

				curBatchBars.Add(bar);

			}

			if (args.RecordNumber == args.RecordTotal - 1)
			{
				//	Done with this batch of data
				BatchEnded();
			}
		}
	}
}
