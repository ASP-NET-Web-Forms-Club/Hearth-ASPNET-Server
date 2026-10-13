using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Web;
using System.Web.Hosting;

namespace HearthPortableWebServer.Hosting
{
    /// <summary>
    /// The single "worker process" runtime. An instance lives inside the dedicated
    /// ASP.NET <see cref="AppDomain"/> created by <c>ApplicationHost.CreateApplicationHost</c>.
    /// It owns one <see cref="HttpListener"/> and dispatches every incoming request
    /// through the ASP.NET pipeline via <c>HttpRuntime.ProcessRequest</c>.
    ///
    /// As a <see cref="MarshalByRefObject"/> it is controlled from the default AppDomain
    /// by the host process (Start / Stop), exactly like w3wp.exe is controlled by WAS.
    /// </summary>
    public sealed class AspNetHost : MarshalByRefObject, System.Web.Hosting.IRegisteredObject
    {
        // Must match HearthPortableWebServer.Common.IpcNames.RecycleEvent. Duplicated
        // here on purpose: this assembly is loaded into the isolated worker AppDomain,
        // where only the Hosting DLL is deployed, so it must not depend on Common.
        private const string RecycleEventPrefix = @"Global\HearthPortableWebServer_Recycle_";

        // Number of concurrent accept operations kept outstanding. This is what lets a
        // single worker process service many simultaneous connections efficiently.
        private const int ConcurrentAccepts = 32;

        private WebServerConfig _webServerConfig;
        private HttpListener _listener;
        private string _physicalDir;
        private string _virtualDir;
        private volatile bool _running;
        private volatile bool _shuttingDown;
        private int _port;
        private long _maxRequestBytes;
        private string _boundPrefix;

        /// <summary>Keep the cross-AppDomain proxy alive for the lifetime of the process.</summary>
        public override object InitializeLifetimeService()
        {
            return null;
        }

        public string BoundPrefix
        {
            get { return _boundPrefix; }
        }

        public void Start(int port, string physicalDir, string virtualDir)
        {
            if (_running)
            {
                return;
            }

            _port = port;
            _physicalDir = physicalDir;
            _virtualDir = string.IsNullOrEmpty(virtualDir) ? "/" : virtualDir;
            _webServerConfig = WebServerConfig.Load(physicalDir);
            _maxRequestBytes = ReadMaxRequestBytes();

            _listener = CreateListener(port, out _boundPrefix);
            _listener.Start();
            _running = true;

            // Ask ASP.NET to call our IRegisteredObject.Stop(bool) when it begins
            // tearing this AppDomain down (a bin DLL or web.config changed). That is
            // our hook to trigger an automatic recycle instead of going dark.
            HostingEnvironment.RegisterObject(this);

            for (int i = 0; i < ConcurrentAccepts; i++)
            {
                BeginAccept();
            }
        }

        /// <summary>
        /// Called by ASP.NET as it shuts the worker AppDomain down. If this was not a
        /// deliberate host-initiated <see cref="Stop()"/>, the cause is a file change
        /// (e.g. an updated DLL in <c>bin</c>); signal the Host to recycle the worker
        /// in place so the site keeps serving the new build without a manual restart.
        /// </summary>
        public void Stop(bool immediate)
        {
            if (!_shuttingDown)
            {
                SignalRecycle();
            }

            try
            {
                HostingEnvironment.UnregisterObject(this);
            }
            catch (Exception)
            {
            }
        }

        private void SignalRecycle()
        {
            try
            {
                EventWaitHandle handle;
                string name = RecycleEventPrefix + _port.ToString(CultureInfo.InvariantCulture);
                if (EventWaitHandle.TryOpenExisting(name, out handle))
                {
                    using (handle)
                    {
                        handle.Set();
                    }
                }
            }
            catch (Exception)
            {
                // Best effort; if we cannot signal, behavior simply falls back to the
                // pre-existing "manual stop/start" case rather than failing hard.
            }
        }

