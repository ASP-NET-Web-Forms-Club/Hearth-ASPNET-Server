using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.NetworkInformation;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using HearthPortableWebServer.Common;
using HearthPortableWebServer.Manager.Models;

namespace HearthPortableWebServer.Manager.Services
{
    public static class ProcessSupervisor
    {
        public static string HostExePath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HearthPortableWebServer.Host.exe");
        }

        public static bool IsRunning(int port)
        {
            return SyncHelper.MutexExists(IpcNames.RunningMutex(port));
        }

        public static bool IsPortOccupiedByOther(int port)
        {
            if (IsRunning(port))
            {
                return false; // Occupied by Hearth, which is expected
            }

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

            string hostExe = HostExePath();
            if (!File.Exists(hostExe))
            {
                errorMessage = "Host executable not found: " + hostExe;
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

            string args = string.Format(CultureInfo.InvariantCulture,
                "--port {0} --root \"{1}\"", site.Port, resolvedRoot);

            ProcessStartInfo psi = new ProcessStartInfo(hostExe, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            };

            try
            {
                Process proc = Process.Start(psi);
                if (proc != null)
                {
                    proc.Dispose();
                }

                // Wait up to 1.5 seconds for mutex to indicate active status
                for (int i = 0; i < 15; i++)
                {
                    if (IsRunning(site.Port))
                    {
                        return true;
                    }
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

        public static bool StopSite(int port)
        {
            if (!IsRunning(port))
            {
                return true;
            }

            bool signaled = SyncHelper.TrySignalEvent(IpcNames.ShutdownEvent(port));
            if (!signaled)
            {
                return false;
            }

            // Wait up to 2 seconds for server to exit
            for (int i = 0; i < 20; i++)
            {
                if (!IsRunning(port))
                {
                    return true;
                }
                Thread.Sleep(100);
            }

            return !IsRunning(port);
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
