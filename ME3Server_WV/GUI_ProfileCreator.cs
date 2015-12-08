using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography;

namespace ME3Server_WV
{
    public partial class GUI_ProfileCreator : Form
    {

        private long currentID;

        public GUI_ProfileCreator()
        {
            InitializeComponent();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (IsValidPlayerName(textBox2.Text))
            {
                currentID = MakeID(textBox2.Text);
                textBox1.Text = currentID.ToString("X8");
                if (!IsValidPassword(textBox3.Text))
                    textBox3.Text = GetRandomPassword();
            }
            else
                textBox1.Text = "";
        }

        public static long MakeID(string name)
        {
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(name);
            byte[] hash = md5.ComputeHash(inputBytes);
            string res = "";
            for (int i = 0; i < 4; i++)
                res += hash[i].ToString("X2");
            return Convert.ToInt64(res, 16);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string msg;
            if (textBox1.Text == "")
            {
                textBox2.Focus();
                return;
            }
            if (!IsValidPassword(textBox3.Text))
            {
                textBox3.Focus();
                return;
            }

            string playertextfile = Frontend.loc + "player\\" + textBox2.Text + ".txt";
            bool overwritten = false;
            if (File.Exists(playertextfile))
            {
                msg = "A file named '" + textBox2.Text + ".txt' already exists inside the player folder and is about to be overwritten.\n\n";
                msg += "If you proceed, server-side data from that file's respective profile will be lost.\n\nContinue?";
                if (MessageBox.Show(msg, "Name conflict", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
                    return;
                overwritten = true;
            }

            DialogResult dr = MessageBox.Show("Do you want to create a Local_Profile.sav for this profile?", "Local_Profile.sav", MessageBoxButtons.YesNo);

            if (dr == DialogResult.Yes)
            {
                SaveFileDialog d = new SaveFileDialog();
                d.Filter = "*.sav|*.sav";
                d.FileName = "Local_Profile.sav";
                string AUTH = currentID.ToString("X8") + "UoE4gBscrqJNM7j6nR84thRQrPmaqc1TgbPCXc3vTmOf-1jnUBttCGvO-j2M2RG54CP48eNSZHqbHLnGeP8PL4YsPVsqKU9s9CmyKohn9ezWeQ5HhX9u9wVY";
                if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    File.WriteAllBytes(d.FileName, Local_Profile.CreateProfile((int)currentID, AUTH));
            }

            if (!CreateProfile(currentID, textBox2.Text, textBox3.Text))
            {
                MessageBox.Show("Error on creating player profile text file.");
                return;
            }

            msg = "Done.\n\nFile 'player\\" + textBox2.Text + ".txt' has been " + (overwritten ? "overwritten." : "created.");
            msg += "\n\nLogin: " + textBox2.Text + "\nPassword: " + textBox3.Text;
            MessageBox.Show(msg, "Profile creation", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }

        public static bool IsValidPlayerName(string name)
        {
            // char count must be higher than 0 (or 'not 0' since an instance of string never has negative length)
            if (name.Length == 0)
                return false;
            // invalid fn chars
            bool hasInvalidChar = false;
            char[] invalidchars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidchars)
                hasInvalidChar |= name.Contains(c);
            if (hasInvalidChar)
                return false;
            // first char can't be space or dot
            if (name[0] == ' ' || name[0] == '.')
                return false ;
            // last char can't be space or dot
            if (name[name.Length - 1] == ' ' || name[name.Length - 1] == '.')
                return false;
            return true;
        }

        public static bool IsValidPassword(string pw)
        {
            // password cannot be empty string
            if (String.IsNullOrEmpty(pw))
                return false;
            // password cannot have more than 10 chars
            if (pw.Length > 10)
                return false;
            // password cannot contain spaces
            if (pw.Contains(' '))
                return false;
            return true;
        }

        public static string GetRandomPassword()
        {
            const string availablechars = "abcdefghijklmnopqrstuvwxyz0123456789";
            Random r = new Random();
            string finalpassword = "";
            int desiredlength = r.Next(3,6);
            for (int i = 0; i < desiredlength; i++)
                finalpassword += availablechars[r.Next(0, availablechars.Length - 1)];
            return finalpassword;
        }

        public static bool CreateProfile(long PlayerID, string PlayerName, string Password)
        {
            try
            {
                string res = "";
                res += "PID=0x" + PlayerID.ToString("X8") + "\r\n";
                res += "UID=0x" + PlayerID.ToString("X8") + "\r\n";
                res += "AUTH=" + PlayerID.ToString("X8") + "UoE4gBscrqJNM7j6nR84thRQrPmaqc1TgbPCXc3vTmOf-1jnUBttCGvO-j2M2RG54CP48eNSZHqbHLnGeP8PL4YsPVsqKU9s9CmyKohn9ezWeQ5HhX9u9wVY\r\n";
                res += "AUTH2=" + Password + "\r\n";
                res += "DSNM=" + PlayerName;
                File.WriteAllText(Frontend.loc + "player\\" + PlayerName + ".txt", res);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print("CreateProfile | " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }
    }
}