        /// <summary>
        /// Try the broadest (IIS-like, all interfaces) binding first, then fall back to
        /// loopback-only prefixes that do not require administrative privileges.
        /// </summary>
        private static HttpListener CreateListener(int port, out string boundPrefix)
        {
            string portText = port.ToString(CultureInfo.InvariantCulture);
            string wildcard = "http://+:" + portText + "/";

            HttpListener wildcardListener = new HttpListener();
            wildcardListener.Prefixes.Add(wildcard);
            try
            {
                wildcardListener.Start();
                wildcardListener.Stop();
                boundPrefix = wildcard;
                return wildcardListener;
            }
            catch (HttpListenerException)
            {
                wildcardListener.Close();
            }

            HttpListener loopback = new HttpListener();
            loopback.Prefixes.Add("http://localhost:" + portText + "/");
            loopback.Prefixes.Add("http://127.0.0.1:" + portText + "/");
            boundPrefix = "http://localhost:" + portText + "/";
            return loopback;
        }

        private void BeginAccept()
        {
            if (!_running)
            {
                return;
            }

            try
            {
                _listener.BeginGetContext(OnGetContext, null);
            }
            catch (Exception)
            {
                // Listener is shutting down.
            }
        }

        private void OnGetContext(IAsyncResult ar)
        {
            HttpListenerContext context = null;
            try
            {
                context = _listener.EndGetContext(ar);
            }
            catch (Exception)
            {
                // Stopped or transient error; stop processing this callback.
                return;
            }

            // Immediately queue another accept so we keep the pipeline saturated.
            BeginAccept();

            if (context != null)
            {
                ProcessContext(context);
            }
        }

