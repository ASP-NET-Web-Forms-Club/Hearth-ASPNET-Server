using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.NetworkInformation;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading;
using HearthPortableWebServer.Common;
using HearthPortableWebServer.Manager.Models;

namespace HearthPortableWebServer.Manager.Services
{
    public static class ProcessSupervisor
    {
        private static readonly Dictionary<int, Process> _trackedProcesses = new Dictionary<int, Process>();
        private static readonly object _procLock = new object();

        public static string HostExePath()
        {
            return Path.Combine(ManagerConfig.GetAppDirectory(), "HearthPortableWebServer.Host.exe");
        }

        public static bool IsPortListening(int port)
        {
            try
            {
                IPGlobalProperties ipProps = IPGlobalProperties.GetIPGlobalProperties();
                System.Net.IPEndPoint[] activeListeners = ipProps.GetActiveTcpListeners();
                foreach (System.Net.IPEndPoint ep in activeListeners)
                {
                    if (ep.Port == port) return true;
                }
            }
            catch { }
            return false;
        }

        public static bool IsRunning(int port)
        {
            if (SyncHelper.MutexExists(IpcNames.RunningMutex(port)))
            {
                return true;
            }

            lock (_procLock)
            {
                if (_trackedProcesses.ContainsKey(port))
                {
                    Process p = _trackedProcesses[port];
                    try
                    {
                        if (!p.HasExited) return true;
                    }
                    catch { }
                }
            }

            // Only processes Hearth itself started count as running. A port held by some
            // other application (e.g. IIS / http.sys) is NOT a running Hearth site.
            return GetPersistedPid(port) > 0;
        }

        // ---- Persisted process tracking ----
        // The UI and the Windows service are separate processes, and the Manager may be restarted,
        // so non-ASP.NET child PIDs are persisted to disk ("port|pid|processName" per line).
        private const string TrackedFileName = "tracked_processes.txt";
        private static readonly object _fileLock = new object();

        private static string TrackedFilePath()
        {
            return Path.Combine(ManagerConfig.GetAppDirectory(), TrackedFileName);
        }

        private static Dictionary<int, KeyValuePair<int, string>> LoadPersisted()
        {
            Dictionary<int, KeyValuePair<int, string>> map = new Dictionary<int, KeyValuePair<int, string>>();
            lock (_fileLock)
            {
                try
                {
                    string path = TrackedFilePath();
                    if (!File.Exists(path)) return map;
                    foreach (string line in File.ReadAllLines(path))
                    {
                        string[] parts = line.Split('|');
                        int port, pid;
                        if (parts.Length >= 3 && int.TryParse(parts[0], out port) && int.TryParse(parts[1], out pid))
                        {
                            map[port] = new KeyValuePair<int, string>(pid, parts[2]);
                        }
                    }
                }
                catch { }
            }
            return map;
        }

        private static void SavePersisted(Dictionary<int, KeyValuePair<int, string>> map)
        {
            lock (_fileLock)
            {
                try
                {
                    string path = TrackedFilePath();
                    if (map.Count == 0)
                    {
                        if (File.Exists(path)) File.Delete(path);
                        return;
                    }
                    List<string> lines = new List<string>();
                    foreach (KeyValuePair<int, KeyValuePair<int, string>> kv in map)
                    {
                        lines.Add(kv.Key + "|" + kv.Value.Key + "|" + kv.Value.Value);
                    }
                    File.WriteAllLines(path, lines.ToArray());
                }
                catch { }
            }
        }

        private static void PersistPid(int port, Process proc)
        {
            try
            {
                Dictionary<int, KeyValuePair<int, string>> map = LoadPersisted();
                map[port] = new KeyValuePair<int, string>(proc.Id, proc.ProcessName);
                SavePersisted(map);
            }
            catch { }
        }

        private static void RemovePersisted(int port)
        {
            Dictionary<int, KeyValuePair<int, string>> map = LoadPersisted();
            if (map.Remove(port)) SavePersisted(map);
        }

