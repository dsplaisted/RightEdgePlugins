using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

using System.Data;
using System.Data.SqlClient;

namespace RightEdge.DataStorage
{
	/// <summary>
	/// Summary description for SQLServerStoreSettings.
	/// </summary>
	public class SQLServerStoreSettings : System.Windows.Forms.Form
	{
		#region Properties
		private string server;
		public string Server
		{
			get
			{
				return server;
			}
			set
			{
				server = value;
			}
		}

		private string database;
		public string Database
		{
			get
			{
				return database;
			}
			set
			{
				database = value;
			}
		}

		private string userName;
		public string UserName
		{
			get
			{
				return userName;
			}
			set
			{
				userName = value;
			}
		}

		private string password;
		public string Password
		{
			get
			{
				return password;
			}
			set
			{
				password = value;
			}
		}

		private bool sqlAuth;
		public bool SqlAuth
		{
			get
			{
				return sqlAuth;
			}
			set
			{
				sqlAuth = value;
			}
		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBoxDatabaseServer;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.RadioButton radioButtonWindowsAuthentication;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox textBoxLoginName;
		private System.Windows.Forms.TextBox textBoxPassword;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonRefreshDatabase;
		private System.Windows.Forms.RadioButton radioButtonSqlAuthentication;
		private ComboBox comboBoxDatabaseName;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public SQLServerStoreSettings()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.label1 = new System.Windows.Forms.Label();
			this.textBoxDatabaseServer = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.textBoxPassword = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.textBoxLoginName = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.radioButtonSqlAuthentication = new System.Windows.Forms.RadioButton();
			this.radioButtonWindowsAuthentication = new System.Windows.Forms.RadioButton();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonRefreshDatabase = new System.Windows.Forms.Button();
			this.comboBoxDatabaseName = new System.Windows.Forms.ComboBox();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(8, 56);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(128, 16);
			this.label1.TabIndex = 0;
			this.label1.Text = "Database &Name:";
			// 
			// textBoxDatabaseServer
			// 
			this.textBoxDatabaseServer.Location = new System.Drawing.Point(8, 24);
			this.textBoxDatabaseServer.Name = "textBoxDatabaseServer";
			this.textBoxDatabaseServer.Size = new System.Drawing.Size(224, 21);
			this.textBoxDatabaseServer.TabIndex = 3;
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(8, 8);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(128, 16);
			this.label2.TabIndex = 2;
			this.label2.Text = "&Database Server:";
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.textBoxPassword);
			this.groupBox1.Controls.Add(this.label4);
			this.groupBox1.Controls.Add(this.textBoxLoginName);
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.radioButtonSqlAuthentication);
			this.groupBox1.Controls.Add(this.radioButtonWindowsAuthentication);
			this.groupBox1.Location = new System.Drawing.Point(8, 112);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(232, 176);
			this.groupBox1.TabIndex = 4;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Connect Using";
			// 
			// textBoxPassword
			// 
			this.textBoxPassword.Enabled = false;
			this.textBoxPassword.Location = new System.Drawing.Point(24, 136);
			this.textBoxPassword.Name = "textBoxPassword";
			this.textBoxPassword.PasswordChar = '●';
			this.textBoxPassword.Size = new System.Drawing.Size(192, 21);
			this.textBoxPassword.TabIndex = 5;
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(24, 120);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(100, 16);
			this.label4.TabIndex = 4;
			this.label4.Text = "&Password:";
			// 
			// textBoxLoginName
			// 
			this.textBoxLoginName.Enabled = false;
			this.textBoxLoginName.Location = new System.Drawing.Point(24, 88);
			this.textBoxLoginName.Name = "textBoxLoginName";
			this.textBoxLoginName.Size = new System.Drawing.Size(192, 21);
			this.textBoxLoginName.TabIndex = 3;
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(24, 72);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(100, 16);
			this.label3.TabIndex = 2;
			this.label3.Text = "&Login Name:";
			// 
			// radioButtonSqlAuthentication
			// 
			this.radioButtonSqlAuthentication.Location = new System.Drawing.Point(8, 48);
			this.radioButtonSqlAuthentication.Name = "radioButtonSqlAuthentication";
			this.radioButtonSqlAuthentication.Size = new System.Drawing.Size(208, 24);
			this.radioButtonSqlAuthentication.TabIndex = 1;
			this.radioButtonSqlAuthentication.Text = "&SQL Server Authentication";
			this.radioButtonSqlAuthentication.CheckedChanged += new System.EventHandler(this.radioButtonSqlAuthentication_CheckedChanged);
			// 
			// radioButtonWindowsAuthentication
			// 
			this.radioButtonWindowsAuthentication.Checked = true;
			this.radioButtonWindowsAuthentication.Location = new System.Drawing.Point(8, 24);
			this.radioButtonWindowsAuthentication.Name = "radioButtonWindowsAuthentication";
			this.radioButtonWindowsAuthentication.Size = new System.Drawing.Size(208, 24);
			this.radioButtonWindowsAuthentication.TabIndex = 0;
			this.radioButtonWindowsAuthentication.TabStop = true;
			this.radioButtonWindowsAuthentication.Text = "&Windows Authentication";
			this.radioButtonWindowsAuthentication.CheckedChanged += new System.EventHandler(this.radioButtonWindowsAuthentication_CheckedChanged);
			// 
			// buttonOK
			// 
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.Location = new System.Drawing.Point(168, 304);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(75, 23);
			this.buttonOK.TabIndex = 5;
			this.buttonOK.Text = "OK";
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			// 
			// buttonCancel
			// 
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(248, 304);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 6;
			this.buttonCancel.Text = "Cancel";
			// 
			// buttonRefreshDatabase
			// 
			this.buttonRefreshDatabase.Location = new System.Drawing.Point(248, 72);
			this.buttonRefreshDatabase.Name = "buttonRefreshDatabase";
			this.buttonRefreshDatabase.Size = new System.Drawing.Size(75, 23);
			this.buttonRefreshDatabase.TabIndex = 8;
			this.buttonRefreshDatabase.Text = "&Refresh";
			this.buttonRefreshDatabase.Click += new System.EventHandler(this.buttonBrowseDatabase_Click);
			// 
			// comboBoxDatabaseName
			// 
			this.comboBoxDatabaseName.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
			this.comboBoxDatabaseName.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
			this.comboBoxDatabaseName.Location = new System.Drawing.Point(8, 72);
			this.comboBoxDatabaseName.Name = "comboBoxDatabaseName";
			this.comboBoxDatabaseName.Size = new System.Drawing.Size(232, 21);
			this.comboBoxDatabaseName.TabIndex = 9;
			// 
			// SQLServerStoreSettings
			// 
			this.AcceptButton = this.buttonOK;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(330, 333);
			this.Controls.Add(this.comboBoxDatabaseName);
			this.Controls.Add(this.buttonRefreshDatabase);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.textBoxDatabaseServer);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SQLServerStoreSettings";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "SQL Server Store Settings";
			this.Load += new System.EventHandler(this.SQLServerStoreSettings_Load);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		private void SQLServerStoreSettings_Load(object sender, System.EventArgs e)
		{
			textBoxDatabaseServer.Text = server;
			textBoxLoginName.Text = userName;
			textBoxPassword.Text = password;
			comboBoxDatabaseName.Text = database;
			if (sqlAuth)
			{
				radioButtonSqlAuthentication.Checked = true;
				radioButtonWindowsAuthentication.Checked = false;
			}
			else
			{
				radioButtonSqlAuthentication.Checked = false;
				radioButtonWindowsAuthentication.Checked = true;
			}
		}

		private void buttonBrowseDatabase_Click(object sender, System.EventArgs e)
		{
			if (textBoxDatabaseServer.Text.Length == 0)
			{
				MessageBox.Show("Database server name required", "SQL Server Data Store", MessageBoxButtons.OK,
					MessageBoxIcon.Error);

				return;
			}

			SqlConnection dbConnection = null;

			try
			{
				Cursor = Cursors.WaitCursor;
				comboBoxDatabaseName.Items.Clear();

				string connectionString = "";

				if (radioButtonWindowsAuthentication.Checked)
				{
					//connectionString = "Provider=SQLOLEDB;Data Source=" + textBoxDatabaseServer.Text + ";Initial Catalog=Northwind;Integrated Security=SSPI";
					connectionString = "server=" + textBoxDatabaseServer.Text + ";Integrated Security=SSPI";
				}
				else
				{
					//connectionString = "Provider=SQLOLEDB;Data Source=" + textBoxDatabaseServer.Text + ";Initial Catalog=Northwind;User ID=" + textBoxLoginName.Text + ";Password=" + textBoxPassword.Text;
					connectionString = "server=" + textBoxDatabaseServer.Text + ";uid=" + textBoxLoginName.Text + ";pwd=" + textBoxPassword.Text;
				}

				dbConnection = new SqlConnection(connectionString);
				dbConnection.Open();

				SqlCommand sqlCom = new SqlCommand();
				sqlCom.Connection = dbConnection;
				sqlCom.CommandType = CommandType.StoredProcedure;
				sqlCom.CommandText = "sp_databases";

				SqlDataReader dbReader;
				dbReader = sqlCom.ExecuteReader(); 
				
				if (dbReader.Read())
				{
					do
					{
						comboBoxDatabaseName.Items.Add(dbReader.GetString(0));
					}
					while (dbReader.Read());
				}
			}
			catch(Exception exception)
			{
				MessageBox.Show(exception.Message, "SQL Server Data Store", MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}
			finally
			{
				Cursor = Cursors.Arrow;
				dbConnection.Close();
			}
		}

		private void radioButtonWindowsAuthentication_CheckedChanged(object sender, System.EventArgs e)
		{
			UpdateFields();
		}

		private void radioButtonSqlAuthentication_CheckedChanged(object sender, System.EventArgs e)
		{
			UpdateFields();
		}

		private void UpdateFields()
		{
			textBoxLoginName.Enabled = radioButtonSqlAuthentication.Checked;
			textBoxPassword.Enabled = radioButtonSqlAuthentication.Checked;
		}

		private void buttonOK_Click(object sender, System.EventArgs e)
		{
			server = textBoxDatabaseServer.Text;
			userName = textBoxLoginName.Text;
			password = textBoxPassword.Text;
			database = comboBoxDatabaseName.Text;
			sqlAuth = radioButtonSqlAuthentication.Checked;
		}
	}
}
