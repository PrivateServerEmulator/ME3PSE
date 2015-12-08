using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PacketViewer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ME3Server_WV.GUI_PacketEditor p = new ME3Server_WV.GUI_PacketEditor();
            p.MainMenuStrip.Visible = true;
            Application.Run(p);
        }
    }
}
