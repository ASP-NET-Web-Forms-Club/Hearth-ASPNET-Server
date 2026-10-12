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
                        if (manualStops.Contains(site.Port)) continue;
                        if (site.AutoStart && !ProcessSupervisor.IsRunning(site.Port))
                        {
                            string err;
                            ProcessSupervisor.StartSite(site, out err);
                        }
                    }
                }
                catch { }

                for (int i = 0; i < 50 && !_stopping; i++)
                {
                    Thread.Sleep(100);
                }
            }
        }
    }
}
