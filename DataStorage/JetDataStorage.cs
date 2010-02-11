using System;
using System.Windows.Forms;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Win32;
using RightEdge.Common;
using RightEdge.Common.Internal;


namespace RightEdge.DataStorage
{
	/// <summary>
	/// Summary description for JetDataStorage.
	/// </summary>
	public class JetDataStorage : IBarDataStorage, IDisposable
	{
		private string databaseFile = "";
		private string lastError = "";
		private string connectionString = "";

		private OleDbConnection connection = null;

		public JetDataStorage()
		{
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(@"Software\Yye Software\JetDataStore");

            if (regKey != null)
            {
                databaseFile = regKey.GetValue("DatabaseFile").ToString();


                if (databaseFile.Length > 0)
                {
                    connectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + databaseFile;
                }

                regKey.Close();
            }
		}

		#region IDisposable Members
		public void Dispose()
		{
			if (connection != null)
			{
				connection.Dispose();
				connection = null;
			}
		}
		#endregion

		#region IBarDataStorage Members

		public int SaveBars(SymbolFreq symbol, List<BarData> bars)
		{
			// Moving the guts to a private member for obfuscation purposes
			return DoSaveBars(symbol, bars);
		}

		private int DoSaveBars(SymbolFreq symbol, List<BarData> bars)
		{
			if (bars.Count == 0)
			{
				return 0;
			}

			DateTime firstDate = DateTime.MaxValue;
			DateTime lastDate = DateTime.MinValue;
			foreach (BarData bar in bars)
			{
				if (bar.BarStartTime < firstDate)
				{
					firstDate = bar.BarStartTime;
				}
				if (bar.BarStartTime > lastDate)
				{
					lastDate = bar.BarStartTime;
				}
			}

			//int symbolId = GetSymbolID(symbol.Symbol, symbol.Frequency);
			string symbolId = symbol.ToUniqueId();
			//int updatedBars = 0;

			using (OleDbConnection dbConnection = new OleDbConnection(connectionString))
			{
				dbConnection.Open();

				OleDbCommand command = new OleDbCommand("DELETE FROM BarData WHERE SymbolID = '" + symbolId +
					"' AND [BarDateTime] >= @FirstDate AND [BarDateTime] <= @LastDate", dbConnection);

				OleDbParameter paramFirstDate = new OleDbParameter("@FirstDate", OleDbType.DBTimeStamp);
				paramFirstDate.Value = firstDate;
				command.Parameters.Add(paramFirstDate);

				OleDbParameter paramLastDate = new OleDbParameter("@LastDate", OleDbType.DBTimeStamp);
				paramLastDate.Value = lastDate;
				command.Parameters.Add(paramLastDate);

				command.ExecuteNonQuery();

				OleDbTransaction transaction;

				transaction = dbConnection.BeginTransaction(IsolationLevel.ReadUncommitted);

				foreach (BarData bar in bars)
				{
					InsertBar(dbConnection, transaction, bar, symbol.ToUniqueId());
				}

				transaction.Commit();

				return bars.Count;
			}
		}

		public long GetBarCount(SymbolFreq symbol, DateTime startDateTime, DateTime endDateTime)
		{
			// Moving the guts to a private member for obfuscation purposes
			return DoGetBarCount(symbol.ToUniqueId(), startDateTime, endDateTime);
		}

		private long DoGetBarCount(string symbolId, DateTime startDateTime, DateTime endDateTime)
		{
			int barCount = 0;

			OleDbConnection dbConnection = new OleDbConnection(connectionString);
			dbConnection.Open();

			string query = "SELECT COUNT(*) FROM BarData WHERE SymbolID = '" + symbolId + "'";
			if (startDateTime != DateTime.MinValue)
			{
				query += " AND BarDateTime >= " + DBDateTime(startDateTime);
			}
			if (endDateTime != DateTime.MaxValue)
			{
				query += " AND BarDateTime <= " + DBDateTime(endDateTime);
			}

			OleDbCommand command = new OleDbCommand(query, dbConnection);
			barCount = Convert.ToInt32(command.ExecuteScalar());

			if (dbConnection.State == ConnectionState.Open)
			{
				dbConnection.Close();
			}

			return barCount;
		}

