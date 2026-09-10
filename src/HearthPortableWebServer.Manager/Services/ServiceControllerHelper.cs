using System;
using System.Diagnostics;
using System.Reflection;
using System.ServiceProcess;

namespace HearthPortableWebServer.Manager.Services
{
    public static class ServiceControllerHelper
    {
        public const string ServiceName = "HearthManagerService";
        public const string DisplayName = "Hearth Multi-Site Server Manager";
        public const string Description = "Background manager and auto-boot supervisor for Hearth Portable ASP.NET Web Server instances.";

        public static bool IsServiceInstalled()
        {
            try
            {
                ServiceController[] services = ServiceController.GetServices();
                for (int i = 0; i < services.Length; i++)
                {
                    bool match = string.Equals(services[i].ServiceName, ServiceName, StringComparison.OrdinalIgnoreCase);
                    services[i].Dispose();
                    if (match) return true;
                }
            }
            catch { }
            return false;
        }

        public static string GetServiceStatusText()
        {
            try
            {
                if (!IsServiceInstalled())
                {
                    return "Not Installed";
                }
                using (ServiceController sc = new ServiceController(ServiceName))
                {
                    return sc.Status.ToString();
                }
            }
            catch (Exception ex)
            {
                return "Unknown (" + ex.Message + ")";
            }
        }

        public static bool InstallService(out string errorMessage)
        {
            errorMessage = null;
            string exe = ExecutablePath();
            string binValue = "\\\"" + exe + "\\\" --service";
            string createArgs = string.Format("create {0} binPath= \"{1}\" start= auto DisplayName= \"{2}\"",
                ServiceName, binValue, DisplayName);

            int rc = RunElevated("sc.exe", createArgs);
            if (rc != 0)
            {
                errorMessage = "Failed to create service (sc exit code " + rc + "). Run as Administrator.";
                return false;
            }

            RunElevated("sc.exe", "description " + ServiceName + " \"" + Description + "\"");
            return true;
        }

        public static bool UninstallService(out string errorMessage)
        {
            errorMessage = null;
            RunElevated("sc.exe", "stop " + ServiceName);
            int rc = RunElevated("sc.exe", "delete " + ServiceName);
            if (rc != 0)
            {
                errorMessage = "Failed to delete service (sc exit code " + rc + "). Run as Administrator.";
                return false;
            }
            return true;
        }

        public static bool StartService()
        {
            return RunElevated("sc.exe", "start " + ServiceName) == 0;
        }

        public static bool StopService()
        {
            return RunElevated("sc.exe", "stop " + ServiceName) == 0;
        }

        private static string ExecutablePath()
        {
            Assembly entry = Assembly.GetEntryAssembly();
            if (entry != null && !string.IsNullOrEmpty(entry.Location))
            {
                return entry.Location;
            }
            return Process.GetCurrentProcess().MainModule.FileName;
        }

        private static int RunElevated(string fileName, string arguments)
        {
            ProcessStartInfo psi = new ProcessStartInfo(fileName, arguments)
            {
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            };

            try
            {
                using (Process proc = Process.Start(psi))
                {
                    if (proc == null) return -1;
                    proc.WaitForExit();
                    return proc.ExitCode;
                }
            }
            catch
            {
                return -1; // UAC declined or failed
            }
        }
    }
}