        private void ProcessContext(HttpListenerContext context)
        {
            try
            {
                if (TryDirectoryRedirect(context))
                {
                    return;
                }

                // Illegal path characters would make every later Path.* call throw; answer
                // 400 up front instead of a 500 that leaks the exception text.
                if (context.Request.Url.LocalPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                {
                    WriteStatus(context, 400, "Bad Request");
                    return;
                }

                string effectivePath = ResolveEffectivePath(context.Request.Url.LocalPath);
                ListenerHttpWorkerRequest worker =
                    new ListenerHttpWorkerRequest(context, _physicalDir, _virtualDir, effectivePath);

                // IIS returns 404 for a static file followed by PathInfo ("/logo.png/x");
                // ASP.NET's StaticFileHandler would otherwise serve the file for any suffix.
                if (!string.IsNullOrEmpty(worker.GetPathInfo()) &&
                    WebServerConfig.IsStaticFile(worker.GetFilePathTranslated()))
                {
                    WriteStatus(context, 404, "Not Found");
                    return;
                }

                // ASP.NET sizes the body from Content-Length; buffer a chunked body so
                // AJAX/web method JSON sent without Content-Length is not read as empty.
                if (worker.HasBodyWithoutContentLength && !worker.TryPreloadBody(_maxRequestBytes))
                {
                    WriteStatus(context, 413, "Request Entity Too Large");
                    return;
                }

                ApplyWebServerConfig(context, worker);
                HttpRuntime.ProcessRequest(worker);
            }
            catch (Exception ex)
            {
                TryWriteError(context, ex);
            }
        }

        private static void WriteStatus(HttpListenerContext context, int statusCode, string text)
        {
            try
            {
                context.Response.StatusCode = statusCode;
                byte[] body = System.Text.Encoding.UTF8.GetBytes(
                    statusCode.ToString(CultureInfo.InvariantCulture) + " " + text);
                context.Response.ContentType = "text/plain; charset=utf-8";
                context.Response.ContentLength64 = body.Length;
                context.Response.OutputStream.Write(body, 0, body.Length);
                context.Response.Close();
            }
            catch (Exception)
            {
                // Client may have disconnected.
            }
        }

        /// <summary>maxRequestLength (KB) from system.web/httpRuntime, as bytes.</summary>
        private static long ReadMaxRequestBytes()
        {
            const long DefaultKb = 4096;
            try
            {
                System.Web.Configuration.HttpRuntimeSection section =
                    System.Web.Configuration.WebConfigurationManager.GetSection("system.web/httpRuntime")
                        as System.Web.Configuration.HttpRuntimeSection;
                return (section != null ? section.MaxRequestLength : DefaultKb) * 1024L;
            }
            catch (Exception)
            {
                return DefaultKb * 1024L;
            }
        }

        /// <summary>
        /// IIS-style courtesy redirect: a request for "/foo" where foo is a physical
        /// directory (and not a file) gets a 301 to "/foo/", so default-document
        /// handling applies. Without this, ASP.NET receives a bare directory path,
        /// routing skips it (RouteExistingFiles = false) and StaticFileHandler
        /// throws "File does not exist." (404).
        /// </summary>
        private bool TryDirectoryRedirect(HttpListenerContext context)
        {
            string localPath = context.Request.Url.LocalPath;
            if (string.IsNullOrEmpty(localPath) || localPath.EndsWith("/", StringComparison.Ordinal))
            {
                return false;
            }

            string physical;
            try
            {
                string relative = localPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                physical = Path.Combine(_physicalDir, relative);
            }
            catch (ArgumentException)
            {
                // Illegal path characters: let ASP.NET produce its normal error.
                return false;
            }

            if (File.Exists(physical) || !Directory.Exists(physical))
            {
                return false;
            }

            // AbsolutePath keeps URL encoding (spaces etc.); Query includes the leading '?'.
            context.Response.StatusCode = 301;
            context.Response.RedirectLocation =
                context.Request.Url.AbsolutePath + "/" + context.Request.Url.Query;
            context.Response.ContentLength64 = 0;
            context.Response.Close();
            return true;
        }

        /// <summary>
        /// Default-document handling: a request for a directory ("/" or "/foo/") is
        /// remapped to the first matching default document that exists on disk.
        /// </summary>
        private string ResolveEffectivePath(string localPath)
        {
            if (string.IsNullOrEmpty(localPath) || localPath[localPath.Length - 1] != '/')
            {
                return localPath;
            }

            string relative = localPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            string physicalDir = string.IsNullOrEmpty(relative)
                ? _physicalDir
                : Path.Combine(_physicalDir, relative);

            if (!_webServerConfig.DefaultDocumentEnabled)
            {
                return localPath;
            }

            foreach (string document in _webServerConfig.DefaultDocuments)
            {
                string candidate = Path.Combine(physicalDir, document.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(candidate))
                {
                    return localPath + document.Replace('\\', '/');
                }
            }

            return localPath;
        }

        /// <summary>
        /// Applies the supported &lt;system.webServer&gt; settings (custom headers and, for
        /// static files, mimeMap / clientCache) to this request's response.
        /// </summary>
        private void ApplyWebServerConfig(HttpListenerContext context, ListenerHttpWorkerRequest worker)
        {
            foreach (System.Collections.Generic.KeyValuePair<string, string> header in _webServerConfig.CustomHeaders)
            {
                try
                {
                    context.Response.Headers[header.Key] = header.Value;
                }
                catch (ArgumentException)
                {
                    // Restricted header; HttpListener manages it itself.
                }
            }

            string physicalPath = worker.GetFilePathTranslated();
            if (!WebServerConfig.IsStaticFile(physicalPath))
            {
                return;
            }

            string mime;
            if (_webServerConfig.MimeMaps.TryGetValue(Path.GetExtension(physicalPath), out mime))
            {
                worker.ContentTypeOverride = mime;
            }
            worker.CacheControlOverride = _webServerConfig.CacheControl;
            worker.ExpiresOverride = _webServerConfig.Expires;
        }

        private static void TryWriteError(HttpListenerContext context, Exception ex)
        {
            // Log the details server-side; never echo exception text to the client.
            System.Diagnostics.Trace.TraceError("HearthPortableWebServer request failed: " + ex);
            WriteStatus(context, 500, "Internal Server Error");
        }

        public void Stop()
        {
            if (!_running)
            {
                return;
            }

            // Mark this as a deliberate, host-initiated stop so the IRegisteredObject
            // shutdown callback does NOT mistake it for a file-change recycle.
            _shuttingDown = true;
            _running = false;

            try
            {
                HostingEnvironment.UnregisterObject(this);
            }
            catch (Exception)
            {
            }

            try
            {
                if (_listener != null)
                {
                    _listener.Stop();
                    _listener.Close();
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                _listener = null;
            }

            try
            {
                // Give in-flight requests a brief moment to drain.
                Thread.Sleep(250);
                HttpRuntime.Close();
            }
            catch (Exception)
            {
            }
        }
    }
}