		public bool DeleteBars(SymbolFreq symbol)
		{
			DoDeleteBars(symbol.ToUniqueId());

			return true;
		}

		private void DoDeleteBars(string symbolId)
		{
			OleDbConnection dbConnection = new OleDbConnection(connectionString);
			dbConnection.Open();

			OleDbCommand command = new OleDbCommand("DELETE FROM BarData WHERE SymbolID = '" + symbolId + "'", dbConnection);
			command.ExecuteNonQuery();

			if (dbConnection.State == ConnectionState.Open)
			{
				dbConnection.Close();
			}
		}

		public string CompanyName()
		{
			return Globals.Author;
		}

		/// <summary>
		/// Access doesn't accept DateTime.MinValue, so give it 1/1/1900 as the min value.
		/// </summary>
		/// <param name="date"></param>
		/// <returns></returns>
		private DateTime EnsureValidDate(DateTime date)
		{
			DateTime ret = date;

			if (date == DateTime.MinValue)
			{
				ret = new DateTime(1900, 1, 1);
			}

			return ret;
		}

		public string GetName()
		{
			return "Local Access (Jet) Database File";
		}


		public bool RequiresSetup()
		{
			return true;
		}

		public bool IsProperlyConfigured()
		{
			if (databaseFile.Length > 0)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public void DoSettings()
		{
			JetDataStoreSettings dlg = new JetDataStoreSettings();
			
			dlg.DatabaseFile = databaseFile;
			
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				try
				{
					RegistryKey regKey = Registry.CurrentUser.CreateSubKey(@"Software\Yye Software\JetDataStore");
					databaseFile = dlg.DatabaseFile;

					regKey.SetValue("DatabaseFile", databaseFile);
					regKey.Close();

					string connectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + databaseFile;
				}
				catch(Exception exception)
				{
					lastError = exception.Message;
				}
			}
		}

		public List<BarData> LoadBars(SymbolFreq symbol, DateTime startDateTime, DateTime endDateTime, int barCount, bool loadFromEnd)
		{
			// Moved guts to a private for obfuscation purposes
			return DoLoadBars(symbol, startDateTime, endDateTime, barCount, loadFromEnd);
		}


		private string DBDateTime(DateTime dateTime)
		{
			return "#" + dateTime.ToShortDateString() + " " + dateTime.ToShortTimeString() + "#";
		}

		private List<BarData> DoLoadBars(SymbolFreq symbol, DateTime startDateTime, DateTime endDateTime, int barCount, bool loadFromEnd)
		{
			List<BarData> bars = new List<BarData>();

			OleDbConnection dbConnection = new OleDbConnection(connectionString);
			dbConnection.Open();

			string query = "SELECT ";
			if (barCount > 0)
			{
				query += "TOP " + barCount + " ";
			}
			
			query += "* FROM BarData WHERE SymbolID = '" + symbol.ToUniqueId() + "'";
			if (startDateTime != DateTime.MinValue)
			{
				query += " AND BarDateTime >= " + DBDateTime(startDateTime);
			}

			if (endDateTime != DateTime.MaxValue)
			{
				query += " AND BarDateTime <= " + DBDateTime(endDateTime);
			}

			query += " ORDER BY BarDateTime ";

			if (loadFromEnd)
			{
				query += "DESC";
			}
			else
			{
				query += "ASC";
			}

			OleDbCommand command = new OleDbCommand(query, dbConnection);
			OleDbDataReader reader = command.ExecuteReader();

			while (reader.Read())
			{
				BarData bar = new BarData();
				bar.EmptyBar = Convert.ToBoolean(reader["EmptyBar"]);

				if (!bar.EmptyBar)
				{
					bar.BarStartTime = Convert.ToDateTime(reader["BarDateTime"]);
					bar.Open = Convert.ToDouble(reader["Open"]);
					bar.Close = Convert.ToDouble(reader["Close"]);
					bar.High = Convert.ToDouble(reader["High"]);
					bar.Low = Convert.ToDouble(reader["Low"]);
					bar.Bid = Convert.ToDouble(reader["Bid"]);
					bar.Ask = Convert.ToDouble(reader["Ask"]);
					bar.Volume = Convert.ToUInt64(reader["Volume"]);
					bar.OpenInterest = Convert.ToInt32(reader["OpenInterest"]);
				}

				bars.Add(bar);
			}

			if (dbConnection.State == ConnectionState.Open)
			{
				dbConnection.Close();
			}

			if (loadFromEnd)
			{
				bars.Reverse();
			}

			return bars;
		}

