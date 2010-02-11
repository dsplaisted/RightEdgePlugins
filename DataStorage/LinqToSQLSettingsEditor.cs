using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RightEdge.Common;

namespace RightEdge.DataStorage
{
	public partial class LinqToSqlSettingsEditor : UserControl, IPluginEditor
	{
		ILinqToSQLStorage _settings;

		public LinqToSqlSettingsEditor()
		{
			InitializeComponent();
		}

		#region IPluginEditor Members

		public void ShowSettings(object plugin)
		{
			_settings = ((ILinqToSQLStorage)plugin).Clone();
			propertyGrid.SelectedObject = _settings;
		}

		public object GetSettings()
		{
			return _settings;
		}

		#endregion

		private void buttonTestConnection_Click(object sender, EventArgs e)
		{
			var result = _settings.TestConnection();
			if (result.Result == ConnectionResult.Succeeded)
			{
				MessageBox.Show(this, "Connection Successful");
			}
			else if (result.Result == ConnectionResult.DatabaseNeedsConversion)
			{
				var choice = MessageBox.Show(this, "The database is using an outdated database format." + Environment.NewLine +
					"Would you like to convert to the new format?", "Upgrade Database", MessageBoxButtons.YesNo);
				if (choice == DialogResult.Yes)
				{
					UpgradeDatabase();
				}
			}
			else
			{
				string message = "Connection failed!";
				if (!string.IsNullOrEmpty(result.AdditionalInformation))
				{
					message += Environment.NewLine + result.AdditionalInformation;
				}
				MessageBox.Show(this, message);
			}
		}

		private void buttonCreateDatabase_Click(object sender, EventArgs e)
		{
			ReturnCode ret = _settings.CreateDatabase();
			if (ret.Success)
			{
				MessageBox.Show(this, "Database created successfully.", "Success");
			}
			else
			{
				MessageBox.Show(this, ret.Message, "Error");
			}
		}

		private void buttonUpgradeDatabase_Click(object sender, EventArgs e)
		{
			UpgradeDatabase();
		}

		private void UpgradeDatabase()
		{
			ReturnCode ret = _settings.UpgradeDatabase();
			if (ret.Success)
			{
				MessageBox.Show(this, "Database upgraded successfully.", "Success");
			}
			else
			{
				MessageBox.Show(this, ret.Message, "Error");
			}
		}
	}
}
