using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace ME3Server_WV
{
    public partial class GUI_Player : Form
    {
        public GUI_Player()
        {
            InitializeComponent();
        }

        public List<int> Indexes;

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (Player.AllPlayers == null)
                return;
            int count = 0;
            bool update = false;
            foreach (Player.PlayerInfo p in Player.AllPlayers)
            {
                if (p.isActive)
                    count++;
                if (p.Update)
                {
                    update = true;
                    p.Update = false;
                }
            }
            if (count != listBox1.Items.Count||update)
            {
                Indexes = new List<int>();
                int n = listBox1.SelectedIndex;
                listBox1.Items.Clear();
                for (int i = 0; i < Player.AllPlayers.Count; i++)
                {
                    Player.PlayerInfo player = Player.AllPlayers[i];
                    if (player.isActive)
                    {
                        listBox1.Items.Add(GetInfo(player));
                        Indexes.Add(i);
                    }
                }
                if (n < listBox1.Items.Count)
                    listBox1.SelectedIndex = n;
            }
        }

        private string GetInfo(Player.PlayerInfo player)
        {
            string s = "";
            s += player.ID + " : ";
            s += "IP(" + player.IP + ") ";
            s += "GameState(" + player.GameState + ") ";
            if (player.UserID != 0)
                s += "UserID(" + player.PlayerID.ToString("X") + ") ";
            if (player.Name != null && player.Name != "")
                s += "Name(" + player.Name + ") ";
            return s;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1)
                return;
            string s = "";
            Player.PlayerInfo p = Player.AllPlayers[Indexes[n]];
            s += "Name: " + p.Name + "\n";
            s += "ID: " + p.ID + "\n";
            s += "Game State: " + p.GameState + "\n";
            s += "Authstring: " + p.AuthString + "\n";
            s += "Player ID: " + p.PlayerID.ToString("X") + "\n";
            s += "User ID: " + p.UserID.ToString("X") + "\n";
            s += "External IP: " + IPtoString(p.EXIP.IP) + "\n";
            s += "External Port: " + p.EXIP.PORT + "\n";
            s += "Internal IP: " + IPtoString(p.INIP.IP) + "\n";
            s += "Internal Port: " + p.INIP.PORT + "\n";
            s += "\nSettings (" + p.Settings.Count + ") : \n";
            s += p.GetSettings();
            rtb1.Text = s;
        }

        public string IPtoString(uint IP)
        {
            string res = "";
            res += ((uint)((IP >> 24) & 0xFF)) + ".";
            res += ((uint)((IP >> 16) & 0xFF)) + ".";
            res += ((uint)((IP >> 8) & 0xFF)) + ".";
            res += ((uint)(IP & 0xFF));
            return res;
        }

        private void editSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1)
                return;
            Player.PlayerInfo p = Player.AllPlayers[Indexes[n]];
            GUI_PlayerSettings gui = new GUI_PlayerSettings();
            gui.player = p;
            gui.FreshList();
            gui.MdiParent = this.MdiParent;
            gui.Show();
            gui.WindowState = FormWindowState.Maximized;
        }
    }
}