        /// <summary>Returns the PID of a still-alive process Hearth started for this port, or 0.</summary>
        private static int GetPersistedPid(int port)
        {
            Dictionary<int, KeyValuePair<int, string>> map = LoadPersisted();
            KeyValuePair<int, string> entry;
            if (!map.TryGetValue(port, out entry)) return 0;
            try
            {
                using (Process p = Process.GetProcessById(entry.Key))
                {
                    // Guard against PID reuse by checking the process name too.
                    if (!p.HasExited && string.Equals(p.ProcessName, entry.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        return entry.Key;
                    }
                }
            }
            catch { }
            return 0;
        }

        /// <summary>Kills any HearthPortableWebServer.Host.exe instance serving the given port.</summary>
        private static void KillHostProcessesForPort(int port)
        {
            try
            {
                string token = "--port " + port.ToString(CultureInfo.InvariantCulture) + " ";
                using (System.Management.ManagementObjectSearcher searcher = new System.Management.ManagementObjectSearcher(
                    "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name = 'HearthPortableWebServer.Host.exe'"))
                {
                    foreach (System.Management.ManagementObject mo in searcher.Get())
                    {
                        string cmd = (mo["CommandLine"] as string) ?? string.Empty;
                        if (cmd.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            KillProcessTree(Convert.ToInt32(mo["ProcessId"]));
                        }
                    }
                }
            }
            catch { }
        }

        public static bool IsPortOccupiedByOther(int port)
        {
            if (IsRunning(port))
            {
                return false; // Occupied by Hearth or tracked site, expected
            }

            return IsPortListening(port);
        }

        public static bool StartSite(SiteEntry site, out string errorMessage)
        {
            errorMessage = null;
            if (site == null)
            {
                errorMessage = "Site configuration is null.";
                return false;
            }

            if (IsRunning(site.Port))
            {
                return true;
            }

            if (IsPortOccupiedByOther(site.Port))
            {
                errorMessage = "Port " + site.Port + " is already in use by another application.";
                return false;
            }

            string resolvedRoot = ManagerConfig.ResolveRootAbsolute(site.Root);
            try
            {
                Directory.CreateDirectory(resolvedRoot);
                EnsureFolderPermissions(resolvedRoot);
            }
            catch (Exception ex)
            {
                errorMessage = "Failed to prepare root directory: " + ex.Message;
                return false;
            }

            string engine = string.IsNullOrEmpty(site.Engine) ? "ASP.NET" : site.Engine.Trim();

            // 1. Classic ASP.NET Web Forms via Hearth Host.exe
            if (string.Equals(engine, "ASP.NET", StringComparison.OrdinalIgnoreCase))
            {
                string hostExe = HostExePath();
                if (!File.Exists(hostExe))
                {
                    errorMessage = "Host executable not found: " + hostExe;
                    return false;
                }

                string args = string.Format(CultureInfo.InvariantCulture,
                    "--port {0} --root \"{1}\"", site.Port, resolvedRoot);

                ProcessStartInfo psi = new ProcessStartInfo(hostExe, args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = ManagerConfig.GetAppDirectory()
                };

                try
                {
                    Process proc = Process.Start(psi);
                    if (proc != null)
                    {
                        lock (_procLock) { _trackedProcesses[site.Port] = proc; }
                    }

                    for (int i = 0; i < 20; i++)
                    {
                        if (IsRunning(site.Port)) return true;
                        Thread.Sleep(100);
                    }

                    return IsRunning(site.Port);
                }
                catch (Exception ex)
                {
                    errorMessage = "Failed to spawn host process: " + ex.Message;
                    return false;
                }
            }

            // 2. Non-ASP.NET Engines (.NET Core, Python, Node.js, PHP, Other)
            return StartNonAspNetSite(site, resolvedRoot, out errorMessage);
        }

        private static bool StartNonAspNetSite(SiteEntry site, string resolvedRoot, out string errorMessage)
        {
            errorMessage = null;
            string engine = site.Engine ?? "Other";
            string exe = ResolveExecutable(site, resolvedRoot);
            string args = string.Empty;

            if (string.IsNullOrEmpty(exe))
            {
                errorMessage = "Executable could not be found for engine '" + engine + "'. Please specify executable path in Configuration.";
                return false;
            }

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = exe,
                WorkingDirectory = resolvedRoot,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Set port environment variables universally for child processes
            psi.EnvironmentVariables["PORT"] = site.Port.ToString(CultureInfo.InvariantCulture);
            psi.EnvironmentVariables["HTTP_PLATFORM_PORT"] = site.Port.ToString(CultureInfo.InvariantCulture);

            if (string.Equals(engine, ".NET Core", StringComparison.OrdinalIgnoreCase))
            {
                psi.EnvironmentVariables["ASPNETCORE_URLS"] = "http://localhost:" + site.Port;
                psi.EnvironmentVariables["ASPNETCORE_HTTP_PORTS"] = site.Port.ToString(CultureInfo.InvariantCulture);

                if (exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && !string.Equals(Path.GetFileName(exe), "dotnet.exe", StringComparison.OrdinalIgnoreCase))
                {
                    args = "--urls \"http://localhost:" + site.Port + "\"" +
                           (string.IsNullOrWhiteSpace(site.Script) ? "" : (" " + site.Script.Trim()));
                }
                else
                {
                    string script = string.IsNullOrWhiteSpace(site.Script) ? "run" : site.Script.Trim();
                    args = "\"" + script + "\" --urls \"http://localhost:" + site.Port + "\"";
                }
            }
            else if (string.Equals(engine, "Python", StringComparison.OrdinalIgnoreCase))
            {
                psi.EnvironmentVariables["PYTHONUNBUFFERED"] = "1";
                string script = string.IsNullOrWhiteSpace(site.Script) ? "app.py" : site.Script.Trim();
                args = "-u \"" + script + "\"";
            }
            else if (string.Equals(engine, "Node.js", StringComparison.OrdinalIgnoreCase))
            {
                string script = string.IsNullOrWhiteSpace(site.Script) ? "server.js" : site.Script.Trim();
                args = "\"" + script + "\"";
            }
            else if (string.Equals(engine, "PHP", StringComparison.OrdinalIgnoreCase))
            {
                args = string.Format(CultureInfo.InvariantCulture, "-S 127.0.0.1:{0} -t \"{1}\"", site.Port, resolvedRoot);
                if (!string.IsNullOrWhiteSpace(site.Script))
                {
                    args += " \"" + site.Script.Trim() + "\"";
                }
            }
            else // Other
            {
                args = (site.Script ?? "").Replace("{port}", site.Port.ToString(CultureInfo.InvariantCulture))
                                          .Replace("%port%", site.Port.ToString(CultureInfo.InvariantCulture));
            }

            psi.Arguments = args;

            try
            {
                Process proc = Process.Start(psi);
                if (proc == null)
                {
                    errorMessage = "Failed to start process: " + exe;
                    return false;
                }

                lock (_procLock)
                {
                    _trackedProcesses[site.Port] = proc;
                }
                PersistPid(site.Port, proc);

                // Register live-reload file watcher if enabled
                SiteWatcherManager.StartWatcher(site);

                // Wait up to 3 seconds for port to start listening
                for (int i = 0; i < 30; i++)
                {
                    if (IsPortListening(site.Port))
                    {
                        return true;
                    }
                    if (proc.HasExited)
                    {
                        errorMessage = "Process exited immediately with code " + proc.ExitCode + ". Check script or arguments.";
                        return false;
                    }
                    Thread.Sleep(100);
                }

                return IsPortListening(site.Port);
            }
            catch (Exception ex)
            {
                errorMessage = "Execution error: " + ex.Message;
                return false;
            }
        }

        private static string ResolveExecutable(SiteEntry site, string resolvedRoot)
        {
            if (!string.IsNullOrWhiteSpace(site.Executable))
            {
                string exp = Environment.ExpandEnvironmentVariables(site.Executable.Trim());
                if (Path.IsPathRooted(exp) && File.Exists(exp)) return exp;
                string inRoot = Path.Combine(resolvedRoot, exp);
                if (File.Exists(inRoot)) return inRoot;
                return exp; // Return command name to let OS PATH resolve it
            }

            string engine = site.Engine ?? "Other";

            if (string.Equals(engine, ".NET Core", StringComparison.OrdinalIgnoreCase))
            {
                // Check if script points to an exe
                if (!string.IsNullOrWhiteSpace(site.Script) && site.Script.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    string candidate = Path.Combine(resolvedRoot, site.Script);
                    if (File.Exists(candidate)) return candidate;
                }

                // Check if root has any .exe
                try
                {
                    string[] exes = Directory.GetFiles(resolvedRoot, "*.exe");
                    foreach (string e in exes)
                    {
                        string fname = Path.GetFileName(e);
                        if (!fname.StartsWith("HearthPortable", StringComparison.OrdinalIgnoreCase))
                        {
                            return e;
                        }
                    }
                }
                catch { }

                return "dotnet";
            }

            if (string.Equals(engine, "Python", StringComparison.OrdinalIgnoreCase))
            {
                // Auto-detect project virtual environment
                string venv1 = Path.Combine(resolvedRoot, ".venv", "Scripts", "python.exe");
                if (File.Exists(venv1)) return venv1;
                string venv2 = Path.Combine(resolvedRoot, "venv", "Scripts", "python.exe");
                if (File.Exists(venv2)) return venv2;

                return "python";
            }

            if (string.Equals(engine, "Node.js", StringComparison.OrdinalIgnoreCase))
            {
                return "node";
            }

            if (string.Equals(engine, "PHP", StringComparison.OrdinalIgnoreCase))
            {
                return "php";
            }

            return site.Executable ?? string.Empty;
        }

        public static bool StopSite(int port)
        {
            SiteWatcherManager.StopWatcher(port);

            // Signal ASP.NET Host.exe gracefully first
            SyncHelper.TrySignalEvent(IpcNames.ShutdownEvent(port));

            // Stop tracked process tree
            lock (_procLock)
            {
                if (_trackedProcesses.ContainsKey(port))
                {
                    Process proc = _trackedProcesses[port];
                    try
                    {
                        if (!proc.HasExited)
                        {
                            KillProcessTree(proc.Id);
                        }
                    }
                    catch { }
                    _trackedProcesses.Remove(port);
                }
            }

            // Stop a child started by another Manager session / the service (persisted PID)
            int persistedPid = GetPersistedPid(port);
            if (persistedPid > 0)
            {
                KillProcessTree(persistedPid);
            }
            RemovePersisted(port);

            // Wait for the ASP.NET host to honour the graceful shutdown signal
            for (int i = 0; i < 25; i++)
            {
                if (!IsRunning(port)) return true;
                Thread.Sleep(100);
            }

            // Host did not exit: kill only Hearth host processes for this port.
            // Never kill whatever else happens to own the port (IIS, other apps).
            KillHostProcessesForPort(port);
            for (int i = 0; i < 10; i++)
            {
                if (!IsRunning(port)) return true;
                Thread.Sleep(100);
            }

            return !IsRunning(port);
        }

        public static void KillProcessTree(int pid)
        {
            if (pid <= 0) return;
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("taskkill", "/F /T /PID " + pid)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using (Process p = Process.Start(psi))
                {
                    if (p != null) p.WaitForExit(2000);
                }
            }
            catch
            {
                try
                {
                    Process proc = Process.GetProcessById(pid);
                    if (!proc.HasExited) proc.Kill();
                }
                catch { }
            }
        }

