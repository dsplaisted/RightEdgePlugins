using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using WriteWrapper = RightEdge.DataStorage.CloseWrapper<System.IO.BinaryWriter>;
using ReadWrapper = RightEdge.DataStorage.CloseWrapper<System.IO.BinaryReader>;
using RightEdge.Common;
using System.Diagnostics;

namespace RightEdge.DataStorage
{
	abstract class BinaryDatabase<T>
	{
		readonly int _itemSize;
		private Dictionary<string, BinaryReader> readFileHandles = new Dictionary<string, BinaryReader>();
		private Dictionary<string, BinaryWriter> writeFileHandles = new Dictionary<string, BinaryWriter>();

		private const bool KeepOpen = false;
		
		public BinaryDatabase(int itemSize)
		{
			_itemSize = itemSize;
		}

		protected abstract DateTime ReadCurrentDateTime(BinaryReader br);
		protected abstract T ReadItem(byte [] buffer, long startPos);
		protected abstract void WriteItem(BinaryWriter writer, T item);
		protected abstract DateTime GetTime(T item);

		public DateTime GetDateTimeAtIndex(string filename, long index, out long numToSkip)
		{
			using (ReadWrapper rw = GetReader(filename))
			{
				BinaryReader br = rw.Obj;
				br.BaseStream.Seek(index * _itemSize, SeekOrigin.Begin);
				DateTime dateTime = ReadCurrentDateTime(br);

				long firstIndex = SeekDate(br, dateTime) / _itemSize;
				numToSkip = index - firstIndex;
				return dateTime;
			}
		}

		public List<T> LoadItems(string filename, DateTime start, DateTime end, long maxItems, bool loadFromEnd)
		{
			List<T> itemCollection = new List<T>();

			using (ReadWrapper rw = GetReader(filename))
			{
				BinaryReader br = rw.Obj;

				if (br != null)
				{
					long seekStart;
					long seekLength;
					SeekDates(br, start, end, out seekStart, out seekLength);

					if (seekLength <= 0)
					{
						// This seems to happen if we don't find a start date
						// So basically, we have no data.
						return itemCollection;
					}

					//	Check if there is a maximum number of bars we should load
					if (maxItems > 0)
					{
						long itemsFound = (seekLength / _itemSize);
						if (itemsFound > maxItems)
						{
							if (loadFromEnd)
							{
								long skipItems = itemsFound - maxItems;
								seekStart += skipItems * _itemSize;
								seekLength = maxItems * _itemSize;
							}
							else
							{
								seekLength = maxItems * _itemSize;
							}
						}
					}

					return ReadItems(br, seekStart, seekLength);
				}
			}

			return itemCollection;
		}

		public long GetItemCount(string filename, DateTime start, DateTime end)
		{
			if (File.Exists(filename))
			{
				using (ReadWrapper rw = GetReader(filename))
				{
					BinaryReader br = rw.Obj;

					long seekStart;
					long seekLength;
					SeekDates(br, start, end, out seekStart, out seekLength);
					if (seekLength > 0)
					{
						return seekLength / _itemSize;
					}
				}
			}
			return 0;
		}

		public long SaveItems(string filename, IList<T> items)
		{
			if (items.Count == 0)
				return 0;

			DateTime lastDate = DateTime.MinValue;
			foreach (T item in items)
			{
				DateTime date = GetTime(item);
				if (date < lastDate)
				{
					throw new ArgumentException("Items must be sorted by date");
				}
				lastDate = date;
			}


			//	We are going to replace the items between the start and end dates
			DateTime start = GetTime(items[0]);
			DateTime end = GetTime(items[items.Count - 1]);

			//	Load any items after the end date
			List<T> additionalItems = LoadItems(filename, end, DateTime.MaxValue, -1, true);
			while (additionalItems.Count > 0 && GetTime(additionalItems[0]) <= end)
			{
				additionalItems.RemoveAt(0);
			}

			//	Delete all items after the start date
			long startPos;
			using (ReadWrapper readWrapper = GetReader(filename))
			{
				startPos = SeekDate(readWrapper.Obj, start);
			}
			using (WriteWrapper writeWrapper = GetWriter(filename, false))
			{
				writeWrapper.Obj.BaseStream.SetLength(startPos);
			}

			//	Append new items
			int ret = AppendItemsToFile(filename, items);

			if (additionalItems.Count > 0)
			{
				//	Append additional items
				AppendItemsToFile(filename, additionalItems);
			}

			return ret;
		}