		public string Version()
		{
			return "1.0";
		}


		/// <summary>
		/// Inserts a new bar into the BarData table.  Does not check for the existence of
		/// the same bar.
		/// </summary>
		/// <param name="bar"></param>
		/// <param name="symbolId"></param>
		private void InsertBar(OleDbConnection dbConnection, OleDbTransaction transaction, BarData bar, string symbolId)
		{
			bar.Open = EnsureValue(bar.Open);
			bar.Close = EnsureValue(bar.Close);
			bar.High = EnsureValue(bar.High);
			bar.Low = EnsureValue(bar.Low);
			bar.Bid = EnsureValue(bar.Bid);
			bar.Ask = EnsureValue(bar.Ask);

			string insertCommand = "INSERT INTO BarData ([SymbolID], [BarDateTime], [Open], [Close], [High], [Low], [Bid], [Ask], [Volume], [OpenInterest], [EmptyBar]) VALUES ('" +
				symbolId + "', #" +
				bar.BarStartTime.ToString() + "#, " +
				bar.Open.ToString() + ", " +
				bar.Close.ToString() + ", " +
				bar.High.ToString() + ", " +
				bar.Low.ToString() + ", " +
				bar.Bid.ToString() + ", " +
				bar.Ask.ToString() + ", " +
				bar.Volume.ToString() + ", " +
				bar.OpenInterest.ToString() + ", " +
				bar.EmptyBar.ToString() + ")";

			OleDbCommand command = new OleDbCommand(insertCommand, dbConnection, transaction);
			command.ExecuteNonQuery();
		}

		/// <summary>
		/// Access data store cannot accept double.MinValue, so convert it to 0
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private double EnsureValue(double value)
		{
			double val = 0;

			if (value != double.MinValue && !double.IsNaN(value))
			{
				val = value;
			}

			return val;
		}


		public string LastError()
		{
			return lastError;
		}

		public string id()
		{
			return "{37445FA3-3D79-458d-9B72-4C976905A7A8}";
		}

		public string GetDescription()
		{
			return "Stores market data locally in a Microsoft Access (Jet) database file.  This format is recommended for higher performance systems and/or systems which require a lot of data.";
		}

		public int SaveTicks(Symbol symbol, List<TickData> ticks)
		{
			OleDbConnection dbConnection = new OleDbConnection(connectionString);
			dbConnection.Open();

			OleDbCommand deleteCommand = new OleDbCommand("DELETE FROM TickData WHERE SymbolId = '" + symbol.ToUniqueId() + "'", dbConnection);

			deleteCommand.ExecuteNonQuery();

			if (dbConnection.State == ConnectionState.Open)
			{
				dbConnection.Close();
			}

			UpdateTicks(symbol, ticks);

			return ticks.Count;
		}

		public void SaveTick(Symbol symbol, TickData tick)
		{
			OleDbConnection dbConnection = new OleDbConnection(connectionString);
			dbConnection.Open();

			InsertTick(dbConnection, null, tick, symbol.ToUniqueId());

			if (dbConnection.State == ConnectionState.Open)
			{
				dbConnection.Close();
			}
		}

