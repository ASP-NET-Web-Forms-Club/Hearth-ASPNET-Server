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

        public static void StartServer(int port, string root)
        {
            if (IsRunning(port))
            {
                return;
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

        public static bool StopServer(int port)
        {
            return SyncHelper.TrySignalEvent(IpcNames.ShutdownEvent(port));
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
