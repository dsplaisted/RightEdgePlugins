using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using Microsoft.Win32;
using System.IO;

using RightEdge.Common;
using RightEdge.Common.Internal;

namespace RightEdge.DataStorage
{
	/// <summary>
	/// Summary description for SQLServerStorage.
	/// </summary>
	public class SQLServerStorage : IBarDataStorage
	{
		private string lastError = "";
		private string connectionString = "";
		private string server = "";
		private string database = "";
		private string userName = "";
		private string password = "";

		private bool sqlAuth = false;

		private SqlConnection connection = null;

		public SQLServerStorage()
		{
			try
			{
				RegistryKey regKey = Registry.CurrentUser.OpenSubKey(@"Software\Yye Software\SQLServerDataStore");
				if (regKey != null)
				{
					if (regKey.GetValue("Server") != null)
					{
						server = regKey.GetValue("Server").ToString();
						database = regKey.GetValue("Database").ToString();
						userName = regKey.GetValue("LoginName").ToString();
						password = regKey.GetValue("Password").ToString();
						sqlAuth = Convert.ToBoolean(regKey.GetValue("SQLAuth"));

						if (sqlAuth)
						{
							connectionString = "Data Source=" + server + ";Initial Catalog=" + database + ";user id=" + userName + ";password=" + password + ";";
						}
						else
						{
							connectionString = "Data Source=" + server + ";Initial Catalog=" + database + ";Integrated Security=SSPI;";
						}
					}

					regKey.Close();
				}
			}
			catch (Exception exception)
			{
				// Don't really care about the exception, just ignore
				Trace.WriteLine("Exception message: " + exception.Message);
				Trace.WriteLine("Exception InnerException: " + exception.InnerException);
				Trace.WriteLine("Stack Trace follows:");
				Trace.WriteLine(exception.StackTrace);
				Trace.Flush();
				string s = exception.Message;
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
			//SqlConnection dbConnection = new SqlConnection(connectionString);

			//Guid symbol = new Guid(symbolId);
			return DoSaveBars(symbol.ToUniqueId(), bars);
		}

		private int DoSaveBars(string symbolId, List<BarData> bars)
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

			using (SqlConnection dbConnection = new SqlConnection(connectionString))
			{
				dbConnection.Open();

				//	Delete existing bars in the same date range as the bars passed in to be saved
				SqlCommand command = new SqlCommand("DELETE FROM BarData WHERE SymbolID = '" + symbolId +
					"' AND [BarDateTime] >= @FirstDate AND [BarDateTime] <= @LastDate", dbConnection);

				SqlParameter paramFirstDate = new SqlParameter("@FirstDate", SqlDbType.DateTime);
				paramFirstDate.Value = firstDate;
				command.Parameters.Add(paramFirstDate);

				SqlParameter paramLastDate = new SqlParameter("@LastDate", SqlDbType.DateTime);
				paramLastDate.Value = lastDate;
				command.Parameters.Add(paramLastDate);

				command.ExecuteNonQuery();

				SqlTransaction transaction;

				transaction = dbConnection.BeginTransaction(IsolationLevel.ReadUncommitted);

				foreach (BarData bar in bars)
				{
					InsertBar(dbConnection, transaction, bar, symbolId);
				}

				transaction.Commit();

				return bars.Count;
			}
		}

		public long GetBarCount(SymbolFreq symbol, DateTime startDateTime, DateTime endDateTime)
		{
			return DoGetBarCount(symbol.ToUniqueId(), startDateTime, endDateTime);
		}

