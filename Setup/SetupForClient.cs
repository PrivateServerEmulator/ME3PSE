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
    public partial class SetupForClient : Form
    {
        private static string loc = Path.GetDirectoryName(Application.ExecutablePath) + "\\";
        public SetupForClient()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string IP = textBox1.Text;
            string[] lines = File.ReadAllLines(loc + "conf\\conf.txt");
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim().StartsWith("#"))
                    continue;
                string[] parts = lines[i].Split('=');
                if (parts[0].Trim() == "IP")
                    lines[i] = "IP=" + IP;
                if (parts[0].Trim() == "RedirectIP")
                    lines[i] = "RedirectIP=" + IP;
            }
            File.WriteAllLines(loc + "conf\\conf.txt", lines);
            ME3Server_WV.Frontend.ActivateRedirection(IP);
            this.Close();
        }
    }
}
