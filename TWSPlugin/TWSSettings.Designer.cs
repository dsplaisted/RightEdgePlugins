namespace RightEdge.TWSCSharpPlugin
{
	partial class TWSSettings
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonOK = new System.Windows.Forms.Button();
			this.checkBoxRetrieveRTHOnly = new System.Windows.Forms.CheckBox();
			this.label1 = new System.Windows.Forms.Label();
			this.textBoxAccountCode = new System.Windows.Forms.TextBox();
			this.checkBoxIgnoreLastHistBar = new System.Windows.Forms.CheckBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.textBoxFAPercentage = new System.Windows.Forms.TextBox();
			this.textBoxFAProfile = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.label8 = new System.Windows.Forms.Label();
			this.textBoxClientIDHist = new System.Windows.Forms.TextBox();
			this.textBoxClientIDLiveData = new System.Windows.Forms.TextBox();
			this.textBoxClientIDBroker = new System.Windows.Forms.TextBox();
			this.label7 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.comboBoxFAMethod = new System.Windows.Forms.ComboBox();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(194, 278);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 5;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.Location = new System.Drawing.Point(112, 278);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(75, 23);
			this.buttonOK.TabIndex = 4;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			// 
			// checkBoxRetrieveRTHOnly
			// 
			this.checkBoxRetrieveRTHOnly.Location = new System.Drawing.Point(12, 12);
			this.checkBoxRetrieveRTHOnly.Name = "checkBoxRetrieveRTHOnly";
			this.checkBoxRetrieveRTHOnly.Size = new System.Drawing.Size(238, 32);
			this.checkBoxRetrieveRTHOnly.TabIndex = 0;
			this.checkBoxRetrieveRTHOnly.Text = "&Restrict orders and downloaded bars to regular trading hours (RTH)";
			this.checkBoxRetrieveRTHOnly.UseVisualStyleBackColor = true;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(15, 16);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(94, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "FA &Account Code:";
			// 
			// textBoxAccountCode
			// 
			this.textBoxAccountCode.Location = new System.Drawing.Point(18, 32);
			this.textBoxAccountCode.Name = "textBoxAccountCode";
			this.textBoxAccountCode.Size = new System.Drawing.Size(101, 20);
			this.textBoxAccountCode.TabIndex = 1;
			// 
			// checkBoxIgnoreLastHistBar
			// 
			this.checkBoxIgnoreLastHistBar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.checkBoxIgnoreLastHistBar.AutoSize = true;
			this.checkBoxIgnoreLastHistBar.Location = new System.Drawing.Point(12, 50);
			this.checkBoxIgnoreLastHistBar.Name = "checkBoxIgnoreLastHistBar";
			this.checkBoxIgnoreLastHistBar.Size = new System.Drawing.Size(137, 17);
			this.checkBoxIgnoreLastHistBar.TabIndex = 1;
			this.checkBoxIgnoreLastHistBar.Text = "&Ignore last historical bar";
			this.checkBoxIgnoreLastHistBar.UseVisualStyleBackColor = true;
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.comboBoxFAMethod);
			this.groupBox1.Controls.Add(this.textBoxFAPercentage);
			this.groupBox1.Controls.Add(this.textBoxFAProfile);
			this.groupBox1.Controls.Add(this.textBoxAccountCode);
			this.groupBox1.Controls.Add(this.label4);
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Location = new System.Drawing.Point(12, 167);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(257, 105);
			this.groupBox1.TabIndex = 3;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Financial Advisor Account Info";
			// 
			// textBoxFAPercentage
			// 
			this.textBoxFAPercentage.Location = new System.Drawing.Point(143, 71);
			this.textBoxFAPercentage.Name = "textBoxFAPercentage";
			this.textBoxFAPercentage.Size = new System.Drawing.Size(101, 20);
			this.textBoxFAPercentage.TabIndex = 8;
			// 
			// textBoxFAProfile
			// 
			this.textBoxFAProfile.Location = new System.Drawing.Point(143, 32);
			this.textBoxFAProfile.Name = "textBoxFAProfile";
			this.textBoxFAProfile.Size = new System.Drawing.Size(101, 20);
			this.textBoxFAProfile.TabIndex = 3;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(140, 55);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(78, 13);
			this.label4.TabIndex = 7;
			this.label4.Text = "FA Per&centage";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(15, 55);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(59, 13);
			this.label3.TabIndex = 4;
			this.label3.Text = "FA &Method";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(140, 16);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(52, 13);
			this.label2.TabIndex = 2;
			this.label2.Text = "FA &Profile";
			// 
			// groupBox2
			// 
			this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox2.Controls.Add(this.label8);
			this.groupBox2.Controls.Add(this.textBoxClientIDHist);
			this.groupBox2.Controls.Add(this.textBoxClientIDLiveData);
			this.groupBox2.Controls.Add(this.textBoxClientIDBroker);
			this.groupBox2.Controls.Add(this.label7);
			this.groupBox2.Controls.Add(this.label6);
			this.groupBox2.Controls.Add(this.label5);
			this.groupBox2.Location = new System.Drawing.Point(12, 73);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(244, 88);
			this.groupBox2.TabIndex = 2;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Client IDs";
			// 
			// label8
			// 
			this.label8.Location = new System.Drawing.Point(6, 55);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(232, 30);
			this.label8.TabIndex = 6;
			this.label8.Text = "If the ID is set to -1, a random ID will be chosen each time the plugin connects." +
				"";
			// 
			// textBoxClientIDHist
			// 
			this.textBoxClientIDHist.Location = new System.Drawing.Point(180, 32);
			this.textBoxClientIDHist.Name = "textBoxClientIDHist";
			this.textBoxClientIDHist.Size = new System.Drawing.Size(58, 20);
			this.textBoxClientIDHist.TabIndex = 5;
			// 
			// textBoxClientIDLiveData
			// 
			this.textBoxClientIDLiveData.Location = new System.Drawing.Point(93, 32);
			this.textBoxClientIDLiveData.Name = "textBoxClientIDLiveData";
			this.textBoxClientIDLiveData.Size = new System.Drawing.Size(58, 20);
			this.textBoxClientIDLiveData.TabIndex = 3;
			// 
			// textBoxClientIDBroker
			// 
			this.textBoxClientIDBroker.Location = new System.Drawing.Point(6, 32);
			this.textBoxClientIDBroker.Name = "textBoxClientIDBroker";
			this.textBoxClientIDBroker.Size = new System.Drawing.Size(58, 20);
			this.textBoxClientIDBroker.TabIndex = 1;
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(177, 16);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(50, 13);
			this.label7.TabIndex = 4;
			this.label7.Text = "&Historical";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(90, 16);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(53, 13);
			this.label6.TabIndex = 2;
			this.label6.Text = "&Live Data";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(6, 16);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(38, 13);
			this.label5.TabIndex = 0;
			this.label5.Text = "&Broker";
			// 
			// comboBoxFAMethod
			// 
			this.comboBoxFAMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxFAMethod.FormattingEnabled = true;
			this.comboBoxFAMethod.Location = new System.Drawing.Point(18, 70);
			this.comboBoxFAMethod.Name = "comboBoxFAMethod";
			this.comboBoxFAMethod.Size = new System.Drawing.Size(101, 21);
			this.comboBoxFAMethod.TabIndex = 6;
			// 
			// TWSSettings
			// 
			this.AcceptButton = this.buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(281, 313);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.checkBoxIgnoreLastHistBar);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.checkBoxRetrieveRTHOnly);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "TWSSettings";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "TWS Settings";
			this.Load += new System.EventHandler(this.TWSSettings_Load);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.CheckBox checkBoxRetrieveRTHOnly;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBoxAccountCode;
		private System.Windows.Forms.CheckBox checkBoxIgnoreLastHistBar;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox textBoxFAPercentage;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox textBoxFAProfile;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.TextBox textBoxClientIDHist;
		private System.Windows.Forms.TextBox textBoxClientIDLiveData;
		private System.Windows.Forms.TextBox textBoxClientIDBroker;
		private System.Windows.Forms.ComboBox comboBoxFAMethod;
	}
}