using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using HearthPortableWebServer.Common;

namespace HearthPortableWebServer.Launcher
{
    /// <summary>
    /// Starts / stops / queries the Host as a SEPARATE, DETACHED process. Because the
    /// Host runs in its own process (not as a child that is force-killed), closing this
    /// launcher leaves the web server running.
    /// </summary>
    internal static class HostProcessManager
    {
        public static string HostExePath()
        {
            return Path.Combine(Application.StartupPath, "HearthPortableWebServer.Host.exe");
        }

        public static bool IsRunning(int port)
        {
            return SyncHelper.MutexExists(IpcNames.RunningMutex(port));
        }

        /// <summary>True when anything (Hearth or another application) is listening on the port.</summary>
        public static bool IsPortListening(int port)
        {
            try
            {
                System.Net.IPEndPoint[] listeners =
                    System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
                foreach (System.Net.IPEndPoint ep in listeners)
                {
                    if (ep.Port == port) return true;
                }
            }
            catch { }
            return false;
        }

        /// <summary>True when the port is held by something other than a Hearth host (e.g. IIS).</summary>
        public static bool IsPortOccupiedByOther(int port)
        {
            return !IsRunning(port) && IsPortListening(port);
        }

        public static void StartServer(int port, string root)
        {
            if (IsRunning(port))
            {
                return;
            }

            if (IsPortOccupiedByOther(port))
            {
                throw new InvalidOperationException(
                    "Port " + port.ToString(CultureInfo.InvariantCulture) +
                    " is already in use by another application. Please choose a different port.");
            }

            string exe = HostExePath();
            if (!File.Exists(exe))
            {
                throw new FileNotFoundException("Host executable not found.", exe);
            }

            string args =
                "--port " + port.ToString(CultureInfo.InvariantCulture) +
                " --root \"" + root + "\"";

            ProcessStartInfo psi = new ProcessStartInfo(exe, args);
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.WorkingDirectory = Application.StartupPath;

            // Fire and forget: we deliberately do NOT keep ownership of the process,
            // so the server survives the launcher being closed.
            Process process = Process.Start(psi);
            if (process != null)
            {
                process.Dispose();
            }
        }

        /// <summary>
        /// Stops the Hearth host on this port: graceful shutdown signal first, then (if the
        /// host does not exit) a force-kill of HearthPortableWebServer.Host.exe instances
        /// serving this port. Never touches processes that are not Hearth hosts.
        /// Returns false when no Hearth server was found on the port.
        /// </summary>
        public static bool StopServer(int port)
        {
            bool signalled = SyncHelper.TrySignalEvent(IpcNames.ShutdownEvent(port));
            if (!signalled && !IsRunning(port))
            {
                return false;
            }

            // Wait up to 3 seconds for a graceful exit
            for (int i = 0; i < 30; i++)
            {
                if (!IsRunning(port)) return true;
                System.Threading.Thread.Sleep(100);
            }

            KillHostProcessesForPort(port);
            for (int i = 0; i < 10; i++)
            {
                if (!IsRunning(port)) return true;
                System.Threading.Thread.Sleep(100);
            }
            return true;
        }

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
                        if (cmd.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0) continue;
                        try
                        {
                            using (Process p = Process.GetProcessById(Convert.ToInt32(mo["ProcessId"])))
                            {
                                p.Kill();
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        public static void OpenBrowser(int port)
        {
            string url = "http://localhost:" + port.ToString(CultureInfo.InvariantCulture) + "/";
            ProcessStartInfo psi = new ProcessStartInfo(url);
            psi.UseShellExecute = true;
            Process browser = Process.Start(psi);
            if (browser != null)
            {
                browser.Dispose();
            }
        }
    }
}
