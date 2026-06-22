using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using HearthPortableWebServer.Common;

namespace HearthPortableWebServer.Host
{
    /// <summary>
    /// Installs / removes the Windows Service using sc.exe. The service binPath points
    /// back at this executable with the <c>--service</c> switch; the port and root are
    /// persisted to server.config (read by the service on start, since the SCM passes
    /// no arguments).
    /// </summary>
    public static class Installer
    {
        public static int Install(int port, string root)
        {
            string exe = ExecutablePath();
            string normalizedRoot = ServerController.NormalizeRoot(root);

            ServerConfig config = new ServerConfig(port, normalizedRoot);
            config.Save(ServerConfig.DefaultConfigPath());

            // binPath value must be: "<exe>" --service   (inner quotes escaped for sc)
            string binValue = "\\\"" + exe + "\\\" --service";
            string serviceName = IpcNames.ServiceName(port);
            string createArgs =
                "create " + serviceName +
                " binPath= \"" + binValue + "\"" +
                " start= auto" +
                " DisplayName= \"" + IpcNames.ServiceDisplayName(port) + "\"";

            int rc = RunSc(createArgs);
            if (rc != 0)
            {
                Console.Error.WriteLine("sc create failed (exit " + rc + "). Run as Administrator.");
                return rc;
            }

            RunSc("description " + serviceName + " \"" + IpcNames.ServiceDescription + "\"");

            // The service runs as LocalSystem, so files it writes would otherwise be
            // owned by SYSTEM and read-only to the interactive user. Grant Authenticated
            // Users full (Modify) access on the web root, inherited by all current and
            // future files/subfolders, so the logged-in user can freely read/write/delete
            // whatever the web app creates. To lock this down, an administrator can change
            // the service Log On account AND tighten this folder's permissions.
            GrantAuthenticatedUsersWrite(normalizedRoot);

            Console.WriteLine("Service '" + serviceName + "' installed (auto-start, port " + port + ").");
            Console.WriteLine("Web root '" + normalizedRoot + "' is writable by authenticated users.");
            return 0;
        }

        public static int Uninstall(int port)
        {
            string serviceName = IpcNames.ServiceName(port);
            RunSc("stop " + serviceName);
            int rc = RunSc("delete " + serviceName);
            if (rc != 0)
            {
                Console.Error.WriteLine("sc delete failed (exit " + rc + "). Run as Administrator.");
                return rc;
            }
            Console.WriteLine("Service '" + serviceName + "' uninstalled.");
            return 0;
        }

        /// <summary>
        /// Grants "Authenticated Users" Modify rights on the web root, inheritable by all
        /// files and subfolders, so files written by the LocalSystem service remain fully
        /// accessible to the interactive user. Uses the well-known SID (not the localized
        /// account name) so it works on non-English Windows. Best-effort: a failure here
        /// is logged but does not fail the install.
        /// </summary>
        private static void GrantAuthenticatedUsersWrite(string root)
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
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    "Warning: could not set web-root permissions (" + ex.Message + "). " +
                    "Files created by the service may be read-only to your user account.");
            }
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

        private static int RunSc(string arguments)
        {
            ProcessStartInfo psi = new ProcessStartInfo("sc.exe", arguments);
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;

            using (Process process = Process.Start(psi))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(output))
                {
                    Console.WriteLine(output.Trim());
                }
                if (!string.IsNullOrEmpty(error))
                {
                    Console.Error.WriteLine(error.Trim());
                }
                return process.ExitCode;
            }
        }
    }
}
