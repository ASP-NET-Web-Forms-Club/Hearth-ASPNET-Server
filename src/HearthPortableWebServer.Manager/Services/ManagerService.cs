using System;
using System.ServiceProcess;
using System.Threading;
using HearthPortableWebServer.Manager.Models;

namespace HearthPortableWebServer.Manager.Services
{
    public sealed class ManagerService : ServiceBase
    {
        private Thread _watchdogThread;
        private volatile bool _stopping;

        public ManagerService()
        {
            ServiceName = ServiceControllerHelper.ServiceName;
            CanStop = true;
            CanShutdown = true;
            AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            _stopping = false;

            // Ensure interactive users in Session 1+ can read/write shared state files
            // (manager_sites.json, manual_stopped.txt, tracked_processes.txt) created by LocalSystem.
            ProcessSupervisor.EnsureFolderPermissions(ManagerConfig.GetAppDirectory());

            // Fresh boot / service launch: manual stops were temporary, honour AutoStart again.
            ManagerConfig.ClearManualStops();

            _watchdogThread = new Thread(WatchdogLoop)
            {
                IsBackground = true,
                Name = "HearthManagerWatchdog"
            };
            _watchdogThread.Start();
        }

        protected override void OnStop()
        {
            _stopping = true;
            if (_watchdogThread != null)
            {
                _watchdogThread.Join(3000);
                _watchdogThread = null;
            }

            try
            {
                ManagerConfig config = ManagerConfig.Load();
                ProcessSupervisor.StopAll(config.Sites);
            }
            catch { }
        }

        protected override void OnShutdown()
        {
            OnStop();
        }

        private void WatchdogLoop()
        {
            while (!_stopping)
            {
                try
                {
                    ManagerConfig config = ManagerConfig.Load();
                    System.Collections.Generic.HashSet<int> manualStops = ManagerConfig.LoadManualStops();
                    foreach (SiteEntry site in config.Sites)
                    {
                        if (_stopping) break;
                        if (manualStops.Contains(site.Port))
                        {
                            // A non-elevated Manager UI cannot terminate a Session 0 (SYSTEM) child process
                            // directly, so if the user marked this port as manually stopped, stop it here.
                            if (ProcessSupervisor.IsRunning(site.Port))
                            {
                                ProcessSupervisor.StopSite(site.Port);
                            }
                            continue;
                        }
                        if (site.AutoStart && !ProcessSupervisor.IsRunning(site.Port))
                        {
                            string err;
                            ProcessSupervisor.StartSite(site, out err);
                        }
                    }
                }
                catch { }

                for (int i = 0; i < 10 && !_stopping; i++)
                {
                    Thread.Sleep(100);
                }
            }
        }
    }
}