		private long DoGetBarCount(string symbolId, DateTime startDateTime, DateTime endDateTime)
		{
			int barCount = 0;

			SqlConnection dbConnection = null;

			try
			{
				dbConnection = new SqlConnection(connectionString);
				dbConnection.Open();

				string query = "SELECT COUNT(*) FROM BarData WHERE SymbolID = '" + symbolId + "'";
				if (startDateTime != DateTime.MinValue)
				{
					query += " AND BarDateTime >= @StartDate";
				}
				if (endDateTime != DateTime.MaxValue)
				{
					query += " AND BarDateTime <= @EndDate";
				}

				SqlCommand command = new SqlCommand(query, dbConnection);
				if (startDateTime != DateTime.MinValue)
				{
					SqlParameter startDateParam = new SqlParameter("@StartDate", SqlDbType.DateTime);
					startDateParam.Value = startDateTime;
					command.Parameters.Add(startDateParam);			
				}

				if (endDateTime != DateTime.MaxValue)
				{
					SqlParameter endDateParam = new SqlParameter("@EndDate", SqlDbType.DateTime);
					endDateParam.Value = endDateTime;
					command.Parameters.Add(endDateParam);
				}

				barCount = Convert.ToInt32(command.ExecuteScalar());
			}
			finally
			{
				if (dbConnection != null)
				{
					dbConnection.Close();
				}
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
			SqlConnection dbConnection = null;

			try
			{
				dbConnection = new SqlConnection(connectionString);
				dbConnection.Open();

				SqlCommand command = new SqlCommand("DELETE FROM BarData WHERE SymbolID = '" + symbolId + "'", dbConnection);
				command.ExecuteNonQuery();
			}
			finally
			{
				if (dbConnection != null)
				{
					dbConnection.Close();
				}
			}
		}

		public string CompanyName()
		{
			return Globals.Author;
		}

		private DateTime EnsureValidDate(DateTime date)
		{
			DateTime ret = date;

			if (date == DateTime.MinValue)
			{
				ret = new DateTime(1753, 1, 1);
			}

			return ret;
		}

		public string GetName()
		{
			return "SQL Server 2000/2005 Data Store";
		}

		public bool RequiresSetup()
		{
			return true;
		}

		public bool IsProperlyConfigured()
		{
			if (server.Length == 0 ||
				database.Length == 0)
			{
				return false;
			}

			return true;
		}

		public void DoSettings()
		{
			SQLServerStoreSettings dlg = new SQLServerStoreSettings();
			dlg.Server = server;
			dlg.Database = database;
			dlg.UserName = userName;
			dlg.Password = password;
			dlg.SqlAuth = sqlAuth;

			if (dlg.ShowDialog() == DialogResult.OK)
			{
				try
				{
					server = dlg.Server;
					database = dlg.Database;
					userName = dlg.UserName;
					password = dlg.Password;
					sqlAuth = dlg.SqlAuth;
					RegistryKey regKey = Registry.CurrentUser.CreateSubKey(@"Software\Yye Software");
					RegistryKey serverKey = regKey.CreateSubKey("SQLServerDataStore", RegistryKeyPermissionCheck.ReadWriteSubTree);
					serverKey.SetValue("Server", server);
					serverKey.SetValue("Database", database);
					serverKey.SetValue("LoginName", userName);
					serverKey.SetValue("Password", password);
					if (sqlAuth)
					{
						serverKey.SetValue("SQLAuth", true);
					}
					else
					{
						serverKey.SetValue("SQLAuth", false);
					}
				}
				catch (Exception exception)
				{
					lastError = exception.Message;
				}
			}
		}

		public List<BarData> LoadBars(SymbolFreq symbol, DateTime startDateTime, DateTime endDateTime, int maxLoadBars, bool loadFromEnd)
		{
			return DoLoadBars(symbol.ToUniqueId(), startDateTime, endDateTime, maxLoadBars, loadFromEnd);
		}

		private List<BarData> DoLoadBars(string symbolId, DateTime startDateTime, DateTime endDateTime, int maxLoadBars, bool loadFromEnd)
		{
			List<BarData> bars = new List<BarData>();

			SqlConnection dbConnection = null;

			try
			{
				dbConnection = new SqlConnection(connectionString);
				dbConnection.Open();

				SqlParameter symbolParam = new SqlParameter("@SymbolID", SqlDbType.NVarChar, 50);
				symbolParam.Value = symbolId;
				string query = "SELECT ";
				if (maxLoadBars > 0)
				{
					query += "TOP " + maxLoadBars;
				}

				query += "* FROM BarData WHERE SymbolID = @SymbolID ";
				if (startDateTime != DateTime.MinValue)
				{
					query += "AND BarDateTime >= @StartDate ";
				}

				if (endDateTime != DateTime.MaxValue)
				{
					query += "AND BarDateTime <= @EndDate ";
				}

				query += "ORDER BY BarDateTime ";

				if (loadFromEnd)
				{
					query += "DESC";
				}
				else
				{
					query += "ASC";
				}

				SqlCommand command = new SqlCommand(query, dbConnection);
				command.Parameters.Add(symbolParam);

				if (startDateTime != DateTime.MinValue)
				{
					SqlParameter startDateParam = new SqlParameter("@StartDate", SqlDbType.DateTime);
					startDateParam.Value = startDateTime;
					command.Parameters.Add(startDateParam);
				}

				if (endDateTime != DateTime.MaxValue)
				{
					SqlParameter endDateParam = new SqlParameter("@EndDate", SqlDbType.DateTime);
					endDateParam.Value = endDateTime;
					command.Parameters.Add(endDateParam);
				}

				SqlDataReader reader = command.ExecuteReader();

				while (reader.Read())
				{
					BarData bar = new BarData();
					bar.EmptyBar = reader.GetBoolean(11);

					if (!bar.EmptyBar)
					{
						bar.BarStartTime = reader.GetDateTime(2);
						bar.Open = reader.GetDouble(3);
						bar.Close = reader.GetDouble(4);
						bar.High = reader.GetDouble(5);
						bar.Low = reader.GetDouble(6);
						bar.Bid = reader.GetDouble(7);
						bar.Ask = reader.GetDouble(8);
						bar.Volume = (ulong)reader.GetInt64(9);
						bar.OpenInterest = reader.GetInt32(10);
					}

					bars.Add(bar);
				}
			}
			finally
			{
				if (dbConnection != null)
				{
					dbConnection.Close();
				}
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

		private double EnsureDouble(double val)
		{
			double dVal = 0.0;

			if (!double.IsNaN(val))
			{
				dVal = val;
			}

			return dVal;
		}

		/// <summary>
		/// Inserts a new bar into the BarData table.  Does not check for the existence of
		/// the same bar.
		/// </summary>
		/// <param name="bar"></param>
		/// <param name="symbolId"></param>
		private void InsertBar(SqlConnection dbConnection, SqlTransaction transaction, BarData bar, string symbolId)
		{

			string insertCommand = "INSERT INTO BarData ([BarDataId], [SymbolID], [BarDateTime], [Open], [Close], [High], [Low], [Bid], [Ask], [Volume], [OpenInterest], [EmptyBar]) VALUES (@BarDataId,@SymbolID,@BarDateTime,@Open,@Close,@High,@Low,@Bid,@Ask,@Volume,@OpenInterest,@EmptyBar)";
			SqlCommand command = new SqlCommand(insertCommand, dbConnection, transaction);
			//File.AppendAllText("sqlserver.log", insertCommand);

			SqlParameter param = new SqlParameter("@BarDataId", SqlDbType.UniqueIdentifier);
			param.Value = Guid.NewGuid();
			command.Parameters.Add(param);

			param = new SqlParameter("@SymbolID", SqlDbType.NVarChar, 50);
			param.Value = symbolId;
			command.Parameters.Add(param);

			param = new SqlParameter("@BarDateTime", SqlDbType.DateTime);
			param.Value = bar.BarStartTime;
			command.Parameters.Add(param);

			param = new SqlParameter("@Open", SqlDbType.Float);
			param.Value = EnsureDouble(bar.Open);
			command.Parameters.Add(param);
			//File.AppendAllText("sqlserver.log", "Open: " + bar.Open.ToString());

			param = new SqlParameter("@Close", SqlDbType.Float);
			param.Value = EnsureDouble(bar.Close);
			command.Parameters.Add(param);
			//File.AppendAllText("sqlserver.log", "Close: " + bar.Close.ToString());

			param = new SqlParameter("@High", SqlDbType.Float);
			param.Value = EnsureDouble(bar.High);
			command.Parameters.Add(param);
			//File.AppendAllText("sqlserver.log", "High: " + bar.High.ToString());

			param = new SqlParameter("@Low", SqlDbType.Float);
			param.Value = EnsureDouble(bar.Low);
			command.Parameters.Add(param);
			//File.AppendAllText("sqlserver.log", "Low: " + bar.Low.ToString());

			param = new SqlParameter("@Bid", SqlDbType.Float);
			param.Value = EnsureDouble(bar.Bid);
			command.Parameters.Add(param);
			//File.AppendAllText("sqlserver.log", "Bid: " + bar.Bid.ToString());

			param = new SqlParameter("@Ask", SqlDbType.Float);
			param.Value = EnsureDouble(bar.Ask);
			command.Parameters.Add(param);
			//File.AppendAllText("sqlserver.log", "Ask: " + bar.Ask.ToString());

			param = new SqlParameter("@Volume", SqlDbType.BigInt);
			param.Value = bar.Volume;
			command.Parameters.Add(param);
			//File.AppendAllText("sqlserver.log", "Volume: " + bar.Volume.ToString());

			param = new SqlParameter("@OpenInterest", SqlDbType.Int);
			param.Value = bar.OpenInterest;
			command.Parameters.Add(param);

			param = new SqlParameter("@EmptyBar", SqlDbType.Bit);
			param.Value = bar.EmptyBar;
			command.Parameters.Add(param);

			//File.AppendAllText("sqlserver.log", "Committing to the database");
			command.ExecuteNonQuery();
		}

	
		public string LastError()
		{
			return lastError;
		}

		public string id()
		{
			return "{F52F4F4B-1937-476f-9A7C-4276BA8BEF58}";
		}

		public string GetDescription()
		{
			return "Stores market data into a Microsoft SQL Server database.  This format is recommended for the most demanding systems and data retrieval collectors.";
		}

		public int SaveTicks(Symbol symbol, List<TickData> ticks)
		{
			SqlConnection dbConnection = null;

			try
			{
				dbConnection = new SqlConnection(connectionString);
				dbConnection.Open();

				SqlCommand deleteCommand = new SqlCommand("DELETE FROM TickData WHERE SymbolId = @SymbolID", dbConnection);
				SqlParameter param = new SqlParameter("@SymbolID", SqlDbType.NVarChar);
				deleteCommand.Parameters.Add(param);

				deleteCommand.ExecuteNonQuery();

				UpdateTicks(symbol, ticks);
			}
			finally
			{
				if (dbConnection != null)
				{
					dbConnection.Close();
				}
			}

			return ticks.Count;
		}

		public void SaveTick(Symbol symbol, TickData tick)
		{
			SqlConnection dbConnection = null;
			try
			{
				dbConnection = new SqlConnection(connectionString);
				dbConnection.Open();

				InsertTick(dbConnection, null, tick, symbol.ToUniqueId());
			}
			finally
			{
				if (dbConnection != null)
				{
					dbConnection.Close();
				}
			}
		}

		public int UpdateTicks(Symbol symbol, List<TickData> newTicks)
		{
			if (newTicks.Count == 0)
			{
				return 0;
			}

			int ret = 0;
			SystemUtils.StableSort(newTicks, new TickDateComparer().Compare);
			DateTime firstDate = newTicks[0].time;
			DateTime lastDate = newTicks[newTicks.Count - 1].time;

			SqlConnection dbConnection = null;

			try
			{
				dbConnection = new SqlConnection(connectionString);
				dbConnection.Open();

				SqlTransaction transaction;

				transaction = dbConnection.BeginTransaction(IsolationLevel.ReadCommitted);

				SqlCommand command = new SqlCommand("DELETE FROM TickData WHERE Time >= @FirstDate " +
					"AND Time <= @LastDate");

				SqlParameter param = new SqlParameter("@FirstDate", SqlDbType.DateTime);
				param.Value = firstDate;
				command.Parameters.Add(param);

				param = new SqlParameter("@LastDate", SqlDbType.DateTime);
				param.Value = lastDate;

				ret -= Convert.ToInt32(command.ExecuteScalar());

				foreach (TickData tick in newTicks)
				{
					InsertTick(dbConnection, transaction, tick, symbol.ToUniqueId());
					ret++;
				}

				transaction.Commit();
			}
			finally
			{
				if (dbConnection != null)
				{
					dbConnection.Close();
				}
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

			SqlConnection dbConnection = null;

			try
			{
				dbConnection = new SqlConnection(connectionString);
				dbConnection.Open();

				string query = "SELECT * FROM TickData WHERE SymbolId = @SymbolId";

				// Can't check for DateTime.MinValue because they may ask for DateTime.MinValue + their frequency
				// in which case, the previous check would fail.  See bug ID 1135.
				if (startDate > new DateTime(1, 1, 1753))
				{
					query += " AND [Time] >= @StartDate ";
				}

				if (endDate != DateTime.MaxValue)
				{
					query += " AND [Time] <= @EndDate ";
				}

				query += " ORDER BY Time ASC, Seconds ASC";

				SqlCommand command = new SqlCommand(query, dbConnection);

                SqlParameter symbolIdParam = new SqlParameter("@SymbolID", SqlDbType.NVarChar, 50);
                symbolIdParam.Value = symbol.ToUniqueId();
                command.Parameters.Add(symbolIdParam);

				if (startDate.Year > 1)
				{
					SqlParameter param = new SqlParameter("@StartDate", SqlDbType.DateTime);
					param.Value = startDate;
					command.Parameters.Add(param);
				}

				if (endDate != DateTime.MaxValue)
				{
					SqlParameter param = new SqlParameter("@EndDate", SqlDbType.DateTime);
					param.Value = endDate;
					command.Parameters.Add(param);
				}

				SqlDataReader reader = command.ExecuteReader();

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
			}
			finally
			{
				if (dbConnection != null)
				{
					dbConnection.Close();
				}
			}

			return ret;
		}

		public bool DeleteTicks(Symbol symbol)
		{
			SqlConnection dbConnection = null;
			bool ret = true;

			try
			{
				dbConnection = new SqlConnection(connectionString);
				dbConnection.Open();

				SqlCommand deleteCommand = new SqlCommand("DELETE FROM TickData WHERE SymbolId = @SymbolID", dbConnection);
				SqlParameter param = new SqlParameter("@SymbolID", SqlDbType.NVarChar);
				deleteCommand.Parameters.Add(param);

				deleteCommand.ExecuteNonQuery();
			}
			catch (Exception exception)
			{
				lastError = exception.Message;
				ret = false;
			}
			finally
			{
				if (dbConnection != null)
				{
					dbConnection.Close();
				}
			}

			return ret;
		}

		private void InsertTick(SqlConnection dbConnection, SqlTransaction transaction, TickData tick, string symbolId)
		{
			// Looks like sometimes we got a MinValue on the date ??
			// Not sure if its us or the data provider, but maybe we should
			// handle it gracefully if it gets to this point.
			if (tick.time == DateTime.MinValue)
			{
				tick.time = DateTime.Now;
			}

			long ticksPerSecond = 10000000;
			long ticks = tick.time.Ticks % ticksPerSecond;
			double seconds = ticks / (double)ticksPerSecond;
			string insertCommand = "INSERT INTO TickData ([SymbolId], [Time], [Seconds], [TickType], [Price], [Size]) VALUES (@SymbolID, @Time, @Seconds, @TickType, @Price, @Size)";

			SqlCommand command = new SqlCommand(insertCommand, dbConnection, transaction);
			SqlParameter param = new SqlParameter("@SymbolID", SqlDbType.NVarChar, 50);
			param.Value = symbolId;
			command.Parameters.Add(param);

			param = new SqlParameter("@Time", SqlDbType.DateTime);
			param.Value = tick.time;
			command.Parameters.Add(param);

			param = new SqlParameter("@Seconds", SqlDbType.BigInt);
			param.Value = seconds;
			command.Parameters.Add(param);

			param = new SqlParameter("@TickType", SqlDbType.Int);
			param.Value = tick.tickType;
			command.Parameters.Add(param);

			param = new SqlParameter("@Price", SqlDbType.Float);
			param.Value = tick.price;
			command.Parameters.Add(param);

			param = new SqlParameter("@Size", SqlDbType.BigInt);
			param.Value = tick.size;
			command.Parameters.Add(param);

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