		public long DeleteItems(string filename, DateTime start, DateTime end)
		{
			CloseReaders(filename);
			CloseWriters(filename);

			if (start == DateTime.MinValue && end == DateTime.MaxValue)
			{
				if (File.Exists(filename))
				{
					long ret = GetItemCount(filename, DateTime.MinValue, DateTime.MaxValue);
					File.Delete(filename);
					return ret;
				}
				else
				{
					return 0;
				}
			}
			else
			{
				long originalCount = GetItemCount(filename, DateTime.MinValue, DateTime.MaxValue);
				//	Load any items after the end date
				List<T> additionalItems = LoadItems(filename, end, DateTime.MaxValue, -1, true);
				while (additionalItems.Count > 0 && GetTime(additionalItems[0]) <= end)
				{
					additionalItems.RemoveAt(0);
				}

				long startPos;
				using (ReadWrapper readWrapper = GetReader(filename))
				{
					startPos = SeekDate(readWrapper.Obj, start);
				}
				using (WriteWrapper writeWrapper = GetWriter(filename, false))
				{
					writeWrapper.Obj.BaseStream.SetLength(startPos);
				}

				//	Append additional items
				AppendItemsToFile(filename, additionalItems);

				long newCount = GetItemCount(filename, DateTime.MinValue, DateTime.MaxValue);
				return originalCount - newCount;
			}
		}

		public void Flush()
		{
			foreach (BinaryReader br in readFileHandles.Values)
			{
				br.Close();
			}
			readFileHandles.Clear();

			foreach (BinaryWriter bw in writeFileHandles.Values)
			{
				//bw.Close();
				bw.Flush();
			}
		}

		private List<T> ReadItems(BinaryReader br, long startPos, long readLength)
		{
			long itemCount = readLength / _itemSize;

			byte[] buffer; // = new byte[readLength];
			br.BaseStream.Position = startPos;
			buffer = br.ReadBytes((int)readLength);
			int offset = 0;

			List<T> ret = new List<T>((int)itemCount);

			for (int index = 0; index < itemCount; index++)
			{
				ret.Add(ReadItem(buffer, offset));
				offset += _itemSize;
			}
			return ret;
		}

		/// <summary>
		/// Returns the file index of the first item on or after the specified date.
		/// </summary>
		private void SeekDates(BinaryReader br, DateTime start, DateTime end, out long seekStart, out long seekLength)
		{
			seekStart = SeekDate(br, start);
			long seekEnd = SeekDate(br, end);
			br.BaseStream.Position = seekEnd;

			//	Check to see whether to include the bar at seekEnd
			DateTime timeAtEnd = ReadCurrentDateTime(br);
			if (timeAtEnd != DateTime.MinValue && timeAtEnd <= end)
			{
				seekLength = seekEnd - seekStart + _itemSize;
			}
			else
			{
				seekLength = seekEnd - seekStart;
			}
		}

		private int AppendItemsToFile(string filename, IEnumerable<T> items)
		{
			int ret = 0;
			using (WriteWrapper ww = GetWriter(filename, false))
			{
				BinaryWriter bw = ww.Obj;

				if (bw != null)
				{
					bw.BaseStream.Seek(0, SeekOrigin.End);

					foreach (T item in items)
					{
						WriteItem(bw, item);

						ret++;
					}
				}
			}

			return ret;
		}