		public int UpdateTicks(Symbol symbol, List<TickData> newTicks)
		{
			if (newTicks.Count == 0)
			{
				return 0;
			}

			SystemUtils.StableSort(newTicks, new TickDateComparer().Compare);
			DateTime firstDate = newTicks[0].time;
			DateTime lastDate = newTicks[newTicks.Count - 1].time;

			OleDbConnection dbConnection = new OleDbConnection(connectionString);
			dbConnection.Open();

			OleDbTransaction transaction;

			transaction = dbConnection.BeginTransaction(IsolationLevel.ReadCommitted);

			int ret = 0;

			OleDbCommand command = new OleDbCommand("DELETE FROM TickData WHERE Time >= #" + firstDate.ToString() +
				"# AND Time <= #" + lastDate.ToString() + "#");

			ret -= Convert.ToInt32(command.ExecuteScalar());

			foreach (TickData tick in newTicks)
			{
				InsertTick(dbConnection, transaction, tick, symbol.ToUniqueId());
				ret++;
			}

			transaction.Commit();

			if (dbConnection.State == ConnectionState.Open)
			{
				dbConnection.Close();
			}

			return ret;
		}

		public List<TickData> LoadTicks(Symbol symbol, DateTime startDate)
		{
			return LoadTicks(symbol, startDate, DateTime.MaxValue);
		}

		public List<TickData> LoadTicks(Symbol symbol, DateTime startDate, DateTime endDate)
		{
			List<TickData> ret = new List<TickData>();

			OleDbConnection dbConnection = new OleDbConnection(connectionString);
			dbConnection.Open();

			string query = "SELECT * FROM TickData WHERE SymbolId = '" + symbol.ToUniqueId() + "' ";
			if (startDate != DateTime.MinValue)
			{
				query += "AND Time >= #" + startDate.ToShortDateString() + " " + startDate.ToShortTimeString() + "# ";
			}

			if (endDate != DateTime.MaxValue)
			{
				query += "AND Time <= #" + endDate.ToShortDateString() + " " + endDate.ToShortTimeString() + "# ";
			}

			query += "ORDER BY Time ASC, Seconds ASC";

			OleDbCommand command = new OleDbCommand(query, dbConnection);
			OleDbDataReader reader = command.ExecuteReader();

			while (reader.Read())
			{
				TickData tick = new TickData();
				tick.time = Convert.ToDateTime(reader["Time"]);
				tick.time = tick.time.AddSeconds(Convert.ToDouble(reader["Seconds"]));
				tick.tickType = (TickType)Convert.ToInt32(reader["TickType"]);
				tick.price = Convert.ToDouble(reader["Price"]);
				tick.size = Convert.ToUInt64(reader["Size"]);

				if (tick.time >= startDate)
				{
					ret.Add(tick);
				}
			}

			if (dbConnection.State == ConnectionState.Open)
			{
				dbConnection.Close();
			}

			return ret;
		}

		public bool DeleteTicks(Symbol symbol)
		{
			OleDbConnection dbConnection = new OleDbConnection(connectionString);
			dbConnection.Open();

			OleDbCommand deleteCommand = new OleDbCommand("DELETE FROM TickData WHERE SymbolId = '" + symbol.ToUniqueId() + "'", dbConnection);

			deleteCommand.ExecuteNonQuery();

			if (dbConnection.State == ConnectionState.Open)
			{
				dbConnection.Close();
			}

			return true;
		}

		private void InsertTick(OleDbConnection dbConnection, OleDbTransaction transaction, TickData tick, string symbolId)
		{
			//DateTime
			long ticksPerSecond = 10000000;
			long ticks = tick.time.Ticks % ticksPerSecond;
			double seconds = ticks / (double)ticksPerSecond;
			string insertCommand = "INSERT INTO TickData ([SymbolId], [Time], [Seconds], [TickType], [Value], [Size]) VALUES (" +
				"'" + symbolId + "', " +
				"#" + tick.time.ToString() + "#, " +
				seconds.ToString() + ", " + 
				((int)tick.tickType).ToString() + ", " +
				tick.price.ToString() + ", " +
				tick.size.ToString() + ")";

			OleDbCommand command = new OleDbCommand(insertCommand, dbConnection, transaction);
			command.ExecuteNonQuery();
		}

		public void ForceDefaultSettings()
		{
		}

		public void Flush()
		{
		}

		#endregion

	}
}
