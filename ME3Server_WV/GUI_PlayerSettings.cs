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
    public partial class GUI_PlayerSettings : Form
    {
        public Player.PlayerInfo player;

        public GUI_PlayerSettings()
        {
            InitializeComponent();
        }

        public void FreshList()
        {
            if (player == null)
                return;
            listBox1.Items.Clear();
            foreach (Player.PlayerInfo.SettingEntry set in player.Settings)
                listBox1.Items.Add(set.Key);
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1)
                return;
            rtb1.Text = player.Settings[n].Data;
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1)
                return;
            player.UpdateSettings(listBox1.Items[n].ToString(), rtb1.Text);
        }
    }
}
