using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace RightEdge.DataStorage
{
	/// <summary>
	/// Summary description for JetDataStoreSettings.
	/// </summary>
	public class JetDataStoreSettings : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonBrowse;
		private System.Windows.Forms.TextBox textBoxDatabaseFile;
		private System.Windows.Forms.Label label1;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		#region Properties
		private string databaseFile;
		public string DatabaseFile
		{
			get
			{
				return databaseFile;
			}
			set
			{
				databaseFile = value;
			}
		}

		#endregion

		public JetDataStoreSettings()
		{
			//
			// Required for Windows Form Designer support
			//
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
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonBrowse = new System.Windows.Forms.Button();
			this.textBoxDatabaseFile = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// buttonCancel
			// 
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(232, 64);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.TabIndex = 9;
			this.buttonCancel.Text = "Cancel";
			// 
			// buttonOK
			// 
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.Location = new System.Drawing.Point(152, 64);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.TabIndex = 8;
			this.buttonOK.Text = "OK";
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			// 
			// buttonBrowse
			// 
			this.buttonBrowse.Location = new System.Drawing.Point(232, 24);
			this.buttonBrowse.Name = "buttonBrowse";
			this.buttonBrowse.TabIndex = 7;
			this.buttonBrowse.Text = "&Browse...";
			this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
			// 
			// textBoxDatabaseFile
			// 
			this.textBoxDatabaseFile.Location = new System.Drawing.Point(8, 24);
			this.textBoxDatabaseFile.Name = "textBoxDatabaseFile";
			this.textBoxDatabaseFile.Size = new System.Drawing.Size(216, 21);
			this.textBoxDatabaseFile.TabIndex = 6;
			this.textBoxDatabaseFile.Text = "";
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(8, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(100, 16);
			this.label1.TabIndex = 5;
			this.label1.Text = "&Database File:";
			// 
			// JetDataStoreSettings
			// 
			this.AcceptButton = this.buttonOK;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(314, 93);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.buttonBrowse);
			this.Controls.Add(this.textBoxDatabaseFile);
			this.Controls.Add(this.label1);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "JetDataStoreSettings";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Access Database Settings";
			this.Load += new System.EventHandler(this.JetDataStoreSettings_Load);
			this.ResumeLayout(false);

		}
		#endregion

		private void JetDataStoreSettings_Load(object sender, System.EventArgs e)
		{
			if (!string.IsNullOrEmpty(databaseFile))
			{
				textBoxDatabaseFile.Text = databaseFile;
			}
		}

		private void buttonBrowse_Click(object sender, System.EventArgs e)
		{
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.Filter = "mdb files (*.mdb)|*.mdb|All files (*.*)|*.*";

			if (dlg.ShowDialog() == DialogResult.OK)
			{
				databaseFile = dlg.FileName;
				textBoxDatabaseFile.Text = dlg.FileName;
			}
		}

		private void buttonOK_Click(object sender, System.EventArgs e)
		{
			databaseFile = textBoxDatabaseFile.Text;
		}
	}
}
