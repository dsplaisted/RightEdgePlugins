using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace RightEdge.DataStorage
{
	/// <summary>
	/// Summary description for XMLDataStoreSettings.
	/// </summary>
	public class XMLDataStoreSettings : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBoxDirectory;
		private System.Windows.Forms.Button buttonBrowse;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		#region Properties
		private string dataDirectory;
		public string DataDirectory
		{
			get
			{
				return dataDirectory;
			}
			set
			{
				dataDirectory = value;
			}
		}

		#endregion

		public XMLDataStoreSettings()
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
			this.label1 = new System.Windows.Forms.Label();
			this.textBoxDirectory = new System.Windows.Forms.TextBox();
			this.buttonBrowse = new System.Windows.Forms.Button();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(8, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(100, 16);
			this.label1.TabIndex = 0;
			this.label1.Text = "&Directory:";
			// 
			// textBoxDirectory
			// 
			this.textBoxDirectory.Location = new System.Drawing.Point(8, 24);
			this.textBoxDirectory.Name = "textBoxDirectory";
			this.textBoxDirectory.Size = new System.Drawing.Size(216, 21);
			this.textBoxDirectory.TabIndex = 1;
			this.textBoxDirectory.Text = "";
			// 
			// buttonBrowse
			// 
			this.buttonBrowse.Location = new System.Drawing.Point(232, 24);
			this.buttonBrowse.Name = "buttonBrowse";
			this.buttonBrowse.TabIndex = 2;
			this.buttonBrowse.Text = "&Browse...";
			this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
			// 
			// buttonOK
			// 
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.Location = new System.Drawing.Point(152, 64);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.TabIndex = 3;
			this.buttonOK.Text = "OK";
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			// 
			// buttonCancel
			// 
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(232, 64);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.TabIndex = 4;
			this.buttonCancel.Text = "Cancel";
			// 
			// XMLDataStoreSettings
			// 
			this.AcceptButton = this.buttonOK;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(314, 93);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.buttonBrowse);
			this.Controls.Add(this.textBoxDirectory);
			this.Controls.Add(this.label1);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "XMLDataStoreSettings";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "XML Data Storage Setup";
			this.Load += new System.EventHandler(this.XMLDataStoreSettings_Load);
			this.ResumeLayout(false);

		}
		#endregion

		private void XMLDataStoreSettings_Load(object sender, System.EventArgs e)
		{
			if (dataDirectory.Length > 0)
			{
				textBoxDirectory.Text = dataDirectory;
			}
		}

		private void buttonBrowse_Click(object sender, System.EventArgs e)
		{
			FolderBrowserDialog dlg = new FolderBrowserDialog();

			if (dlg.ShowDialog() == DialogResult.OK)
			{
				textBoxDirectory.Text = dlg.SelectedPath;
			}
		}

		private void buttonOK_Click(object sender, System.EventArgs e)
		{
			dataDirectory = textBoxDirectory.Text;
		}
	}
}
