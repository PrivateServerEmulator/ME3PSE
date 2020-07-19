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

namespace ME3Server_WV
{
    public partial class Frontend : Form
    {
        public static string loc = Path.GetDirectoryName(Application.ExecutablePath) + "\\";
        public static string sysdir = Environment.SystemDirectory + "\\";
        public GUI_Log LogWindow;
        public GUI_Player PlayerWindow;
        public GUI_GameList GameList;
        private static Frontend frontend;

        public Frontend()
        {
            InitializeComponent();
            frontend = this;
        }

        public static void UpdateLogLevelMenu()
        {
            frontend.level0MostCriticalToolStripMenuItem.Checked = (Logger.LogLevel == 0);
            frontend.level3ToolStripMenuItem.Checked = (Logger.LogLevel == 3);
            frontend.level5EverythingToolStripMenuItem.Checked = (Logger.LogLevel == 5);
        }

        public static void UpdateMITMMenuState()
        {
            frontend.activateToolStripMenuItem.Text = ME3Server.isMITM ? "Deactivate" : "Activate";
            frontend.recordPlayerSettingsToolStripMenuItem.Enabled = ME3Server.isMITM;
            frontend.importPlayerSettingsToolStripMenuItem.Enabled = ME3Server.isMITM;
        }

        private void Frontend_Load(object sender, EventArgs e)
        {
            string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            this.Text = "ME3 Private Server Emulator by Warranty Voider, build: " + version;
            LogWindow = new GUI_Log();
            PlayerWindow = new GUI_Player();
            GameList = new GUI_GameList();
            OpenMaxed(PlayerWindow);
            OpenMaxed(GameList);         
            OpenMaxed(LogWindow); 
        }

        public void OpenMaxed(Form f)
        {
            f.MdiParent = this;
            f.Show();
            f.WindowState = FormWindowState.Maximized;
        }

        private void patchGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PatchGame();
        }

