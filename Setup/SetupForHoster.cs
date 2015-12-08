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
using System.Net.NetworkInformation;
namespace Setup
{
    public partial class SetupForHoster : Form
    {
        public SetupForHoster()
        {
            InitializeComponent();
        }
        List<string> IPAdresses;
        public Form1 parent;
        private static string loc = Path.GetDirectoryName(Application.ExecutablePath) + "\\";

        private void SetupForHoster_Load(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            IPAdresses = new List<string>();
            IPAdresses.Add("127.0.0.1");
            comboBox1.Items.Add("Loopback : 127.0.0.1");
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface face in interfaces)
                if ((face.NetworkInterfaceType == NetworkInterfaceType.Ethernet) || (face.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                {
                    UnicastIPAddressInformationCollection props = face.GetIPProperties().UnicastAddresses;
                    foreach (UnicastIPAddressInformation uipai in props)
                        if (uipai.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            comboBox1.Items.Add(face.Name + " : " + uipai.Address.ToString());
                            IPAdresses.Add(uipai.Address.ToString());
                        }
                }
            comboBox1.SelectedIndex = comboBox1.Items.Count - 1;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int n = comboBox1.SelectedIndex;
            if (n == -1)
                return;
            string IP = IPAdresses[n];
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
            System.Diagnostics.Process.Start("ME3Server_WV.exe","-silentstart");
            parent.Close();
            this.Close();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}
