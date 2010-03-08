namespace RightEdge.DataStorage
{
	partial class LinqToSqlSettingsEditor
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.linkLabelTestConnection = new System.Windows.Forms.LinkLabel();
			this.linkLabelCreateDatabase = new System.Windows.Forms.LinkLabel();
			this.linkLabelUpgradeDatabase = new System.Windows.Forms.LinkLabel();
			this.SuspendLayout();
			// 
			// linkLabelTestConnection
			// 
			this.linkLabelTestConnection.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.linkLabelTestConnection.AutoSize = true;
			this.linkLabelTestConnection.Location = new System.Drawing.Point(3, 265);
			this.linkLabelTestConnection.Name = "linkLabelTestConnection";
			this.linkLabelTestConnection.Size = new System.Drawing.Size(85, 13);
			this.linkLabelTestConnection.TabIndex = 4;
			this.linkLabelTestConnection.TabStop = true;
			this.linkLabelTestConnection.Text = "Test Connection";
			this.linkLabelTestConnection.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelTestConnection_LinkClicked);
			// 
			// linkLabelCreateDatabase
			// 
			this.linkLabelCreateDatabase.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.linkLabelCreateDatabase.AutoSize = true;
			this.linkLabelCreateDatabase.Location = new System.Drawing.Point(3, 280);
			this.linkLabelCreateDatabase.Name = "linkLabelCreateDatabase";
			this.linkLabelCreateDatabase.Size = new System.Drawing.Size(87, 13);
			this.linkLabelCreateDatabase.TabIndex = 5;
			this.linkLabelCreateDatabase.TabStop = true;
			this.linkLabelCreateDatabase.Text = "Create Database";
			this.linkLabelCreateDatabase.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelCreateDatabase_LinkClicked);
			// 
			// linkLabelUpgradeDatabase
			// 
			this.linkLabelUpgradeDatabase.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.linkLabelUpgradeDatabase.AutoSize = true;
			this.linkLabelUpgradeDatabase.Location = new System.Drawing.Point(3, 295);
			this.linkLabelUpgradeDatabase.Name = "linkLabelUpgradeDatabase";
			this.linkLabelUpgradeDatabase.Size = new System.Drawing.Size(169, 13);
			this.linkLabelUpgradeDatabase.TabIndex = 6;
			this.linkLabelUpgradeDatabase.TabStop = true;
			this.linkLabelUpgradeDatabase.Text = "Upgrade Database to New Format";
			this.linkLabelUpgradeDatabase.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelUpgradeDatabase_LinkClicked);
			// 
			// LinqToSqlSettingsEditor
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.linkLabelUpgradeDatabase);
			this.Controls.Add(this.linkLabelCreateDatabase);
			this.Controls.Add(this.linkLabelTestConnection);
			this.Name = "LinqToSqlSettingsEditor";
			this.Size = new System.Drawing.Size(226, 308);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.LinkLabel linkLabelTestConnection;
		private System.Windows.Forms.LinkLabel linkLabelCreateDatabase;
		private System.Windows.Forms.LinkLabel linkLabelUpgradeDatabase;
	}
}
