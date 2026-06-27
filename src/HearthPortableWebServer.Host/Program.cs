using System;
using System.Globalization;
using System.ServiceProcess;
using System.Threading;
using HearthPortableWebServer.Common;

namespace HearthPortableWebServer.Host
{
    internal enum HostMode
    {
        Console,
        Service,
        Install,
        Uninstall,
        Stop,
        Help
    }

    internal sealed class Options
    {
        public HostMode Mode = HostMode.Console;
        public int Port = ServerConfig.DefaultPort;
        public string Root = string.Empty;

        public static Options Parse(string[] args)
        {
            Options o = new Options();
            bool portSet = false;
            bool rootSet = false;

            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i].ToLowerInvariant();
                switch (a)
                {
                    case "--service":
                    case "-service":
                        o.Mode = HostMode.Service;
                        break;
                    case "--install":
                        o.Mode = HostMode.Install;
                        break;
                    case "--uninstall":
                        o.Mode = HostMode.Uninstall;
                        break;
                    case "--stop":
                        o.Mode = HostMode.Stop;
                        break;
                    case "--help":
                    case "-h":
                    case "/?":
                        o.Mode = HostMode.Help;
                        break;
                    case "--port":
                    case "-p":
                        if (i + 1 < args.Length)
                        {
                            int p;
                            if (int.TryParse(args[++i], NumberStyles.Integer, CultureInfo.InvariantCulture, out p))
                            {
                                o.Port = p;
                                portSet = true;
                            }
                        }
                        break;
                    case "--root":
                    case "-r":
                        if (i + 1 < args.Length)
                        {
                            o.Root = args[++i];
                            rootSet = true;
                        }
                        break;
                }
            }

            // For service mode the SCM supplies no arguments, so fall back to server.config.
            // Uninstall also falls back to config so it can target the correct port-based
            // service name when invoked without an explicit --port.
            if ((o.Mode == HostMode.Service || o.Mode == HostMode.Uninstall) && (!portSet || !rootSet))
            {
                ServerConfig config = ServerConfig.Load(ServerConfig.DefaultConfigPath());
                if (!portSet)
                {
                    o.Port = config.Port;
                }
                if (!rootSet)
                {
                    o.Root = config.Root;
                }
            }

            return o;
        }
    }

    internal static class Program
    {
        private static int Main(string[] args)
        {
            Options o = Options.Parse(args);

            switch (o.Mode)
            {
                case HostMode.Service:
                    ServiceBase.Run(new WebServerService(o.Port, o.Root));
                    return 0;
                case HostMode.Install:
                    return Installer.Install(o.Port, o.Root);
                case HostMode.Uninstall:
                    return Installer.Uninstall(o.Port);
                case HostMode.Stop:
                    return StopRunning(o.Port);
                case HostMode.Help:
                    PrintHelp();
                    return 0;
                default:
                    return RunConsole(o);
            }
        }

        private static int RunConsole(Options o)
        {
            bool createdNew;
            Mutex runningMutex = SyncHelper.CreateMutex(IpcNames.RunningMutex(o.Port), out createdNew);
            if (!createdNew)
            {
                Console.Error.WriteLine("A server is already running on port " + o.Port + ".");
                return 2;
            }

            ServerController controller = new ServerController();
            try
            {
                controller.Start(o.Port, o.Root);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to start server: " + ex.Message);
                runningMutex.ReleaseMutex();
                return 3;
            }

            Console.WriteLine("Hearth Portable ASP.NET Web Server is running.");
            Console.WriteLine("  Port  : " + o.Port.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("  Root  : " + controller.Root);
            Console.WriteLine("  Bound : " + controller.BoundPrefix);
            Console.WriteLine("  URL   : http://localhost:" + o.Port.ToString(CultureInfo.InvariantCulture) + "/");
            Console.WriteLine("Press Ctrl+C (or send --stop) to shut down.");

            EventWaitHandle shutdown = SyncHelper.CreateEvent(
                IpcNames.ShutdownEvent(o.Port), EventResetMode.ManualReset);
            EventWaitHandle recycle = SyncHelper.CreateEvent(
                IpcNames.RecycleEvent(o.Port), EventResetMode.AutoReset);
            EventWaitHandle ready = SyncHelper.CreateEvent(
                IpcNames.ReadyEvent(o.Port), EventResetMode.ManualReset);
            ready.Set();

            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
            {
                e.Cancel = true;
                shutdown.Set();
            };

            // Wait for either an explicit shutdown or a recycle request raised by the
            // worker AppDomain when ASP.NET tears it down (e.g. a bin DLL was updated).
            // On recycle, rebuild the worker in place — the IIS app-pool-recycle
            // equivalent — so the site keeps serving without a manual stop/start.
            WaitHandle[] handles = { shutdown, recycle };
            bool keepRunning = true;
            while (keepRunning)
            {
                int which = WaitHandle.WaitAny(handles);
                if (which == 0)
                {
                    keepRunning = false;
                }
                else
                {
                    Console.WriteLine("Change detected - recycling worker AppDomain...");
                    ready.Reset();
                    try
                    {
                        controller.Stop();
                        // Debounce: file copies often raise several change events in a
                        // row; let them settle before bringing the new domain up.
                        Thread.Sleep(500);
                        controller.Start(o.Port, o.Root);
                        ready.Set();
                        Console.WriteLine("Recycled. Serving the updated application.");
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Recycle failed: " + ex.Message);
                        keepRunning = false;
                    }
                }
            }

            Console.WriteLine("Stopping...");
            controller.Stop();
            ready.Reset();
            runningMutex.ReleaseMutex();
            Console.WriteLine("Stopped.");
            return 0;
        }

        private static int StopRunning(int port)
        {
            if (SyncHelper.TrySignalEvent(IpcNames.ShutdownEvent(port)))
            {
                Console.WriteLine("Stop signal sent to server on port " + port + ".");
                return 0;
            }
            Console.Error.WriteLine("No running server found on port " + port + ".");
            return 1;
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Hearth Portable ASP.NET Web Server - host process");
            Console.WriteLine();
            Console.WriteLine("Usage: HearthPortableWebServer.Host.exe [options]");
            Console.WriteLine("  --port <n>     Port to listen on (default 8080)");
            Console.WriteLine("  --root <path>  Web application root (default <exe>\\wwwroot)");
            Console.WriteLine("  --service      Run as a Windows Service (config from server.config)");
            Console.WriteLine("  --install      Install the Windows Service (requires admin)");
            Console.WriteLine("  --uninstall    Remove the Windows Service (requires admin)");
            Console.WriteLine("  --stop         Signal a running server on the port to shut down");
            Console.WriteLine("  --help         Show this help");
        }
    }
}
