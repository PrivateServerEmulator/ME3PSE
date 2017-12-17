using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;

namespace ME3Server_WV
{
    public static class Logger
    {
        private static object _sync = new object();
        private static string PacketLogFile = "PacketLog";
        private static string loc = Path.GetDirectoryName(Application.ExecutablePath) + "\\";
        public static string mainlogpath = loc + "logs\\MainServerLog.txt";
        public static RichTextBox box;
        public static int LogLevel = 0;
        public static void Log(string msg, Color c, int Level = 0)
        {
            lock (_sync)
            {
                try
                {
                    if (box == null)
                        return;
                    string s = string.Format(@"{0:yyyy.MM.dd HH:mm:ss}", DateTime.Now) + " " + msg + "\n";
                    if (!File.Exists(mainlogpath))
                        File.WriteAllBytes(mainlogpath, new byte[0]);
                    File.AppendAllText(mainlogpath, s.Replace("\n", "\r\n"));
                    if (Level > LogLevel)
                        return;                  
                    box.Invoke(new Action(() =>
                    {
                        box.SelectionStart = box.TextLength;
                        box.SelectionLength = 0;
                        box.SelectionColor = c;
                        if (c == Color.White || c == Color.Cyan || c == Color.Yellow)
                            box.SelectionBackColor = Color.Black;
                        box.AppendText(s);
                        box.SelectionBackColor = box.BackColor;
                        box.SelectionColor = box.ForeColor;
                        box.SelectionStart = box.TextLength;
                        box.SelectionLength = 0;
                        box.ScrollToCaret();
                    }));
                }
                catch (Exception)
                {
                }
            }
        }

        public struct DumpStruct
        {
            public byte[] buff;
            public Player.PlayerInfo player;
        }

        public static void DumpPacket(byte[] buff,Player.PlayerInfo player)
        {
            Thread t = new Thread(ThreadPacketDump);
            DumpStruct d = new DumpStruct();
            d.buff = buff;
            d.player = player;
            t.Start(d);
        }

        public static void DeleteLogs()
        {            
            string[] files = Directory.GetFiles(loc + "logs\\");
            if (files.Length != 0)
            {
                if (files.Length == 1 && files[0].Contains("MainServerLog.txt"))
                    return;
                string autodelete = Config.FindEntry("AutoDeleteLogs");
                if (autodelete == "1" || MessageBox.Show("There are old logs, delete them?", "Confirmation", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Log("Found Logs, deleting...", Color.Red);
                    foreach (string file in files)
                        if (!file.Contains("MainServerLog.txt"))
                        {
                            Log("Deleting : " + Path.GetFileName(file) + " ...", Color.Red);
                            File.Delete(file);
                        }                    
                }
            }            
        }

        public static void ThreadPacketDump(object objs)
        {
            DumpStruct d = (DumpStruct)objs;
            byte[] buff = d.buff;
            lock (_sync)
            {
                FileStream fs = new FileStream(loc + "logs\\" + PacketLogFile + "_" + d.player.timestring + "_" + d.player.ID.ToString("00") + ".bin", FileMode.Append, FileAccess.Write);
                fs.Write(buff, 0, buff.Length);
                fs.Close();
            }
        }
    }
}
