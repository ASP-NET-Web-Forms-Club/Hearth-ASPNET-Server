using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using HearthPortableWebServer.Manager.Models;

namespace HearthPortableWebServer.Manager.Services
{
    public static class ManagerIpc
    {
        public static string PipeName()
        {
            return "HearthManager_" + ManagerConfig.GetInstanceId();
        }

        public sealed class IpcRequest
        {
            public string Command { get; set; }
            public int Port { get; set; }
        }

        public sealed class SiteStatusDto
        {
            public int Port { get; set; }
            public string State { get; set; } // "Running", "PortInUse", "Stopped"
            public bool RuntimeMissing { get; set; }
        }

        public sealed class IpcResponse
        {
            public bool Ok { get; set; }
            public string Error { get; set; }
            public List<string> Errors { get; set; }
            public string AppDirectory { get; set; }
            public string ServiceName { get; set; }
            public int ServicePid { get; set; }
            public List<SiteStatusDto> Sites { get; set; }
        }

        internal static bool PathsEqual(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            try
            {
                string na = Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string nb = Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return string.Equals(na, nb, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    /// <summary>
    /// Named Pipe IPC server hosted inside ManagerService (Session 0).
    /// Allows the non-elevated Manager GUI (Session 1) in the same folder to query
    /// authoritative site status and execute Start/Stop/Reload inside the service.
    /// </summary>
    public sealed class ManagerIpcServer : IDisposable
    {
        private readonly Func<ManagerIpc.IpcRequest, ManagerIpc.IpcResponse> _handler;
        private readonly string _pipeName;
        private readonly Thread _acceptThread;
        private volatile bool _stopping;
        private NamedPipeServerStream _currentListeningPipe;
        private readonly object _pipeLock = new object();

        public ManagerIpcServer(Func<ManagerIpc.IpcRequest, ManagerIpc.IpcResponse> handler)
        {
            _handler = handler ?? throw new ArgumentNullException("handler");
            _pipeName = ManagerIpc.PipeName();
            _acceptThread = new Thread(AcceptLoop)
            {
                IsBackground = true,
                Name = "HearthManagerIpcServer"
            };
        }

        public void Start()
        {
            _stopping = false;
            _acceptThread.Start();
        }

        public void Stop()
        {
            _stopping = true;
            lock (_pipeLock)
            {
                if (_currentListeningPipe != null)
                {
                    try { _currentListeningPipe.Dispose(); } catch { }
                    _currentListeningPipe = null;
                }
            }
            try { _acceptThread.Join(1500); } catch { }
        }

        public void Dispose()
        {
            Stop();
        }

        private static PipeSecurity CreatePipeSecurity()
        {
            PipeSecurity security = new PipeSecurity();
            security.AddAccessRule(new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                PipeAccessRights.FullControl,
                AccessControlType.Allow));
            security.AddAccessRule(new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
                PipeAccessRights.FullControl,
                AccessControlType.Allow));
            security.AddAccessRule(new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
                PipeAccessRights.ReadWrite,
                AccessControlType.Allow));
            return security;
        }

        private void AcceptLoop()
        {
            while (!_stopping)
            {
                NamedPipeServerStream server = null;
                try
                {
                    server = new NamedPipeServerStream(
                        _pipeName,
                        PipeDirection.InOut,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous,
                        4096,
                        4096,
                        CreatePipeSecurity());

                    lock (_pipeLock)
                    {
                        if (_stopping)
                        {
                            server.Dispose();
                            return;
                        }
                        _currentListeningPipe = server;
                    }

                    IAsyncResult ar = server.BeginWaitForConnection(null, null);
                    ar.AsyncWaitHandle.WaitOne();
                    if (_stopping)
                    {
                        try { server.Dispose(); } catch { }
                        return;
                    }

                    server.EndWaitForConnection(ar);

                    lock (_pipeLock)
                    {
                        if (_currentListeningPipe == server)
                        {
                            _currentListeningPipe = null;
                        }
                    }

                    NamedPipeServerStream clientPipe = server;
                    server = null;
                    ThreadPool.QueueUserWorkItem(delegate { HandleClient(clientPipe); });
                }
                catch
                {
                    if (server != null)
                    {
                        try { server.Dispose(); } catch { }
                    }
                    if (!_stopping)
                    {
                        Thread.Sleep(200);
                    }
                }
            }
        }

        private void HandleClient(NamedPipeServerStream pipe)
        {
            using (pipe)
            {
                try
                {
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    UTF8Encoding utf8NoBom = new UTF8Encoding(false);
                    using (StreamReader reader = new StreamReader(pipe, utf8NoBom, false, 4096, true))
                    using (StreamWriter writer = new StreamWriter(pipe, utf8NoBom, 4096, true))
                    {
                        writer.AutoFlush = true;
                        string line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line)) return;

                        ManagerIpc.IpcRequest req = serializer.Deserialize<ManagerIpc.IpcRequest>(line);
                        ManagerIpc.IpcResponse resp;
                        try
                        {
                            resp = _handler(req ?? new ManagerIpc.IpcRequest());
                        }
                        catch (Exception ex)
                        {
                            resp = new ManagerIpc.IpcResponse
                            {
                                Ok = false,
                                Error = ex.Message
                            };
                        }

                        if (resp == null)
                        {
                            resp = new ManagerIpc.IpcResponse { Ok = false, Error = "Empty response." };
                        }

                        resp.AppDirectory = ManagerConfig.GetAppDirectory();
                        resp.ServicePid = Process.GetCurrentProcess().Id;
                        resp.ServiceName = ServiceControllerHelper.ServiceName;

                        string json = serializer.Serialize(resp);
                        writer.WriteLine(json);
                        pipe.WaitForPipeDrain();
                    }
                }
                catch
                {
                    // Client disconnected or malformed request
                }
            }
        }
    }

    /// <summary>
    /// Named Pipe IPC client used by the Manager GUI and CLI to talk to this folder's
    /// running ManagerService instance.
    /// </summary>
    public static class ManagerIpcClient
    {
        private static ManagerIpc.IpcResponse Send(ManagerIpc.IpcRequest req, int connectTimeoutMs)
        {
            string pipeName = ManagerIpc.PipeName();
            try
            {
                using (NamedPipeClientStream client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None))
                {
                    client.Connect(connectTimeoutMs);

                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    UTF8Encoding utf8NoBom = new UTF8Encoding(false);
                    using (StreamWriter writer = new StreamWriter(client, utf8NoBom, 4096, true))
                    using (StreamReader reader = new StreamReader(client, utf8NoBom, false, 4096, true))
                    {
                        writer.AutoFlush = true;
                        writer.WriteLine(serializer.Serialize(req));
                        string responseLine = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(responseLine)) return null;

                        ManagerIpc.IpcResponse resp = serializer.Deserialize<ManagerIpc.IpcResponse>(responseLine);
                        if (resp == null) return null;

                        // Extra safety: confirm the responding service belongs to this exact folder.
                        if (!ManagerIpc.PathsEqual(resp.AppDirectory, ManagerConfig.GetAppDirectory()))
                        {
                            return null;
                        }
                        return resp;
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public static ManagerIpc.IpcResponse TryGetStatus()
        {
            return Send(new ManagerIpc.IpcRequest { Command = "status" }, 150);
        }

        public static bool TryStartSite(int port, out bool succeeded, out string error)
        {
            succeeded = false;
            error = null;
            ManagerIpc.IpcResponse resp = Send(new ManagerIpc.IpcRequest { Command = "start", Port = port }, 500);
            if (resp == null) return false;
            succeeded = resp.Ok;
            error = resp.Error;
            return true;
        }

        public static bool TryStopSite(int port, out bool succeeded, out string error)
        {
            succeeded = false;
            error = null;
            ManagerIpc.IpcResponse resp = Send(new ManagerIpc.IpcRequest { Command = "stop", Port = port }, 500);
            if (resp == null) return false;
            succeeded = resp.Ok;
            error = resp.Error;
            return true;
        }

        public static bool TryStopSiteForReconfig(int port)
        {
            ManagerIpc.IpcResponse resp = Send(new ManagerIpc.IpcRequest { Command = "stop_reconfig", Port = port }, 500);
            return resp != null && resp.Ok;
        }

        public static bool TryStartAll(out List<string> errors)
        {
            errors = new List<string>();
            ManagerIpc.IpcResponse resp = Send(new ManagerIpc.IpcRequest { Command = "start_all" }, 500);
            if (resp == null) return false;
            if (resp.Errors != null) errors.AddRange(resp.Errors);
            else if (!resp.Ok && !string.IsNullOrEmpty(resp.Error)) errors.Add(resp.Error);
            return true;
        }

        public static bool TryStopAll()
        {
            ManagerIpc.IpcResponse resp = Send(new ManagerIpc.IpcRequest { Command = "stop_all" }, 500);
            return resp != null && resp.Ok;
        }

        public static bool TryReload()
        {
            ManagerIpc.IpcResponse resp = Send(new ManagerIpc.IpcRequest { Command = "reload" }, 300);
            return resp != null && resp.Ok;
        }

        public static bool TryStopOrphans()
        {
            ManagerIpc.IpcResponse resp = Send(new ManagerIpc.IpcRequest { Command = "stop_orphans" }, 300);
            return resp != null && resp.Ok;
        }
    }
}
