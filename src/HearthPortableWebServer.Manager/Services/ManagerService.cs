using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Threading;
using HearthPortableWebServer.Manager.Models;

namespace HearthPortableWebServer.Manager.Services
{
    public sealed class ManagerService : ServiceBase
    {
        private readonly object _opLock = new object();
        private readonly AutoResetEvent _wakeEvent = new AutoResetEvent(false);
        private Thread _watchdogThread;
        private ManagerIpcServer _ipcServer;
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

            _ipcServer = new ManagerIpcServer(HandleIpcRequest);
            _ipcServer.Start();

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
            try { _wakeEvent.Set(); } catch { }

            if (_ipcServer != null)
            {
                try { _ipcServer.Stop(); } catch { }
                _ipcServer = null;
            }

            if (_watchdogThread != null)
            {
                _watchdogThread.Join(3000);
                _watchdogThread = null;
            }

            try
            {
                lock (_opLock)
                {
                    ManagerConfig config = ManagerConfig.Load();
                    ProcessSupervisor.StopAll(config.Sites);
                }
            }
            catch { }
        }

        protected override void OnShutdown()
        {
            OnStop();
        }

        private ManagerIpc.IpcResponse HandleIpcRequest(ManagerIpc.IpcRequest req)
        {
            string cmd = (req.Command ?? string.Empty).Trim().ToLowerInvariant();

            if (cmd == "status" || cmd == "ping")
            {
                ManagerConfig config = ManagerConfig.Load();
                List<ManagerIpc.SiteStatusDto> list = new List<ManagerIpc.SiteStatusDto>();
                foreach (SiteEntry site in config.Sites)
                {
                    string state;
                    if (ProcessSupervisor.IsRunning(site.Port))
                    {
                        state = "Running";
                    }
                    else if (ProcessSupervisor.IsPortListening(site.Port))
                    {
                        state = "PortInUse";
                    }
                    else
                    {
                        state = "Stopped";
                    }

                    list.Add(new ManagerIpc.SiteStatusDto
                    {
                        Port = site.Port,
                        State = state,
                        RuntimeMissing = ProcessSupervisor.IsRuntimeMissing(site)
                    });
                }

                return new ManagerIpc.IpcResponse
                {
                    Ok = true,
                    Sites = list
                };
            }

            if (cmd == "start")
            {
                lock (_opLock)
                {
                    ManagerConfig config = ManagerConfig.Load();
                    SiteEntry site = FindSiteByPort(config.Sites, req.Port);
                    if (site == null)
                    {
                        return new ManagerIpc.IpcResponse { Ok = false, Error = "No site configured on port " + req.Port + "." };
                    }

                    ManagerConfig.SetManuallyStopped(site.Port, false);
                    string err;
                    bool ok = ProcessSupervisor.StartSite(site, false, out err);
                    return new ManagerIpc.IpcResponse { Ok = ok, Error = err };
                }
            }

            if (cmd == "stop")
            {
                lock (_opLock)
                {
                    ManagerConfig.SetManuallyStopped(req.Port, true);
                    bool ok = ProcessSupervisor.StopSite(req.Port);
                    return new ManagerIpc.IpcResponse { Ok = ok };
                }
            }

            if (cmd == "stop_reconfig")
            {
                lock (_opLock)
                {
                    ManagerConfig.SetManuallyStopped(req.Port, false);
                    bool ok = ProcessSupervisor.StopSite(req.Port);
                    return new ManagerIpc.IpcResponse { Ok = ok };
                }
            }

            if (cmd == "start_all")
            {
                lock (_opLock)
                {
                    ManagerConfig.ClearManualStops();
                    ManagerConfig config = ManagerConfig.Load();
                    List<string> errors = new List<string>();
                    ProcessSupervisor.StartAll(config.Sites, false, errors);
                    return new ManagerIpc.IpcResponse
                    {
                        Ok = errors.Count == 0,
                        Errors = errors
                    };
                }
            }

            if (cmd == "stop_all")
            {
                lock (_opLock)
                {
                    ManagerConfig config = ManagerConfig.Load();
                    foreach (SiteEntry s in config.Sites)
                    {
                        ManagerConfig.SetManuallyStopped(s.Port, true);
                    }
                    ProcessSupervisor.StopAll(config.Sites);
                    return new ManagerIpc.IpcResponse { Ok = true };
                }
            }

            if (cmd == "stop_orphans")
            {
                lock (_opLock)
                {
                    ManagerConfig config = ManagerConfig.Load();
                    ProcessSupervisor.StopOrphans(config.Sites);
                    return new ManagerIpc.IpcResponse { Ok = true };
                }
            }

            if (cmd == "reload")
            {
                try { _wakeEvent.Set(); } catch { }
                return new ManagerIpc.IpcResponse { Ok = true };
            }

            return new ManagerIpc.IpcResponse { Ok = false, Error = "Unknown command: " + cmd };
        }

        private static SiteEntry FindSiteByPort(List<SiteEntry> sites, int port)
        {
            if (sites == null) return null;
            foreach (SiteEntry s in sites)
            {
                if (s.Port == port) return s;
            }
            return null;
        }

        private void WatchdogLoop()
        {
            while (!_stopping)
            {
                try
                {
                    lock (_opLock)
                    {
                        if (_stopping) break;

                        ManagerConfig config = ManagerConfig.Load();
                        HashSet<int> manualStops = ManagerConfig.LoadManualStops();

                        // Stop children on ports that are no longer configured (site deleted or its
                        // port/engine changed). The service can kill Session 0 children the UI cannot.
                        ProcessSupervisor.StopOrphans(config.Sites);

                        foreach (SiteEntry site in config.Sites)
                        {
                            if (_stopping) break;
                            if (manualStops.Contains(site.Port))
                            {
                                if (ProcessSupervisor.IsRunning(site.Port))
                                {
                                    ProcessSupervisor.StopSite(site.Port);
                                }
                                continue;
                            }
                            if (site.AutoStart && !ProcessSupervisor.IsRunning(site.Port))
                            {
                                string err;
                                // Automatic: skipped silently once this site's runtime is known to be missing.
                                ProcessSupervisor.StartSite(site, true, out err);
                            }
                        }
                    }
                }
                catch { }

                _wakeEvent.WaitOne(1000);
            }
        }
    }
}