		private long SeekDate(BinaryReader br, DateTime date)
		{
			if (date == DateTime.MinValue)
			{
				return 0;
			}
			else if (date == DateTime.MaxValue)
			{
				return br.BaseStream.Length;
			}

			long start = 0;
			long end = (br.BaseStream.Length / _itemSize) - 1;

			while (true)
			{
				DateTime currentTime;
				if (end - start < 10)
				{
					for (long i = start; i <= end; i++)
					{
						br.BaseStream.Seek(i * _itemSize, SeekOrigin.Begin);
						currentTime = ReadCurrentDateTime(br);
						if (currentTime >= date)
						{
							return i * _itemSize;
						}
					}
					return (end + 1) * _itemSize;
				}

				long mid = (start + end) / 2;
				br.BaseStream.Seek(mid * _itemSize, SeekOrigin.Begin);
				currentTime = ReadCurrentDateTime(br);
				if (currentTime == DateTime.MinValue)
				{
					throw new RightEdgeError("Error seeking for date: " + date.ToString());
				}
				if (currentTime == date)
				{
					//	Can't return this item yet because we want to make sure we return the first occurrance of that date
					end = mid;
					//return mid * _itemSize;
				}
				else if (currentTime < date)
				{
					start = mid;
				}
				else
				{
					end = mid;
				}
			}
		}

		public void CloseReaders(string filename)
		{
			if (readFileHandles.ContainsKey(filename))
			{
				readFileHandles[filename].Close();
				readFileHandles.Remove(filename);
			}
		}

		public void CloseWriters(string filename)
		{
			if (writeFileHandles.ContainsKey(filename))
			{
				writeFileHandles[filename].Close();
				writeFileHandles.Remove(filename);
			}
		}

		private ReadWrapper GetReader(string filename)
		{
			// If the writer already has the file open
			// I believe we need to close it to let
			// the reader have it.
			CloseWriters(filename);

			if (readFileHandles.ContainsKey(filename))
			{
				Trace.WriteLine("Read handle already exists for file: " + filename);
				return new ReadWrapper(readFileHandles[filename], !KeepOpen);
			}
			else
			{
				//Trace.WriteLine("About to get a reader instance for filename: " + fileName);
				FileStream fs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
				BinaryReader br = new BinaryReader(fs);
				if (KeepOpen)
				{
					readFileHandles.Add(filename, br);
				}

				return new ReadWrapper(br, !KeepOpen);
			}
		}

		private WriteWrapper GetWriter(string filename, bool truncate)
		{
			// If the reader already has the file open
			// I believe we need to close it to let
			// the writer have it.
			CloseReaders(filename);

			if (writeFileHandles.ContainsKey(filename))
			{
				Trace.WriteLine("Write handle already exists for file: " + filename);
				return new WriteWrapper(writeFileHandles[filename], !KeepOpen);
			}
			else
			{
				FileStream fs = null;

				//Trace.WriteLine("About to get a writer instance for filename: " + fileName);
				if (truncate)
				{
					fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
				}
				else
				{
					fs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
				}
				BinaryWriter bw = new BinaryWriter(fs);
				//bw.CloseBase = !KeepOpen;
				if (KeepOpen)
				{
					writeFileHandles.Add(filename, bw);
				}
				return new WriteWrapper(bw, !KeepOpen);
			}
		}
	}

	class BarDatabase : BinaryDatabase<BarData>
	{
		public BarDatabase()
			: base(BarDataSize())
		{
		}

		protected override DateTime ReadCurrentDateTime(BinaryReader br)
		{
			if (br.BaseStream.Position != br.BaseStream.Length)
			{
				BarData bar = new BarData();
				bar.Ask = br.ReadDouble();
				bar.Bid = br.ReadDouble();
				bar.Close = br.ReadDouble();
				bar.EmptyBar = br.ReadBoolean();
				bar.High = br.ReadDouble();
				bar.Low = br.ReadDouble();
				bar.Open = br.ReadDouble();
				bar.OpenInterest = br.ReadInt32();
				double date = br.ReadDouble();
				bar.BarStartTime = DateTime.FromOADate(date);
				bar.Volume = br.ReadUInt64();

				return bar.BarStartTime;
			}

			return DateTime.MinValue;
		}

