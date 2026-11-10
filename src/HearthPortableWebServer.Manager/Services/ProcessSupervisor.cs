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

        // The UI (user) and the service (LocalSystem) both read-modify-write the tracked file,
        // so an in-process lock is not enough. A Global named mutex, open to all users,
        // serialises access across processes and sessions.
        private const string TrackedMutexName = @"Global\HearthPortableWebServer.TrackedProcesses";

        private static Mutex OpenTrackedMutex()
        {
            try
            {
                MutexSecurity sec = new MutexSecurity();
                sec.AddAccessRule(new MutexAccessRule(
                    new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                    MutexRights.Synchronize | MutexRights.Modify,
                    AccessControlType.Allow));
                bool created;
                return new Mutex(false, TrackedMutexName, out created, sec);
            }
            catch
            {
                try { return Mutex.OpenExisting(TrackedMutexName); } catch { return null; }
            }
        }

        /// <summary>Runs an action on the tracked file under both the in-process and the cross-process lock.</summary>
        private static void WithTrackedFileLock(Action action)
        {
            lock (_fileLock)
            {
                Mutex m = OpenTrackedMutex();
                bool owned = false;
                try
                {
                    if (m != null)
                    {
                        try { owned = m.WaitOne(5000); }
                        catch (AbandonedMutexException) { owned = true; }
                    }
                    action();
                }
                finally
                {
                    if (m != null)
                    {
                        if (owned) { try { m.ReleaseMutex(); } catch { } }
                        m.Dispose();
                    }
                }
            }
        }

        private static string TrackedFilePath()
        {
            return Path.Combine(ManagerConfig.GetAppDirectory(), TrackedFileName);
        }

        // Caller must hold WithTrackedFileLock.
        private static Dictionary<int, KeyValuePair<int, string>> ReadPersistedUnlocked()
        {
            Dictionary<int, KeyValuePair<int, string>> map = new Dictionary<int, KeyValuePair<int, string>>();
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
            catch (Exception ex)
            {
                Trace.TraceWarning("Hearth: failed to read " + TrackedFileName + ": " + ex.Message);
            }
            return map;
        }

        // Caller must hold WithTrackedFileLock. Retries briefly so a transient sharing
        // violation does not silently drop a PID (which would orphan that process).
        private static void WritePersistedUnlocked(Dictionary<int, KeyValuePair<int, string>> map)
        {
            string path = TrackedFilePath();
            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
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
                    return;
                }
                catch (Exception ex)
                {
                    if (attempt == 4)
                    {
                        Trace.TraceError("Hearth: failed to write " + TrackedFileName + ": " + ex.Message);
                        return;
                    }
                    Thread.Sleep(100);
                }
            }
        }

        private static Dictionary<int, KeyValuePair<int, string>> LoadPersisted()
        {
            Dictionary<int, KeyValuePair<int, string>> map = null;
            WithTrackedFileLock(() => { map = ReadPersistedUnlocked(); });
            return map ?? new Dictionary<int, KeyValuePair<int, string>>();
        }

        private static void PersistPid(int port, Process proc)
        {
            try
            {
                int pid = proc.Id;
                string name = proc.ProcessName;
                WithTrackedFileLock(() =>
                {
                    Dictionary<int, KeyValuePair<int, string>> map = ReadPersistedUnlocked();
                    map[port] = new KeyValuePair<int, string>(pid, name);
                    WritePersistedUnlocked(map);
                });
            }
            catch { }
        }

        private static void RemovePersisted(int port)
        {
            WithTrackedFileLock(() =>
            {
                Dictionary<int, KeyValuePair<int, string>> map = ReadPersistedUnlocked();
                if (map.Remove(port)) WritePersistedUnlocked(map);
            });
        }

        /// <summary>
        /// Stops every process Hearth started (tracked on disk or in memory) whose port is no
        /// longer assigned to any configured site — e.g. after a site was deleted or its port/engine changed.
        /// </summary>
        public static void StopOrphans(IEnumerable<SiteEntry> sites)
        {
            HashSet<int> configured = new HashSet<int>();
            if (sites != null)
            {
                foreach (SiteEntry s in sites) configured.Add(s.Port);
            }

            HashSet<int> ports = new HashSet<int>(LoadPersisted().Keys);
            lock (_procLock)
            {
                foreach (int p in _trackedProcesses.Keys) ports.Add(p);
            }

            foreach (int port in ports)
            {
                if (!configured.Contains(port))
                {
                    StopSite(port);
                }
            }
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

        // ---- Missing-runtime memory (in-process only) ----
        // When a site's runtime (PHP / Python / Node.js / .NET) cannot be found, remember it so
        // silent auto-start / auto-restart does not retry it on every watchdog tick. The key
        // includes engine + executable, so changing the site's configuration re-evaluates it.
        // A manual Start from the UI always retries.
        private static readonly HashSet<string> _missingRuntime = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _missingLock = new object();

        private static string RuntimeKey(SiteEntry site)
        {
            return site.Port.ToString(CultureInfo.InvariantCulture) + "|" + (site.Engine ?? "") + "|" + (site.Executable ?? "").Trim();
        }

        public static bool IsRuntimeMissing(SiteEntry site)
        {
            if (site == null) return false;
            lock (_missingLock) { return _missingRuntime.Contains(RuntimeKey(site)); }
        }

        private static void SetRuntimeMissing(SiteEntry site, bool missing)
        {
            lock (_missingLock)
            {
                if (missing) _missingRuntime.Add(RuntimeKey(site));
                else _missingRuntime.Remove(RuntimeKey(site));
            }
        }

        private static string RuntimeDisplayName(string engine)
        {
            if (string.Equals(engine, ".NET Core", StringComparison.OrdinalIgnoreCase)) return ".NET";
            return string.IsNullOrEmpty(engine) ? "runtime" : engine;
        }

        private static string MissingRuntimeMessage(string engine)
        {
            return "No " + RuntimeDisplayName(engine) + " runtime found." + Environment.NewLine + Environment.NewLine +
                   "Please download the full Hearth package with PHP / Python / Node.js runtimes bundled, " +
                   "or install the runtime and set its path in the site's Executable field.";
        }

        /// <summary>Manual start (e.g. the Start Server button): always attempts, even if the runtime was missing before.</summary>
        public static bool StartSite(SiteEntry site, out string errorMessage)
        {
            return StartSite(site, false, out errorMessage);
        }

        /// <summary>
        /// Starts a site. When <paramref name="automatic"/> is true (service watchdog, Start All on boot,
        /// live reload), a site whose runtime is already known to be missing is skipped silently.
        /// </summary>
        public static bool StartSite(SiteEntry site, bool automatic, out string errorMessage)
        {
            errorMessage = null;
            if (site == null)
            {
                errorMessage = "Site configuration is null.";
                return false;
            }

            if (automatic && IsRuntimeMissing(site))
            {
                errorMessage = MissingRuntimeMessage(site.Engine);
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
                SetRuntimeMissing(site, true);
                errorMessage = MissingRuntimeMessage(engine);
                return false;
            }
            SetRuntimeMissing(site, false);

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

            // Prepend the resolved runtime directory (and local node_modules\.bin if present)
            // to the child process PATH so sibling tools (npm, pip, etc.) resolve automatically.
            try
            {
                List<string> pathAdditions = new List<string>();
                if (Path.IsPathRooted(exe))
                {
                    string exeDir = Path.GetDirectoryName(exe);
                    if (!string.IsNullOrEmpty(exeDir) && Directory.Exists(exeDir))
                    {
                        pathAdditions.Add(exeDir);
                    }
                }
                if (string.Equals(engine, "Node.js", StringComparison.OrdinalIgnoreCase))
                {
                    string localNodeBin = Path.Combine(resolvedRoot, "node_modules", ".bin");
                    if (Directory.Exists(localNodeBin))
                    {
                        pathAdditions.Add(localNodeBin);
                    }
                }
                if (pathAdditions.Count > 0)
                {
                    string existingPath = psi.EnvironmentVariables["PATH"] ?? Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                    psi.EnvironmentVariables["PATH"] = string.Join(";", pathAdditions.ToArray()) + ";" + existingPath;
                }
            }
            catch { }

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
            catch (System.ComponentModel.Win32Exception wex) when (wex.NativeErrorCode == 2 || wex.NativeErrorCode == 3)
            {
                // ERROR_FILE_NOT_FOUND / ERROR_PATH_NOT_FOUND: the runtime executable is not there.
                SetRuntimeMissing(site, true);
                errorMessage = MissingRuntimeMessage(engine);
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = "Execution error: " + ex.Message;
                return false;
            }
        }

        /// <summary>Finds a bare command name (e.g. "php") on the system PATH, honouring PATHEXT. Returns null if absent.</summary>
        private static string FindOnPath(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return null;
            try
            {
                List<string> names = new List<string>();
                if (Path.HasExtension(command)) names.Add(command);
                string pathExt = Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD";
                foreach (string ext in pathExt.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    names.Add(command + ext);
                }

                string path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                foreach (string dir in path.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string d = dir.Trim().Trim('"');
                    if (d.Length == 0) continue;
                    foreach (string n in names)
                    {
                        try
                        {
                            string candidate = Path.Combine(d, n);
                            if (File.Exists(candidate)) return candidate;
                        }
                        catch { }
                    }
                }
            }
            catch { }
            return null;
        }

        private static string ResolveExecutable(SiteEntry site, string resolvedRoot)
        {
            string appDir = ManagerConfig.GetAppDirectory();

            // 1. Check explicitly configured executable in site settings first
            if (!string.IsNullOrWhiteSpace(site.Executable))
            {
                string exp = Environment.ExpandEnvironmentVariables(site.Executable.Trim());
                try
                {
                    if (Path.IsPathRooted(exp))
                    {
                        if (File.Exists(exp)) return Path.GetFullPath(exp);
                    }
                    else
                    {
                        string inAppDir = Path.GetFullPath(Path.Combine(appDir, exp));
                        if (File.Exists(inAppDir)) return inAppDir;

                        string inRoot = Path.GetFullPath(Path.Combine(resolvedRoot, exp));
                        if (File.Exists(inRoot)) return inRoot;
                    }
                }
                catch { }

                // If explicit command name has no path separators (e.g. "python3"), let OS PATH resolve it;
                // otherwise fall through to bundled runtime & system PATH discovery below.
                if (exp.IndexOf(Path.DirectorySeparatorChar) < 0 && exp.IndexOf(Path.AltDirectorySeparatorChar) < 0)
                {
                    string onPath = FindOnPath(exp);
                    if (onPath != null) return onPath;
                }
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

                string bundledDotnet = Path.Combine(appDir, "runtimes", "dotnet", "dotnet.exe");
                if (File.Exists(bundledDotnet)) return bundledDotnet;

                return FindOnPath("dotnet");
            }

            if (string.Equals(engine, "Python", StringComparison.OrdinalIgnoreCase))
            {
                // Auto-detect project virtual environment
                string venv1 = Path.Combine(resolvedRoot, ".venv", "Scripts", "python.exe");
                if (File.Exists(venv1)) return venv1;
                string venv2 = Path.Combine(resolvedRoot, "venv", "Scripts", "python.exe");
                if (File.Exists(venv2)) return venv2;

                // Scan bundled runtime
                string bundledPython = Path.Combine(appDir, "runtimes", "python", "python.exe");
                if (File.Exists(bundledPython)) return bundledPython;

                // Fall back to system PATH (null if not installed)
                return FindOnPath("python");
            }

            if (string.Equals(engine, "Node.js", StringComparison.OrdinalIgnoreCase))
            {
                // Scan bundled runtime
                string bundledNode = Path.Combine(appDir, "runtimes", "node", "node.exe");
                if (File.Exists(bundledNode)) return bundledNode;

                // Fall back to system PATH (null if not installed)
                return FindOnPath("node");
            }

            if (string.Equals(engine, "PHP", StringComparison.OrdinalIgnoreCase))
            {
                // Scan bundled runtime
                string bundledPhp = Path.Combine(appDir, "runtimes", "php", "php.exe");
                if (File.Exists(bundledPhp)) return bundledPhp;

                // Fall back to system PATH (null if not installed)
                return FindOnPath("php");
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
            if (GetPersistedPid(port) == 0)
            {
                RemovePersisted(port);
            }

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

        /// <summary>Automatic start of all sites (e.g. --start-all): skips sites whose runtime is known missing.</summary>
        public static void StartAll(IEnumerable<SiteEntry> sites)
        {
            StartAll(sites, true, null);
        }

        /// <summary>
        /// Starts all sites. Manual (automatic = false) always retries; any per-site errors are
        /// collected into <paramref name="errors"/> when provided.
        /// </summary>
        public static void StartAll(IEnumerable<SiteEntry> sites, bool automatic, List<string> errors)
        {
            if (sites == null) return;
            foreach (SiteEntry s in sites)
            {
                string err;
                if (!StartSite(s, automatic, out err) && errors != null && !string.IsNullOrEmpty(err))
                {
                    errors.Add(s.Name + " (port " + s.Port + "): " + err);
                }
            }
        }

        public static void StopAll(IEnumerable<SiteEntry> sites)
        {
            if (sites == null) return;
            foreach (SiteEntry s in sites)
            {
                StopSite(s.Port);
            }
            // Also stop anything Hearth started on ports no longer in the config.
            StopOrphans(sites);
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
