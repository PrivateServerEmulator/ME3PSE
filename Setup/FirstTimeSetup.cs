using System;
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
    public partial class FirstTimeSetup : Form
    {
        public FirstTimeSetup()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ME3Server_WV.GUI_ProfileCreator gui = new ME3Server_WV.GUI_ProfileCreator();
            gui.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ME3Server_WV.Frontend.PatchGame();
        }
    }
}
