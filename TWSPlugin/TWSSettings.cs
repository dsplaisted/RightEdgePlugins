using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Krs.Ats.IBNet;

namespace RightEdge.TWSCSharpPlugin
{
	public partial class TWSSettings : Form
	{
		private bool useRTH;
		public bool UseRTH
		{
			get
			{
				return useRTH;
			}
			set
			{
				useRTH = value;
			}
		}

		public bool IgnoreLastHistBar { get; set; }
		
		public string ClientIDBroker { get; set; }
		public string ClientIDLiveData { get; set; }
		public string ClientIDHist { get; set; }

		public string AccountCode { get; set; }
		public string FAProfile { get; set; }
		public FinancialAdvisorAllocationMethod FAMethod { get; set; }
		public string FAPercentage { get; set; }

		public TWSSettings()
		{
			InitializeComponent();
		}

		private void TWSSettings_Load(object sender, EventArgs e)
		{
			checkBoxRetrieveRTHOnly.Checked = useRTH;
			checkBoxIgnoreLastHistBar.Checked = IgnoreLastHistBar;

			textBoxClientIDBroker.Text = ClientIDBroker;
			textBoxClientIDLiveData.Text = ClientIDLiveData;
			textBoxClientIDHist.Text = ClientIDHist;

			foreach (var faValue in Enum.GetValues(typeof(FinancialAdvisorAllocationMethod)))
			{
				comboBoxFAMethod.Items.Add(faValue);
			}

			textBoxAccountCode.Text = AccountCode;
			textBoxFAProfile.Text = FAProfile;
			comboBoxFAMethod.SelectedItem = FAMethod;
			textBoxFAPercentage.Text = FAPercentage;
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			useRTH = checkBoxRetrieveRTHOnly.Checked;
			IgnoreLastHistBar = checkBoxIgnoreLastHistBar.Checked;

			ClientIDBroker = textBoxClientIDBroker.Text;
			ClientIDLiveData = textBoxClientIDLiveData.Text;
			ClientIDHist = textBoxClientIDHist.Text;

			AccountCode = textBoxAccountCode.Text;
			FAProfile = textBoxFAProfile.Text;
			FAMethod = (FinancialAdvisorAllocationMethod)comboBoxFAMethod.SelectedItem;
			FAPercentage = textBoxFAPercentage.Text;			
		}
	}
}