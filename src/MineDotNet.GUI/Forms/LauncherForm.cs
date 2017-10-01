using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MineDotNet.GUI.Forms
{
    public partial class LauncherForm : Form
    {
        public LauncherForm()
        {
            InitializeComponent();
        }

        private void SolvingButton_Click(object sender, EventArgs e)
        {
            new MainForm().Show();
        }

        private void BenchmarkingButton_Click(object sender, EventArgs e)
        {
            new BenchmarkingForm().Show();
        }
    }
}
