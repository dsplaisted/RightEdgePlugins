using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Diagnostics;

using RightEdge.Common;

using WriteWrapper = RightEdge.DataStorage.CloseWrapper<System.IO.BinaryWriter>;
using ReadWrapper = RightEdge.DataStorage.CloseWrapper<System.IO.BinaryReader>;
using System.ComponentModel;

namespace RightEdge.DataStorage
{
	//	Old binary data store plugin ID: {CD58E46C-187B-4a96-98A9-E451C52BF587}
	[DisplayName("Local Data Store")]
	[Description("Stores bar data and symbol information on the local disk in binary format.")]
	public sealed class BinaryDataStore : IDataStore, IDisposable
	{
		private string dataDirectory = "";
		
		[DisplayName("Data Directory")]
		[Description("The path to the directory where the data files should be stored.")]
		public string DataDirectory
		{
			get { return dataDirectory; }
			set { dataDirectory = value; }
		}

		private BarDatabase _barDatabase;
		private TickDatabase _tickDatabase;
		public BinaryDataStore()
		{
			_barDatabase = new BarDatabase();
			_tickDatabase = new TickDatabase();
		}

		public void EnsureDataDir()
		{
			try
			{
				if (!string.IsNullOrEmpty(dataDirectory))
				{
					if (!Directory.Exists(dataDirectory))
					{
						Directory.CreateDirectory(dataDirectory);
					}
				}
			}
			catch (Exception e)
			{
				System.Diagnostics.Trace.WriteLine(e.Message);
				System.Diagnostics.Trace.WriteLine(e.StackTrace);
			}
		}

		private string GetFileName(SymbolFreq symbol)
		{
			EnsureDataDir();

			string validFileName = symbol.ToUniqueId();
			char[] invalidFileChars = Path.GetInvalidFileNameChars();

			foreach (char c in invalidFileChars)
			{
				validFileName = validFileName.Replace(c, '_');
			}

			return Path.Combine(dataDirectory, validFileName + ".dat");
		}

		private string GetTickFileName(Symbol symbol)
		{
			string validFileName = symbol.ToUniqueId();
			char[] invalidFileChars = Path.GetInvalidFileNameChars();

			foreach (char c in invalidFileChars)
			{
				validFileName = validFileName.Replace(c, '_');
			}

			return Path.Combine(dataDirectory, validFileName + "_tick.dat");
		}

		public void Flush()
		{
			_barDatabase.Flush();
			_tickDatabase.Flush();
		}

		public IDataAccessor<BarData> GetBarStorage(SymbolFreq symbol)
		{
			return new DataAccessor<BarData>(_barDatabase, GetFileName(symbol));
		}
		public IDataAccessor<TickData> GetTickStorage(Symbol symbol)
		{
			return new DataAccessor<TickData>(_tickDatabase, GetTickFileName(symbol));
		}
		public void FlushAll()
		{
			Flush();
		}

		public void Dispose()
		{
			Flush();
		}

		class DataAccessor<T> : IDataAccessor<T>
		{
			BinaryDatabase<T> _database;
			string _filename;

			public DataAccessor(BinaryDatabase<T> database, string filename)
			{
				_database = database;
				_filename = filename;
			}

			#region IDataAccessor<BarData> Members

			public List<T> Load(DateTime start, DateTime end, long maxItems, bool loadFromEnd)
			{
				return _database.LoadItems(_filename, start, end, maxItems, loadFromEnd);
			}

			public long GetCount(DateTime start, DateTime end)
			{
				return _database.GetItemCount(_filename, start, end);
			}

			public long Save(IList<T> items)
			{
				//using (new TimeOperation("Total save (Binary store)"))
				{
					return _database.SaveItems(_filename, items);
				}
			}

			public long Delete(DateTime start, DateTime end)
			{
				return _database.DeleteItems(_filename, start, end);
			}

			public DateTime GetDateTimeAtIndex(long index, out long numSameDatePreceding)
			{
				return _database.GetDateTimeAtIndex(_filename, index, out numSameDatePreceding);
			}

			#endregion

			#region IDisposable Members

			public void Dispose()
			{
				_database.CloseReaders(_filename);
				_database.CloseWriters(_filename);
			}

			#endregion
		}

	}

	internal sealed class CloseWrapper<T> : IDisposable where T:class,IDisposable
		{
			private T _obj = null;
			public T Obj
			{
				get { return _obj; }
			}

			public CloseWrapper(T writer, bool closeBase)
			{
				_obj = writer;
				_closeBase = closeBase;
			}

			private bool _closeBase = true;
			public bool CloseBase
			{
				get { return _closeBase; }
				//set { _closeBase = value; }
			}

			void Dispose(bool disposing)
			{
				if (disposing)
				{
					if (_closeBase)
					{
						_obj.Dispose();
					}
				}
			}




			#region IDisposable Members

			public void Dispose()
			{
				Dispose(true);
			}

			#endregion

			~CloseWrapper()
			{
				Dispose(false);
			}
		}
}
