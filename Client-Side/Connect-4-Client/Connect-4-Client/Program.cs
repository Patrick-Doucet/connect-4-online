using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text;
// Needed to create client-side sockets
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Connect_4_Client
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                // Initial configurations for the application
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                
                // Create and run the main thread of the application
                MainForm mainForm = new MainForm();
                Application.Run(mainForm);
            }
            catch { }
        }
    }
}
