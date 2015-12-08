using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ME3Server_WV
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (ME3Server.IsRunningAsAdmin())
            {
                string[] commandlineargs = System.Environment.GetCommandLineArgs();
                ME3Server.silentStart = commandlineargs.Contains("-silentstart", StringComparer.InvariantCultureIgnoreCase);
                ME3Server.silentExit = commandlineargs.Contains("-silentexit", StringComparer.InvariantCultureIgnoreCase);
                if (commandlineargs.Contains("-deactivateonly", StringComparer.InvariantCultureIgnoreCase))
                {
                    Frontend.DeactivateRedirection();
                }
                else
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new Frontend());
                    //Application.Run(new GUI_PacketEditor());
                    //Application.Run(new GUI_ProfileCreator());
                }
            }
            else
            {
                MessageBox.Show("This program requires administrator rights.", "ME3Server_WV", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }
    }
}
