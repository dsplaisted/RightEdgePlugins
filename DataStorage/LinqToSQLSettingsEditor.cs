using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RightEdge.Common;
using RightEdge.Shared;

namespace RightEdge.DataStorage
{
	public partial class LinqToSqlSettingsEditor : UserControl, IPluginEditor
	{
		ILinqToSQLStorage _settings;
		ISQLDataStoreSettingsEditor _editor;

		public LinqToSqlSettingsEditor()
		{
			InitializeComponent();

			//this.propertyGrid = new System.Windows.Forms.PropertyGrid();
			_editor = SQLDataStoreSettingsUtil.CreatePropertyGrid();

			Control propertyGrid = (Control)_editor;

			propertyGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			propertyGrid.Location = new System.Drawing.Point(3, 3);

			int top = linkLabelTestConnection.Top - 5;
			int height = top - propertyGrid.Top;

			propertyGrid.Size = new System.Drawing.Size(220, height);
			propertyGrid.TabIndex = 0;

			this.Controls.Add(propertyGrid);
		}

		#region IPluginEditor Members

		public void ShowSettings(object plugin)
		{
			_settings = ((ILinqToSQLStorage)plugin).Clone();
			//propertyGrid.SelectedObject = _settings;
			_editor.ShowSettings(_settings);
		}

		public object GetSettings()
		{
			return _settings;
		}

		#endregion

		private void linkLabelTestConnection_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
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

		private void linkLabelCreateDatabase_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
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

		private void linkLabelUpgradeDatabase_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			UpgradeDatabase();
		}

		private void UpgradeDatabase()
		{
			ReturnCode ret = _settings.UpgradeDatabase();
			if (ret.Success)
			{
				if (_settings.DatabaseSchema == DatabaseSchema.BackwardsCompatible)
				{
					_settings.DatabaseSchema = DatabaseSchema.Default;
					_editor.ShowSettings(_settings);
				}

				MessageBox.Show(this, "Database upgraded successfully.", "Success");
			}
			else
			{
				MessageBox.Show(this, ret.Message, "Error");
			}
		}
	}
}
