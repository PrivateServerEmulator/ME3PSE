using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ME3Server_WV
{
    public static class Config
    {
        private static string loc = Path.GetDirectoryName(Application.ExecutablePath) + "\\";
        private static readonly object _sync = new object();
        public static List<string> Entries;

        public static void Load()
        {
            if (File.Exists(loc + "conf\\conf.txt"))
                Entries = new List<string>(File.ReadAllLines(loc + "conf\\conf.txt")); 
            else
                Logger.Log("Configuration loading failed", Color.Red);
        }

        public static string FindEntry(string name)
        {
            string s = "";
            lock (_sync)
            {
                for (int i = 0; i < Entries.Count; i++)
                {
                    string line = Entries[i];
                    if (line.Trim().StartsWith("#"))
                        continue;
                    string[] parts = line.Split('=');
                    if (parts.Length != 2)
                        continue;
                    if (parts[0].Trim().ToLower() == name.ToLower())
                        return parts[1].Trim();
                }
            }
            return s;
        }

        public static string MainMenuMessage()
        {
            string defaultMessage = "Welcome to ME3 Private Server Emulator";
            string messagefile = loc + "conf\\MainMenuMessage.txt";
            try
            {
                string[] lines = File.ReadAllLines(messagefile);

                lines[0] = lines[0].Trim();
                if (lines[0] == "")
                    return defaultMessage;

                return lines[0];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print("MainMenuMessage | " + ex.GetType().Name + ex.Message);
                return defaultMessage;
            }
        }

        public static bool AlwaysSkipHostsCheck()
        {
            try
            {
                return Boolean.Parse(FindEntry("AlwaysSkipHostsCheck"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print("AlwaysSkipHostsCheck | " + ex.GetType().Name + ex.Message);
                return false;
            }
        }

        public static bool PromotionsEnabled()
        {
            try
            {
                return Boolean.Parse(FindEntry("GaW_EnablePromotions"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print("PromotionsEnabled | " + ex.GetType().Name + ex.Message);
                return true;
            }
        }

    }
}
