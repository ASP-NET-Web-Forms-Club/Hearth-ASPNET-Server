using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.ServiceProcess;
using System.Windows.Forms;
using HearthPortableWebServer.Common;

namespace HearthPortableWebServer.Launcher
{
    /// <summary>
    /// Installs / removes / controls the Windows Service. Operations that change machine
    /// state are launched elevated (UAC) via the Host's own --install / --uninstall
    /// switches and sc.exe. Status queries run unelevated.
    /// </summary>
    internal static class ServiceManager
    {
        public static bool ServiceExists(int port)
        {
            string target = IpcNames.ServiceName(port);
            ServiceController[] services = ServiceController.GetServices();
            for (int i = 0; i < services.Length; i++)
            {
                bool match = string.Equals(services[i].ServiceName, target,
                    StringComparison.OrdinalIgnoreCase);
                services[i].Dispose();
                if (match)
                {
                    return true;
                }
            }
            return false;
        }

        public static string ServiceStatusText(int port)
        {
            try
            {
                if (!ServiceExists(port))
                {
                    return "Not installed";
                }
                using (ServiceController sc = new ServiceController(IpcNames.ServiceName(port)))
                {
                    return "Installed - " + sc.Status.ToString();
                }
            }
            catch (Exception ex)
            {
                return "Unknown (" + ex.Message + ")";
            }
        }

        public static bool Install(int port, string root)
        {
            string exe = HostProcessManager.HostExePath();
            if (!File.Exists(exe))
            {
                throw new FileNotFoundException("Host executable not found.", exe);
            }

            string args =
                "--install --port " + port.ToString(CultureInfo.InvariantCulture) +
                " --root \"" + root + "\"";
            return RunElevated(exe, args);
        }

        public static bool Uninstall(int port)
        {
            string exe = HostProcessManager.HostExePath();
            // Pass the port explicitly so the Host removes the matching port-based service,
            // rather than relying on whatever is currently in server.config.
            string args = "--uninstall --port " + port.ToString(CultureInfo.InvariantCulture);
            return RunElevated(exe, args);
        }

        public static bool StartService(int port)
        {
            return RunElevated("sc.exe", "start " + IpcNames.ServiceName(port));
        }

        public static bool StopService(int port)
        {
            return RunElevated("sc.exe", "stop " + IpcNames.ServiceName(port));
        }

        /// <summary>
        /// Runs a program elevated (UAC prompt) and waits for it to finish.
        /// Returns true when the process exits with code 0.
        /// </summary>
        private static bool RunElevated(string fileName, string arguments)
        {
            ProcessStartInfo psi = new ProcessStartInfo(fileName, arguments);
            psi.UseShellExecute = true;
            psi.Verb = "runas";
            psi.WindowStyle = ProcessWindowStyle.Hidden;

            try
            {
                using (Process process = Process.Start(psi))
                {
                    if (process == null)
                    {
                        return false;
                    }
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // User declined the UAC elevation prompt.
                return false;
            }
        }
    }
}
