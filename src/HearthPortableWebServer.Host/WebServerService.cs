using System.ServiceProcess;
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

        public WebServerService(int port, string root)
        {
            ServiceName = IpcNames.ServiceName;
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
        }

        protected override void OnStop()
        {
            StopController();
        }

        protected override void OnShutdown()
        {
            StopController();
        }

        private void StopController()
        {
            if (_controller != null)
            {
                _controller.Stop();
                _controller = null;
            }
        }
    }
}
