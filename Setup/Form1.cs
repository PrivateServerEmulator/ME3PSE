using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Setup
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        FirstTimeSetup ftform;
        SetupForHoster shform;
        SetupForClient scform;

        private void button1_Click(object sender, EventArgs e)
        {
            if (ftform != null)
                ftform.Close();
            ftform = new FirstTimeSetup();
            ftform.Show();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ME3Server_WV.Frontend.DeactivateRedirection();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (shform != null)
                shform.Close();
            shform = new SetupForHoster();
            shform.parent = this;
            shform.Show();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (scform != null)
                scform.Close();
            scform = new SetupForClient();
            scform.Show();
        }
    }
}
