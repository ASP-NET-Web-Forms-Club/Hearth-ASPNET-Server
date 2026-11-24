using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using Microsoft.Win32;

namespace HearthPortableWebServer.Manager.Services
{
    public static class ServiceControllerHelper
    {
        public const string DefaultServiceName = "HearthManagerService";
        public const string DisplayName = "Hearth Multi-Site Server Manager";
        public const string Description = "Background manager and auto-boot supervisor for Hearth Portable ASP.NET Web Server instances.";

        /// <summary>
        /// Returns the Windows Service name registered for THIS executable's folder
        /// (whether the default name or a custom/secondary name), or DefaultServiceName if none is installed.
        /// </summary>
        public static string ServiceName
        {
            get { return FindServiceNameForThisExe() ?? DefaultServiceName; }
        }

        /// <summary>
        /// Finds any installed Windows Service whose ImagePath points to THIS Manager.exe.
        /// Works for non-elevated users because HKLM\SYSTEM\CurrentControlSet\Services is readable by Users.
        /// </summary>
        public static string FindServiceNameForThisExe()
        {
            string thisExe = NormalizePath(ExecutablePath());
            if (string.IsNullOrEmpty(thisExe)) return null;

            try
            {
                using (RegistryKey servicesKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services", false))
                {
                    if (servicesKey == null) return null;

                    // Fast path: check the default service name first.
                    if (ServiceKeyMatchesExe(servicesKey, DefaultServiceName, thisExe))
                    {
                        return DefaultServiceName;
                    }

                    // Scan all installed services so manually-registered or secondary-folder services are also found.
                    foreach (string subKeyName in servicesKey.GetSubKeyNames())
                    {
                        if (string.Equals(subKeyName, DefaultServiceName, StringComparison.OrdinalIgnoreCase)) continue;
                        if (ServiceKeyMatchesExe(servicesKey, subKeyName, thisExe))
                        {
                            return subKeyName;
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private static bool ServiceKeyMatchesExe(RegistryKey servicesKey, string serviceName, string normalizedThisExe)
        {
            try
            {
                using (RegistryKey svcKey = servicesKey.OpenSubKey(serviceName, false))
                {
                    if (svcKey == null) return false;
                    string imagePath = svcKey.GetValue("ImagePath") as string;
                    if (string.IsNullOrEmpty(imagePath)) return false;

                    string exePath = ExtractExeFromImagePath(imagePath);
                    if (string.IsNullOrEmpty(exePath)) return false;

                    return string.Equals(NormalizePath(exePath), normalizedThisExe, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                return false;
            }
        }

        private static string ExtractExeFromImagePath(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath)) return null;
            string expanded = Environment.ExpandEnvironmentVariables(imagePath.Trim());
            if (expanded.StartsWith("\""))
            {
                int endQuote = expanded.IndexOf('"', 1);
                if (endQuote > 1)
                {
                    return expanded.Substring(1, endQuote - 1);
                }
            }

            int exeIdx = expanded.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            if (exeIdx > 0)
            {
                return expanded.Substring(0, exeIdx + 4).Trim('"');
            }

            int spaceIdx = expanded.IndexOf(' ');
            return spaceIdx > 0 ? expanded.Substring(0, spaceIdx) : expanded;
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            try { return Path.GetFullPath(path.Trim()); }
            catch { return path.Trim(); }
        }

        private static bool ServiceNameExists(string name)
        {
            try
            {
                ServiceController[] services = ServiceController.GetServices();
                for (int i = 0; i < services.Length; i++)
                {
                    bool match = string.Equals(services[i].ServiceName, name, StringComparison.OrdinalIgnoreCase);
                    services[i].Dispose();
                    if (match) return true;
                }
            }
            catch { }
            return false;
        }

        public static bool IsServiceInstalled()
        {
            return FindServiceNameForThisExe() != null;
        }

        public static string GetServiceStatusText()
        {
            try
            {
                string svcName = FindServiceNameForThisExe();
                if (string.IsNullOrEmpty(svcName))
                {
                    return "Not Installed";
                }
                using (ServiceController sc = new ServiceController(svcName))
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
            string existingForThisExe = FindServiceNameForThisExe();
            if (!string.IsNullOrEmpty(existingForThisExe))
            {
                return true;
            }

            string svcName = DefaultServiceName;
            string displayName = DisplayName;
            if (ServiceNameExists(DefaultServiceName))
            {
                // Another folder already owns HearthManagerService; register a folder-scoped name.
                svcName = DefaultServiceName + "_" + ManagerConfig.GetInstanceId().Substring(0, 8);
                string folderName = Path.GetFileName(ManagerConfig.GetAppDirectory().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                displayName = string.IsNullOrEmpty(folderName)
                    ? (DisplayName + " (" + svcName + ")")
                    : (DisplayName + " (" + folderName + ")");
            }

            string exe = ExecutablePath();
            string binValue = "\\\"" + exe + "\\\" --service";
            string createArgs = string.Format("create {0} binPath= \"{1}\" start= auto DisplayName= \"{2}\"",
                svcName, binValue, displayName);

            int rc = RunElevated("sc.exe", createArgs);
            if (rc != 0)
            {
                errorMessage = "Failed to create service (sc exit code " + rc + "). Run as Administrator.";
                return false;
            }

            RunElevated("sc.exe", "description " + svcName + " \"" + Description + "\"");
            return true;
        }

        public static bool UninstallService(out string errorMessage)
        {
            errorMessage = null;
            string svcName = FindServiceNameForThisExe();
            if (string.IsNullOrEmpty(svcName))
            {
                return true;
            }

            RunElevated("sc.exe", "stop " + svcName);
            int rc = RunElevated("sc.exe", "delete " + svcName);
            if (rc != 0)
            {
                errorMessage = "Failed to delete service (sc exit code " + rc + "). Run as Administrator.";
                return false;
            }
            return true;
        }

        public static bool StartService()
        {
            string svcName = FindServiceNameForThisExe();
            if (string.IsNullOrEmpty(svcName)) return false;
            return RunElevated("sc.exe", "start " + svcName) == 0;
        }

        public static bool StopService()
        {
            string svcName = FindServiceNameForThisExe();
            if (string.IsNullOrEmpty(svcName)) return false;
            return RunElevated("sc.exe", "stop " + svcName) == 0;
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
