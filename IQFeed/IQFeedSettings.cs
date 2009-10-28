using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace IQFeed
{
	public partial class IQFeedSettings : Form
	{
		public bool IgnoreLastHistBar { get; set; }
		public IQFeedSettings()
		{
			InitializeComponent();
		}

		private void IQFeedSettings_Load(object sender, EventArgs e)
		{
			checkBoxIgnoreLastHistBar.Checked = IgnoreLastHistBar;
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			IgnoreLastHistBar = checkBoxIgnoreLastHistBar.Checked;
		}
	}
}
