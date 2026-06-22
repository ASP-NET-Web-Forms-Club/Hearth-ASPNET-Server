using System.Globalization;

namespace HearthPortableWebServer.Common
{
    /// <summary>
    /// Centralized names for the cross-process synchronization primitives that the
    /// Launcher (WinForms) and the Host (console / Windows Service) use to talk to
    /// each other. Everything is keyed by port so multiple servers can coexist.
    /// </summary>
    public static class IpcNames
    {
        // Global\ prefix => visible across sessions and across UAC integrity levels.
        private const string RunningMutexPrefix = @"Global\HearthPortableWebServer_Running_";
        private const string ShutdownEventPrefix = @"Global\HearthPortableWebServer_Shutdown_";
        private const string ReadyEventPrefix = @"Global\HearthPortableWebServer_Ready_";

        /// <summary>Held by the Host while the server is alive. Presence == running.</summary>
        public static string RunningMutex(int port)
        {
            return RunningMutexPrefix + port.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Set by the Launcher to ask the Host to shut down gracefully.</summary>
        public static string ShutdownEvent(int port)
        {
            return ShutdownEventPrefix + port.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Set by the Host once the listener is accepting requests.</summary>
        public static string ReadyEvent(int port)
        {
            return ReadyEventPrefix + port.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Name of the Windows Service for a given port. Port-based so multiple copies of
        /// the server (in different folders, on different ports) can each install their
        /// own service without name collisions.
        /// </summary>
        public static string ServiceName(int port)
        {
            return "HearthPortableWebServer_Port" + port.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Human-readable service name shown in services.msc, per port.</summary>
        public static string ServiceDisplayName(int port)
        {
            return "Hearth Portable ASP.NET Web Server (port "
                + port.ToString(CultureInfo.InvariantCulture) + ")";
        }

        public const string ServiceDescription =
            "Self-hosted, IIS-equivalent worker process for ASP.NET Web Forms applications.";
    }
}
