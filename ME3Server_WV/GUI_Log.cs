using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ME3Server_WV
{
    public partial class GUI_Log : Form
    {
        public GUI_Log()
        {
            InitializeComponent();
        }

        private void GUI_Log_Load(object sender, EventArgs e)
        {
            Logger.box = rtb1;
            ME3Server.Start();
        }

        private void GUI_Log_FormClosing(object sender, FormClosingEventArgs e)
        {
            ME3Server.Stop();
        }
    }
}
