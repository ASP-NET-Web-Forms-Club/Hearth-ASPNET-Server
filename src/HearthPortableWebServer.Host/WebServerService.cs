using System;
using System.ServiceProcess;
using System.Threading;
using HearthPortableWebServer.Common;

namespace HearthPortableWebServer.Host
{
    /// <summary>
    /// Windows Service wrapper so the portable web server can auto-start with the
    /// machine, completely independent of any interactive UI.
    /// </summary>
    public sealed class WebServerService : ServiceBase
    {
        private readonly int _port;
        private readonly string _root;
        private ServerController _controller;

        private EventWaitHandle _recycle;
        private EventWaitHandle _stopWatcher;
        private Thread _watcherThread;

        public WebServerService(int port, string root)
        {
            ServiceName = IpcNames.ServiceName(port);
            CanStop = true;
            CanShutdown = true;
            AutoLog = true;
            _port = port;
            _root = root;
        }

        protected override void OnStart(string[] args)
        {
            _controller = new ServerController();
            _controller.Start(_port, _root);

            // Watch for worker-AppDomain recycle requests (a bin DLL or web.config
            // changed) and rebuild the worker in place, the IIS app-pool-recycle
            // equivalent. Without this the service would go dark on a DLL update.
            _recycle = SyncHelper.CreateEvent(IpcNames.RecycleEvent(_port), EventResetMode.AutoReset);
            _stopWatcher = new EventWaitHandle(false, EventResetMode.ManualReset);
            _watcherThread = new Thread(WatchForRecycle) { IsBackground = true, Name = "RecycleWatcher" };
            _watcherThread.Start();
        }

        protected override void OnStop()
        {
            StopController();
        }

        protected override void OnShutdown()
        {
            StopController();
        }

        private void WatchForRecycle()
        {
            WaitHandle[] handles = { _stopWatcher, _recycle };
            while (true)
            {
                int which = WaitHandle.WaitAny(handles);
                if (which == 0)
                {
                    return;
                }

                try
                {
                    if (_controller != null)
                    {
                        _controller.Stop();
                        Thread.Sleep(500);   // debounce burst of file-change events
                        _controller.Start(_port, _root);
                    }
                }
                catch (Exception)
                {
                    // Leave the worker down; the SCM recovery options can restart the
                    // service. Swallow so the watcher thread does not crash the process.
                }
            }
        }

        private void StopController()
        {
            if (_stopWatcher != null)
            {
                _stopWatcher.Set();
            }
            if (_watcherThread != null)
            {
                _watcherThread.Join(2000);
                _watcherThread = null;
            }

            if (_controller != null)
            {
                _controller.Stop();
                _controller = null;
            }
        }
    }
}