		protected override BarData ReadItem(byte[] buffer, long startPos)
		{
			BarData bar = new BarData();
			double date;
			int offset = (int) startPos;

			bar.Ask = BitConverter.ToDouble(buffer, offset);
			offset += sizeof(double);
			bar.Bid = BitConverter.ToDouble(buffer, offset);
			offset += sizeof(double);
			bar.Close = BitConverter.ToDouble(buffer, offset);
			offset += sizeof(double);
			bar.EmptyBar = BitConverter.ToBoolean(buffer, offset);
			offset += sizeof(bool);
			bar.High = BitConverter.ToDouble(buffer, offset);
			offset += sizeof(double);
			bar.Low = BitConverter.ToDouble(buffer, offset);
			offset += sizeof(double);
			bar.Open = BitConverter.ToDouble(buffer, offset);
			offset += sizeof(double);
			bar.OpenInterest = BitConverter.ToInt32(buffer, offset);
			offset += sizeof(Int32);
			date = BitConverter.ToDouble(buffer, offset);
			bar.BarStartTime = DateTime.FromOADate(date);
			offset += sizeof(double);
			bar.Volume = BitConverter.ToUInt32(buffer, offset);
			offset += sizeof(ulong);

			return bar;
		}

		protected override void WriteItem(BinaryWriter bw, BarData bar)
		{
			bw.Write(bar.Ask);
			bw.Write(bar.Bid);
			bw.Write(bar.Close);
			bw.Write(bar.EmptyBar);
			bw.Write(bar.High);
			bw.Write(bar.Low);
			bw.Write(bar.Open);
			bw.Write(bar.OpenInterest);
			bw.Write(bar.BarStartTime.ToOADate());
			bw.Write(bar.Volume);
		}

		protected override DateTime GetTime(BarData item)
		{
			return item.BarStartTime;
		}

		private static int BarDataSize()
		{
			int size = 0;
			size += sizeof(double);		// Ask
			size += sizeof(double);		// Bid
			size += sizeof(double);		// Close
			size += sizeof(bool);		// EmptyBar
			size += sizeof(double);		// High
			size += sizeof(double);		// Low
			size += sizeof(double);		// Open
			size += sizeof(int);		// OpenInterest
			size += sizeof(double);		// PriceDateTime persisted as double
			size += sizeof(ulong);		// Volume

			return size;
		}
	}

	class TickDatabase : BinaryDatabase<TickData>
	{
		public TickDatabase()
			: base(TickDataSize())
		{

		}

		protected override DateTime ReadCurrentDateTime(BinaryReader br)
		{
			if (br.BaseStream.Position != br.BaseStream.Length)
			{
				TickData tick = new TickData();
				tick.tickType = (TickType)br.ReadInt32();
				double date = br.ReadDouble();
				tick.time = DateTime.FromOADate(date);
				tick.price = br.ReadDouble();
				tick.size = br.ReadUInt64();

				return tick.time;
			}
			return DateTime.MinValue;
		}

		protected override TickData ReadItem(byte[] buffer, long startPos)
		{
			TickData tick = new TickData();
			double date;
			int offset = (int) startPos;

			tick.tickType = (TickType)BitConverter.ToInt32(buffer, offset);
			offset += sizeof(Int32);
			date = BitConverter.ToDouble(buffer, offset);
			tick.time = DateTime.FromOADate(date);
			offset += sizeof(double);
			tick.price = BitConverter.ToDouble(buffer, offset);
			offset += sizeof(double);
			tick.size = BitConverter.ToUInt64(buffer, offset);
			offset += sizeof(UInt64);

			return tick;
		}

		protected override void WriteItem(BinaryWriter bw, TickData tick)
		{
			bw.Write((int)tick.tickType);
			bw.Write(tick.time.ToOADate());
			bw.Write(tick.price);
			bw.Write(tick.size);
		}

		protected override DateTime GetTime(TickData item)
		{
			return item.time;
		}

		private static int TickDataSize()
		{
			int size = 0;
			size += sizeof(Int32);
			size += sizeof(double);
			size += sizeof(double);
			size += sizeof(UInt64);

			return size;
		}

	}


}