        public static int FindPidByPort(int port)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("netstat", "-ano -p tcp")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                using (Process p = Process.Start(psi))
                {
                    if (p == null) return 0;
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(2000);

                    string pattern = @":(" + port + @")\s+.*LISTENING\s+(\d+)";
                    Match match = Regex.Match(output, pattern, RegexOptions.IgnoreCase);
                    if (match.Success && match.Groups.Count >= 3)
                    {
                        int pid;
                        if (int.TryParse(match.Groups[2].Value, out pid))
                        {
                            return pid;
                        }
                    }
                }
            }
            catch { }
            return 0;
        }

        public static void StartAll(IEnumerable<SiteEntry> sites)
        {
            if (sites == null) return;
            foreach (SiteEntry s in sites)
            {
                string err;
                StartSite(s, out err);
            }
        }

        public static void StopAll(IEnumerable<SiteEntry> sites)
        {
            if (sites == null) return;
            foreach (SiteEntry s in sites)
            {
                StopSite(s.Port);
            }
        }

        public static void OpenBrowser(int port)
        {
            string url = string.Format(CultureInfo.InvariantCulture, "http://localhost:{0}/", port);
            ProcessStartInfo psi = new ProcessStartInfo(url) { UseShellExecute = true };
            Process proc = Process.Start(psi);
            if (proc != null) proc.Dispose();
        }

        public static void OpenFolder(string path)
        {
            string abs = ManagerConfig.ResolveRootAbsolute(path);
            if (!Directory.Exists(abs))
            {
                Directory.CreateDirectory(abs);
            }
            ProcessStartInfo psi = new ProcessStartInfo("explorer.exe", "\"" + abs + "\"") { UseShellExecute = true };
            Process proc = Process.Start(psi);
            if (proc != null) proc.Dispose();
        }

        public static void EnsureFolderPermissions(string root)
        {
            try
            {
                Directory.CreateDirectory(root);

                SecurityIdentifier authUsers = new SecurityIdentifier(
                    WellKnownSidType.AuthenticatedUserSid, null);

                FileSystemAccessRule rule = new FileSystemAccessRule(
                    authUsers,
                    FileSystemRights.Modify | FileSystemRights.Synchronize,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow);

                DirectoryInfo info = new DirectoryInfo(root);
                DirectorySecurity security = info.GetAccessControl();
                security.AddAccessRule(rule);
                info.SetAccessControl(security);
            }
            catch
            {
                // Best effort; continue if already permitted
            }
        }
    }
}
