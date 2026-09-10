using System;
using System.ServiceProcess;
using System.Windows.Forms;
using HearthPortableWebServer.Manager.Services;
using HearthPortableWebServer.Manager.UI;

namespace HearthPortableWebServer.Manager
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                string first = args[0].ToLowerInvariant();
                if (first == "--service" || first == "-service")
                {
                    ServiceBase.Run(new ManagerService());
                    return;
                }
                if (first == "--start-all" || first == "-start-all")
                {
                    ProcessSupervisor.StartAll(ManagerConfig.Load().Sites);
                    return;
                }
                if (first == "--stop-all" || first == "-stop-all")
                {
                    ProcessSupervisor.StopAll(ManagerConfig.Load().Sites);
                    return;
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
