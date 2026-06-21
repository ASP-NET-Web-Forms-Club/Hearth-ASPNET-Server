using System;
using System.Diagnostics;
using System.Reflection;
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
            string createArgs =
                "create " + IpcNames.ServiceName +
                " binPath= \"" + binValue + "\"" +
                " start= auto" +
                " DisplayName= \"" + IpcNames.ServiceDisplayName + "\"";

            int rc = RunSc(createArgs);
            if (rc != 0)
            {
                Console.Error.WriteLine("sc create failed (exit " + rc + "). Run as Administrator.");
                return rc;
            }

            RunSc("description " + IpcNames.ServiceName + " \"" + IpcNames.ServiceDescription + "\"");
            Console.WriteLine("Service '" + IpcNames.ServiceName + "' installed (auto-start, port " + port + ").");
            return 0;
        }

        public static int Uninstall()
        {
            RunSc("stop " + IpcNames.ServiceName);
            int rc = RunSc("delete " + IpcNames.ServiceName);
            if (rc != 0)
            {
                Console.Error.WriteLine("sc delete failed (exit " + rc + "). Run as Administrator.");
                return rc;
            }
            Console.WriteLine("Service '" + IpcNames.ServiceName + "' uninstalled.");
            return 0;
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