        public static void PatchGame()
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "masseffect3.exe|masseffect3.exe";
            if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    string path = Path.GetDirectoryName(d.FileName);
                    File.Copy(loc + "patch\\binkw32.dll", path + "\\binkw32.dll", true);
                    File.Copy(loc + "patch\\binkw23.dll", path + "\\binkw23.dll", true);
                    if (File.Exists(loc + "patch\\MassEffect3.exe"))
                        File.Copy(loc + "patch\\MassEffect3.exe", path + "\\MassEffect3.exe", true);
                    MessageBox.Show("Done.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error:\n" + ME3Server.GetExceptionMessage(ex));
                }
            }
        }

        private void aktivateRedirectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ActivateRedirection(Config.FindEntry("RedirectIP"));
        }

        public static void ActivateRedirection(string hostIP)
        {
            DeactivateRedirection(false);
            try
            {
                List<string> r = new List<string>(File.ReadAllLines(loc + "conf\\redirect.txt"));
                List<string> h = new List<string>(File.ReadAllLines(sysdir + @"drivers\etc\hosts"));
                foreach (string url in r)
                {
                    string s = hostIP + " " + url;
                    if (!h.Contains(s))
                        h.Add(s);
                }
                File.WriteAllLines(sysdir + @"drivers\etc\hosts", h);
                MessageBox.Show("Done.", "Activate Redirection");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error:\n" + ME3Server.GetExceptionMessage(ex), "Activate Redirection");
            }
        }

        private void deactivateRedirectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeactivateRedirection();
        }

        public static void DeactivateRedirection(bool bShowMsg = true)
        {
            try
            {
                List<string> r = new List<string>(File.ReadAllLines(loc + "conf\\redirect.txt"));
                List<string> h = new List<string>(File.ReadAllLines(sysdir + @"drivers\etc\hosts"));
                foreach (string url in r)
                {
                    for (int i = (h.Count - 1); i >= 0; i--)
                    {
                        if (h[i].EndsWith(url) && !h[i].StartsWith("#"))
                            h.RemoveAt(i);
                    }
                }
                File.WriteAllLines(sysdir + @"drivers\etc\hosts", h);
                if (bShowMsg)
                    MessageBox.Show("Done.", "Deactivate Redirection");
            }
            catch (Exception ex)
            {
                if (bShowMsg)
                    MessageBox.Show("Error:\n" + ME3Server.GetExceptionMessage(ex), "Deactivate Redirection");
                else
                    System.Diagnostics.Debug.Print("DeactivateRedirection | " + ME3Server.GetExceptionMessage(ex));
            }
        }

        public static bool IsRedirectionActive()
        {
            try
            {

                int count = 0;
                List<string> r = new List<string>(File.ReadAllLines(loc + "conf\\redirect.txt"));
                List<string> h = new List<string>(File.ReadAllLines(sysdir + @"drivers\etc\hosts"));
                foreach (string url in r)
                {
                    for (int i = (h.Count - 1); i >= 0; i--)
                    {
                        if (h[i].EndsWith(url) && !h[i].StartsWith("#"))
                            count++;
                    }
                }
                if (count > 0)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print("IsRedirectionActive | Error:\n" + ME3Server.GetExceptionMessage(ex));
                return false;
            }
        }

        private void showContentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                MessageBox.Show(File.ReadAllText(sysdir + @"drivers\etc\hosts"), "Show Content");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error:\n" + ME3Server.GetExceptionMessage(ex), "Show Content");
            }
        }

        private void packetEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GUI_PacketEditor p = new GUI_PacketEditor();
            p.MdiParent = this;
            p.Show();
            p.WindowState = FormWindowState.Maximized;
        }

        private void showLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LogWindow.BringToFront();
        }

        private void showPlayerListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PlayerWindow.BringToFront();
        }

        private void showGameListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GameList.BringToFront();
        }

        private void localProfileCreatorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GUI_ProfileCreator pc = new GUI_ProfileCreator();
            pc.StartPosition = FormStartPosition.CenterScreen;
            pc.Show();
        }

        private void deleteLogsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string[] logs = Directory.GetFiles(loc + "logs\\");
            foreach (string log in logs)
                File.Delete(log);
            MessageBox.Show("Done");
        }

        private void level0MostCriticalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Logger.LogLevel == 0)
                return;
            Logger.LogLevel = 0;
            Logger.Log("Log Level Changed to : " + Logger.LogLevel, Color.Black);
            UpdateLogLevelMenu();
        }

        private void level3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Logger.LogLevel == 3)
                return;
            Logger.LogLevel = 3;
            Logger.Log("Log Level Changed to : " + Logger.LogLevel, Color.Black);
            UpdateLogLevelMenu();
        }

        private void level5EverythingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Logger.LogLevel == 5)
                return;
            Logger.LogLevel = 5;
            Logger.Log("Log Level Changed to : " + Logger.LogLevel, Color.Black);
            UpdateLogLevelMenu();
        }

        private void recordPlayerSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ME3Server.bRecordPlayerSettings = recordPlayerSettingsToolStripMenuItem.Checked;
            Logger.Log("MITM | Record player settings = " + ME3Server.bRecordPlayerSettings, Color.Black);
        }

        private void importPlayerSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool activePlayer = false;
            foreach (Player.PlayerInfo p in Player.AllPlayers)
                activePlayer |= p.isActive;
            if (!activePlayer)
            {
                MessageBox.Show("You must be already connected through PSE before using this function.", "Import player settings");
                return;
            }

            OpenFileDialog o = new OpenFileDialog();
            o.Filter = "*.txt|*.txt";
            if (o.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            try
            {
                List<string> Lines = new List<string>(System.IO.File.ReadAllLines(o.FileName));
                if (Lines.Count < 6)
                {
                    Logger.Log("[Import player settings] Invalid player file (number of lines)", Color.Red);
                    return;
                }
                Lines.RemoveRange(0, 5);
                List<string> keys = new List<string>();
                List<string> values = new List<string>();
                foreach (string line in Lines)
                {
                    string[] s = line.Split(Char.Parse("="));
                    if (s.Length != 2)
                    {
                        Logger.Log("[Import player settings] Invalid player file (line split)", Color.Red);
                        return;
                    }
                    keys.Add(s[0]);
                    values.Add(s[1]);
                }
                ME3Server.importKeys = keys;
                ME3Server.importValues = values;
            }
            catch (Exception ex)
            {
                Logger.Log("[Import player settings] " + ME3Server.GetExceptionMessage(ex), Color.Red);
            }
        }

        private void playerDataEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.CurrentDirectory = loc;
            System.Diagnostics.Process.Start("ME3PlayerDataEditor.exe");
        }

        private void restartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }

        private void activateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ME3Server.isMITM = !ME3Server.isMITM;
            Logger.Log("MITM mode = " + ME3Server.isMITM, Color.Black);
            UpdateMITMMenuState();
            recordPlayerSettingsToolStripMenuItem.Checked = false;
            ME3Server.bRecordPlayerSettings = false;
        }

        private void Frontend_Shown(object sender, EventArgs e)
        {
            ME3Server.Start();
        }
    }
}
