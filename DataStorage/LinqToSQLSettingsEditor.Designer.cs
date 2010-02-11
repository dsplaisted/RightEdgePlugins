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
			this.propertyGrid = new System.Windows.Forms.PropertyGrid();
			this.buttonTestConnection = new System.Windows.Forms.Button();
			this.buttonCreateDatabase = new System.Windows.Forms.Button();
			this.buttonUpgradeDatabase = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// propertyGrid
			// 
			this.propertyGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.propertyGrid.Location = new System.Drawing.Point(3, 3);
			this.propertyGrid.Name = "propertyGrid";
			this.propertyGrid.Size = new System.Drawing.Size(220, 215);
			this.propertyGrid.TabIndex = 0;
			// 
			// buttonTestConnection
			// 
			this.buttonTestConnection.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonTestConnection.Location = new System.Drawing.Point(3, 224);
			this.buttonTestConnection.Name = "buttonTestConnection";
			this.buttonTestConnection.Size = new System.Drawing.Size(108, 23);
			this.buttonTestConnection.TabIndex = 1;
			this.buttonTestConnection.Text = "Test Connection";
			this.buttonTestConnection.UseVisualStyleBackColor = true;
			this.buttonTestConnection.Click += new System.EventHandler(this.buttonTestConnection_Click);
			// 
			// buttonCreateDatabase
			// 
			this.buttonCreateDatabase.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonCreateDatabase.Location = new System.Drawing.Point(3, 253);
			this.buttonCreateDatabase.Name = "buttonCreateDatabase";
			this.buttonCreateDatabase.Size = new System.Drawing.Size(108, 23);
			this.buttonCreateDatabase.TabIndex = 2;
			this.buttonCreateDatabase.Text = "Create Database";
			this.buttonCreateDatabase.UseVisualStyleBackColor = true;
			this.buttonCreateDatabase.Click += new System.EventHandler(this.buttonCreateDatabase_Click);
			// 
			// buttonUpgradeDatabase
			// 
			this.buttonUpgradeDatabase.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonUpgradeDatabase.Location = new System.Drawing.Point(3, 282);
			this.buttonUpgradeDatabase.Name = "buttonUpgradeDatabase";
			this.buttonUpgradeDatabase.Size = new System.Drawing.Size(108, 23);
			this.buttonUpgradeDatabase.TabIndex = 3;
			this.buttonUpgradeDatabase.Text = "Upgrade Database";
			this.buttonUpgradeDatabase.UseVisualStyleBackColor = true;
			this.buttonUpgradeDatabase.Click += new System.EventHandler(this.buttonUpgradeDatabase_Click);
			// 
			// LinqToSqlSettingsEditor
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.buttonUpgradeDatabase);
			this.Controls.Add(this.buttonCreateDatabase);
			this.Controls.Add(this.buttonTestConnection);
			this.Controls.Add(this.propertyGrid);
			this.Name = "LinqToSqlSettingsEditor";
			this.Size = new System.Drawing.Size(226, 308);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.PropertyGrid propertyGrid;
		private System.Windows.Forms.Button buttonTestConnection;
		private System.Windows.Forms.Button buttonCreateDatabase;
		private System.Windows.Forms.Button buttonUpgradeDatabase;
	}
}
