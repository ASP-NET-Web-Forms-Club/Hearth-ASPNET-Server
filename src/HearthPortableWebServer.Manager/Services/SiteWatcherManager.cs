using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using HearthPortableWebServer.Manager.Models;

namespace HearthPortableWebServer.Manager.Services
{
    public static class SiteWatcherManager
    {
        private static readonly Dictionary<int, FileSystemWatcher> _watchers = new Dictionary<int, FileSystemWatcher>();
        private static readonly Dictionary<int, System.Threading.Timer> _debounceTimers = new Dictionary<int, System.Threading.Timer>();
        private static readonly object _lock = new object();

        // Extensions that trigger a reload
        private static readonly HashSet<string> _watchedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".py", ".js", ".mjs", ".cjs", ".ts", ".jsx", ".tsx",
            ".php", ".phtml",
            ".cs", ".dll", ".config", ".json", ".env", ".xml"
        };

        // Folders to ignore
        private static readonly string[] _ignoredDirectories = new string[]
        {
            ".git", "node_modules", "__pycache__", ".venv", "venv", "bin", "obj", "logs", ".vs", "App_Data"
        };

        public static void StartWatcher(SiteEntry site)
        {
            if (site == null || !site.AutoRestart) return;

            // ASP.NET 4.8 handles bin/web.config AppDomain recycling natively in Host.exe
            if (string.Equals(site.Engine, "ASP.NET", StringComparison.OrdinalIgnoreCase)) return;

            StopWatcher(site.Port);

            string resolvedRoot = ManagerConfig.ResolveRootAbsolute(site.Root);
            if (!Directory.Exists(resolvedRoot)) return;

            lock (_lock)
            {
                try
                {
                    FileSystemWatcher watcher = new FileSystemWatcher(resolvedRoot)
                    {
                        IncludeSubdirectories = true,
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                        EnableRaisingEvents = true
                    };

                    FileSystemEventHandler onChange = delegate(object sender, FileSystemEventArgs e)
                    {
                        OnFileChanged(site, e.FullPath);
                    };

                    RenamedEventHandler onRename = delegate(object sender, RenamedEventArgs e)
                    {
                        OnFileChanged(site, e.FullPath);
                    };

                    watcher.Changed += onChange;
                    watcher.Created += onChange;
                    watcher.Deleted += onChange;
                    watcher.Renamed += onRename;

                    _watchers[site.Port] = watcher;
                }
                catch { }
            }
        }

        public static void StopWatcher(int port)
        {
            lock (_lock)
            {
                if (_debounceTimers.ContainsKey(port))
                {
                    try { _debounceTimers[port].Dispose(); } catch { }
                    _debounceTimers.Remove(port);
                }

                if (_watchers.ContainsKey(port))
                {
                    try
                    {
                        _watchers[port].EnableRaisingEvents = false;
                        _watchers[port].Dispose();
                    }
                    catch { }
                    _watchers.Remove(port);
                }
            }
        }

        private static void OnFileChanged(SiteEntry site, string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath)) return;

            // Check if path is inside ignored directory
            foreach (string ignored in _ignoredDirectories)
            {
                string token = Path.DirectorySeparatorChar + ignored + Path.DirectorySeparatorChar;
                if (fullPath.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fullPath.EndsWith(Path.DirectorySeparatorChar + ignored, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            string ext = Path.GetExtension(fullPath);
            if (!_watchedExtensions.Contains(ext)) return;

            lock (_lock)
            {
                int port = site.Port;
                if (_debounceTimers.ContainsKey(port))
                {
                    _debounceTimers[port].Change(500, Timeout.Infinite);
                }
                else
                {
                    _debounceTimers[port] = new System.Threading.Timer(delegate
                    {
                        TriggerRestart(site);
                    }, null, 500, Timeout.Infinite);
                }
            }
        }

        private static void TriggerRestart(SiteEntry site)
        {
            lock (_lock)
            {
                if (_debounceTimers.ContainsKey(site.Port))
                {
                    try { _debounceTimers[site.Port].Dispose(); } catch { }
                    _debounceTimers.Remove(site.Port);
                }
            }

            try
            {
                // Restart only if still running
                if (ProcessSupervisor.IsRunning(site.Port))
                {
                    ProcessSupervisor.StopSite(site.Port);
                    Thread.Sleep(300);
                    string err;
                    ProcessSupervisor.StartSite(site, out err);
                }
            }
            catch { }
        }
    }
}
