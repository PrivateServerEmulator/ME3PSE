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
    public partial class GUI_GameList : Form
    {
        public GUI_GameList()
        {
            InitializeComponent();
        }
        public List<int> Indexes = new List<int>();

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (GameManager.AllGames== null)
                return;
            int count = 0;
            bool update = false;
            Indexes = new List<int>();
            for (int i = 0; i < GameManager.AllGames.Count; i++) 
            {
                GameManager.GameInfo g = GameManager.AllGames[i];
                if (g.isActive)
                {
                    count++;
                    Indexes.Add(i);
                }
                if (g.Update)
                {
                    update = true;
                    g.Update = false;
                }
            }
            if (count != listBox1.Items.Count || update)
            {
                int n = listBox1.SelectedIndex;
                listBox1.Items.Clear();
                foreach (int idx in Indexes)
                    listBox1.Items.Add(GetInfo(GameManager.AllGames[idx]));
                if (n < listBox1.Items.Count)
                    listBox1.SelectedIndex = n;
            }
        }

        private string GetInfo(GameManager.GameInfo g)
        {
            string s = "";
            s += "GameID: 0x" + g.ID.ToString("X") + " Creator: " + g.Creator.Name;
            return s;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1)
                return;
            string s = "";
            GameManager.GameInfo g = GameManager.AllGames[Indexes[n]];
            s += "ID: 0x" + g.ID.ToString("X") + "\n";
            s += "Creator: " + g.Creator.Name + "\n";
            s += "Game State: 0x" + g.GAMESTATE.ToString("X") + "\n";
            s += "Game Setting: 0x" + g.GAMESETTING.ToString("X") + "\n";
            s += "Attributes (" + g.Attributes.Count + ") :\n";
            foreach (GameManager.GameInfo.Attribut a in g.Attributes)
                s += "\t" + a.Name + " = " + a.Value + "\n";
            s += "\nPlayer Summary (Creator):\n" + CreatePlayerSummary(g.Creator);
            foreach(Player.PlayerInfo player in g.OtherPlayers)
                s += "\nPlayer Summary (Other Player):\n" + CreatePlayerSummary(player);
            rtb1.Text = s;
        }

        private string CreatePlayerSummary(Player.PlayerInfo player)
        {
            string s = "";            
            s += " Player Name : " + player.Name + "\n";            
            s += " Player ID   : 0x" + player.ID.ToString("X") + "\n";
            s += " Player PID  : 0x" + player.PlayerID.ToString("X") + "\n";
            s += " Player UID  : 0x" + player.UserID.ToString("X") + "\n";
            s += " EXIP IP     : 0x" + player.EXIP.IP.ToString("X") + "\n";
            s += " EXIP PORT   : 0x" + player.EXIP.PORT.ToString("X") + "\n";
            s += " INIP IP     : 0x" + player.INIP.IP.ToString("X") + "\n";
            s += " INIP PORT   : 0x" + player.INIP.PORT.ToString("X") + "\n";
            s += "\n";
            return s;
        }
    }
}
