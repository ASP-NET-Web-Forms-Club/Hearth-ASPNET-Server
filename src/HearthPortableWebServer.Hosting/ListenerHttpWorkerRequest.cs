using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Web;

namespace HearthPortableWebServer.Hosting
{
    /// <summary>
    /// Bridges a single <see cref="HttpListenerContext"/> to the ASP.NET pipeline by
    /// implementing <see cref="HttpWorkerRequest"/>. One instance is created per request
    /// and handed to <c>HttpRuntime.ProcessRequest</c>.
    /// </summary>
    internal sealed class ListenerHttpWorkerRequest : HttpWorkerRequest
    {
        private readonly HttpListenerContext _context;
        private readonly string _physicalDir;
        private readonly string _virtualDir;
        private readonly string _uriPath;     // effective path (may include default document)
        private readonly string _filePath;    // virtual path to physical target file (e.g. /Page.aspx)
        private readonly string _pathInfo;    // sub-path info for WebMethods / routing (e.g. /GetCustomerData)
        private readonly string _queryString;
        private byte[] _preloadedBody;        // buffered chunked body (null = stream from HttpListener)

        // Set by AspNetHost from <system.webServer> for static files; they replace the
        // values ASP.NET's StaticFileHandler would send (null = keep ASP.NET's value).
        private string _contentTypeOverride;
        private string _cacheControlOverride;
        private string _expiresOverride;

        public string ContentTypeOverride
        {
            get { return _contentTypeOverride; }
            set { _contentTypeOverride = value; if (value != null) _context.Response.ContentType = value; }
        }

        public string CacheControlOverride
        {
            get { return _cacheControlOverride; }
            set { _cacheControlOverride = value; if (value != null) TrySetHeader("Cache-Control", value); }
        }

        public string ExpiresOverride
        {
            get { return _expiresOverride; }
            set { _expiresOverride = value; if (value != null) TrySetHeader("Expires", value); }
        }

        private void TrySetHeader(string name, string value)
        {
            try
            {
                _context.Response.Headers[name] = value;
            }
            catch (ArgumentException)
            {
            }
        }

        public ListenerHttpWorkerRequest(HttpListenerContext context, string physicalDir,
            string virtualDir, string effectiveUriPath)
        {
            if (context == null) throw new ArgumentNullException("context");
            _context = context;
            _physicalDir = physicalDir;
            _virtualDir = string.IsNullOrEmpty(virtualDir) ? "/" : virtualDir;
            _uriPath = string.IsNullOrEmpty(effectiveUriPath)
                ? context.Request.Url.LocalPath
                : effectiveUriPath;

            string query = _context.Request.Url.Query;
            if (!string.IsNullOrEmpty(query) && query[0] == '?')
            {
                query = query.Substring(1);
            }
            _queryString = query;

            ParsePathInfo(out _filePath, out _pathInfo);
        }

        // ---- Request line ------------------------------------------------------

        public override string GetUriPath()
        {
            return _uriPath;
        }

        public override string GetQueryString()
        {
            return _queryString;
        }

        public override string GetRawUrl()
        {
            if (string.IsNullOrEmpty(_queryString))
            {
                return _uriPath;
            }
            return _uriPath + "?" + _queryString;
        }

        public override string GetHttpVerbName()
        {
            return _context.Request.HttpMethod;
        }

        public override string GetHttpVersion()
        {
            // IMPORTANT: report HTTP/1.0 to the ASP.NET runtime.
            //
            // Under HTTP/1.1 with an unknown content length (buffering off or an early
            // Response.Flush()), ASP.NET performs *managed* chunked transfer-encoding -
            // it writes the hex chunk-size framing directly into the bytes handed to
            // SendResponseFromMemory. HttpListener then applies its OWN transfer-encoding
            // on top, so the client decodes the transport layer and is left with ASP.NET's
            // inner "dfb...0" framing showing up as page text.
            //
            // HttpListener cannot forward a pre-chunked body verbatim, so the correct fix
            // is to stop ASP.NET from chunking at all. Reporting HTTP/1.0 does exactly that
            // (chunked encoding is an HTTP/1.1 feature). HttpListener still negotiates the
            // real transport encoding and returns a proper HTTP/1.1 response to the client.
            return "HTTP/1.0";
        }

        public override string GetProtocol()
        {
            return _context.Request.IsSecureConnection ? "HTTPS" : "HTTP";
        }

        // ---- Endpoints ---------------------------------------------------------

        public override string GetRemoteAddress()
        {
            return _context.Request.RemoteEndPoint.Address.ToString();
        }

        public override int GetRemotePort()
        {
            return _context.Request.RemoteEndPoint.Port;
        }

        public override string GetLocalAddress()
        {
            return _context.Request.LocalEndPoint.Address.ToString();
        }

        public override int GetLocalPort()
        {
            return _context.Request.LocalEndPoint.Port;
        }

        // ---- Paths -------------------------------------------------------------

        public override string GetFilePath()
        {
            return _filePath;
        }

        public override string GetFilePathTranslated()
        {
            return MapPath(_filePath);
        }

        public override string GetPathInfo()
        {
            return _pathInfo;
        }

        /// <summary>
        /// Decomposes <see cref="_uriPath"/> into a physical virtual file path and additional
        /// sub-path info. For example, a request to "/Page.aspx/GetCustomerData" (ASP.NET AJAX
        /// WebMethod) resolves FilePath to "/Page.aspx" and PathInfo to "/GetCustomerData".
        /// Without this, ASP.NET treats the entire path as a nonexistent file and returns 404.
        ///
        /// Like IIS, segments are scanned left to right and the split happens after the FIRST
        /// segment that is either a known ASP.NET handler extension or an existing file. This
        /// keeps dots inside the PathInfo intact (e.g. "/Handler.ashx/files/report.pdf") and
        /// skips dotted directory names (e.g. "/v1.2/Page.aspx/Save").
        /// </summary>
        private void ParsePathInfo(out string filePath, out string pathInfo)
        {
            filePath = _uriPath;
            pathInfo = string.Empty;

            if (string.IsNullOrEmpty(_uriPath))
            {
                return;
            }

            int segmentStart = 0;
            while (true)
            {
                int slash = _uriPath.IndexOf('/', segmentStart + 1);
                if (slash < 0)
                {
                    // The last segment is the target itself; there is no PathInfo.
                    return;
                }

                // Only segments containing a dot can be a file or handler.
                if (_uriPath.IndexOf('.', segmentStart, slash - segmentStart) >= 0)
                {
                    string candidate = _uriPath.Substring(0, slash);
                    if (IsKnownAspNetExtension(SafeGetExtension(candidate)) || IsExistingFile(candidate))
                    {
                        filePath = candidate;
                        pathInfo = _uriPath.Substring(slash);
                        return;
                    }
                }

                segmentStart = slash;
            }
        }

        private static string SafeGetExtension(string path)
        {
            try
            {
                return Path.GetExtension(path);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private bool IsExistingFile(string virtualPath)
        {
            try
            {
                return File.Exists(MapPath(virtualPath));
            }
            catch (ArgumentException)
            {
                // Illegal path characters: cannot be a file on disk.
                return false;
            }
        }

        private static bool IsKnownAspNetExtension(string ext)
        {
            if (string.IsNullOrEmpty(ext)) return false;
            return ext.Equals(".aspx", StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".asmx", StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".ashx", StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".axd", StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".svc", StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".rem", StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".soap", StringComparison.OrdinalIgnoreCase);
        }

        public override string GetAppPath()
        {
            return _virtualDir;
        }

        public override string GetAppPathTranslated()
        {
            return _physicalDir;
        }

        public override string MapPath(string virtualPath)
        {
            if (string.IsNullOrEmpty(virtualPath) || virtualPath == _virtualDir || virtualPath == "/")
            {
                return _physicalDir;
            }

            string relative = virtualPath;
            if (relative.StartsWith(_virtualDir, StringComparison.OrdinalIgnoreCase))
            {
                relative = relative.Substring(_virtualDir.Length);
            }

            relative = relative.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(_physicalDir, relative);
        }

        // ---- Request headers ---------------------------------------------------

        public override string GetKnownRequestHeader(int index)
        {
            // A buffered chunked body is presented to ASP.NET as a plain body of known length.
            if (_preloadedBody != null)
            {
                if (index == HeaderContentLength)
                {
                    return _preloadedBody.Length.ToString(CultureInfo.InvariantCulture);
                }
                if (index == HeaderTransferEncoding)
                {
                    return null;
                }
            }

            string name = GetKnownRequestHeaderName(index);
            if (name == null)
            {
                return null;
            }
            return _context.Request.Headers[name];
        }

        public override string GetUnknownRequestHeader(string name)
        {
            return _context.Request.Headers[name];
        }

        public override string[][] GetUnknownRequestHeaders()
        {
            System.Collections.Specialized.NameValueCollection headers = _context.Request.Headers;
            int count = headers.Count;
            string[][] result = new string[count][];
            for (int i = 0; i < count; i++)
            {
                result[i] = new string[2];
                result[i][0] = headers.GetKey(i);
                result[i][1] = headers.Get(i);
            }
            return result;
        }

        // ---- Request body ------------------------------------------------------

        /// <summary>
        /// True when the request carries a body without a Content-Length header (chunked
        /// transfer encoding). ASP.NET sizes the body from Content-Length, so such a body
        /// must be buffered with <see cref="TryPreloadBody"/> or ASP.NET reads it as empty.
        /// </summary>
        public bool HasBodyWithoutContentLength
        {
            get { return _context.Request.HasEntityBody && _context.Request.ContentLength64 < 0; }
        }

        /// <summary>
        /// Reads the whole request body into memory so it can be handed to ASP.NET as a
        /// preloaded body with a known length. Returns false if the body exceeds
        /// <paramref name="maxBytes"/> (the caller should answer 413).
        /// </summary>
        public bool TryPreloadBody(long maxBytes)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                byte[] buffer = new byte[64 * 1024];
                int read;
                while ((read = _context.Request.InputStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (ms.Length + read > maxBytes)
                    {
                        return false;
                    }
                    ms.Write(buffer, 0, read);
                }
                _preloadedBody = ms.ToArray();
            }
            return true;
        }

        public override byte[] GetPreloadedEntityBody()
        {
            return _preloadedBody;
        }

        public override int GetPreloadedEntityBodyLength()
        {
            return _preloadedBody == null ? 0 : _preloadedBody.Length;
        }

        public override int GetTotalEntityBodyLength()
        {
            return _preloadedBody == null ? base.GetTotalEntityBodyLength() : _preloadedBody.Length;
        }

        public override int ReadEntityBody(byte[] buffer, int size)
        {
            if (buffer == null || size <= 0 || _preloadedBody != null)
            {
                return 0;
            }
            return _context.Request.InputStream.Read(buffer, 0, size);
        }

        public override int ReadEntityBody(byte[] buffer, int offset, int size)
        {
            if (buffer == null || size <= 0 || _preloadedBody != null)
            {
                return 0;
            }
            return _context.Request.InputStream.Read(buffer, offset, size);
        }

        public override bool IsEntireEntityBodyIsPreloaded()
        {
            return _preloadedBody != null;
        }

        // ---- Server variables --------------------------------------------------

        public override string GetServerName()
        {
            string host = _context.Request.UserHostName;
            if (!string.IsNullOrEmpty(host))
            {
                int colon = host.IndexOf(':');
                return colon >= 0 ? host.Substring(0, colon) : host;
            }
            return _context.Request.LocalEndPoint.Address.ToString();
        }

        public override string GetServerVariable(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            switch (name)
            {
                case "HTTPS":
                    return _context.Request.IsSecureConnection ? "on" : "off";
                case "SERVER_PROTOCOL":
                    return GetHttpVersion();
                case "SERVER_SOFTWARE":
                    return "HearthPortableWebServer/1.0";
                case "REMOTE_ADDR":
                    return GetRemoteAddress();
                case "REMOTE_PORT":
                    return GetRemotePort().ToString(CultureInfo.InvariantCulture);
                case "LOCAL_ADDR":
                    return GetLocalAddress();
                case "SERVER_NAME":
                    return GetServerName();
                case "SERVER_PORT":
                    return GetLocalPort().ToString(CultureInfo.InvariantCulture);
                case "REQUEST_METHOD":
                    return GetHttpVerbName();
                case "QUERY_STRING":
                    return _queryString;
                case "URL":
                case "SCRIPT_NAME":
                    return _filePath;
                case "PATH_INFO":
                    return _pathInfo;
                case "PATH_TRANSLATED":
                    return GetFilePathTranslated();
                default:
                    // Only HTTP_* variables come from request headers (HTTP_USER_AGENT ->
                    // "User-Agent"). Any other unknown variable must NOT fall back to a header:
                    // a client could otherwise spoof server-owned values such as LOGON_USER,
                    // AUTH_TYPE or REMOTE_USER simply by sending a header with that name.
                    if (name.StartsWith("HTTP_", StringComparison.OrdinalIgnoreCase) && name.Length > 5)
                    {
                        string header = _context.Request.Headers[name.Substring(5).Replace('_', '-')];
                        return header ?? string.Empty;
                    }
                    return string.Empty;
            }
        }

        // ---- Response ----------------------------------------------------------

        public override void SendStatus(int statusCode, string statusDescription)
        {
            _context.Response.StatusCode = statusCode;
            if (!string.IsNullOrEmpty(statusDescription))
            {
                _context.Response.StatusDescription = statusDescription;
            }
        }

        public override void SendKnownResponseHeader(int index, string value)
        {
            string name = GetKnownResponseHeaderName(index);
            SendUnknownResponseHeader(name, value);
        }

        public override void SendUnknownResponseHeader(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            // A number of headers are "restricted" on HttpListenerResponse and must be
            // assigned through dedicated properties rather than the Headers collection.
            string lower = name.ToLowerInvariant();
            switch (lower)
            {
                case "content-length":
                    long length;
                    if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out length))
                    {
                        _context.Response.ContentLength64 = length;
                    }
                    return;
                case "content-type":
                    _context.Response.ContentType = _contentTypeOverride ?? value;
                    return;
                case "cache-control":
                    TrySetHeader(name, _cacheControlOverride ?? value);
                    return;
                case "expires":
                    TrySetHeader(name, _expiresOverride ?? value);
                    return;
                case "transfer-encoding":
                    // Never honor an upstream chunked header: HttpListener handles transfer
                    // encoding itself. Setting SendChunked here would double-encode the body
                    // (the "dfb" hex-prefix bug). See GetHttpVersion for the full rationale.
                    return;
                case "keep-alive":
                case "connection":
                    // Managed by HttpListener itself; ignore.
                    return;
                default:
                    try
                    {
                        _context.Response.Headers[name] = value;
                    }
                    catch (ArgumentException)
                    {
                        // Header rejected as restricted; safe to ignore for self-hosting.
                    }
                    return;
            }
        }

        public override void SendCalculatedContentLength(int contentLength)
        {
            _context.Response.ContentLength64 = contentLength;
        }

        public override void SendCalculatedContentLength(long contentLength)
        {
            _context.Response.ContentLength64 = contentLength;
        }

        public override bool HeadersSent()
        {
            return false;
        }

        public override void SendResponseFromMemory(byte[] data, int length)
        {
            if (data == null || length <= 0)
            {
                return;
            }
            _context.Response.OutputStream.Write(data, 0, length);
        }

        public override void SendResponseFromFile(string filename, long offset, long length)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                SendStreamPortion(fs, offset, length);
            }
        }

        public override void SendResponseFromFile(IntPtr handle, long offset, long length)
        {
            using (Microsoft.Win32.SafeHandles.SafeFileHandle safeHandle =
                       new Microsoft.Win32.SafeHandles.SafeFileHandle(handle, false))
            using (FileStream fs = new FileStream(safeHandle, FileAccess.Read))
            {
                SendStreamPortion(fs, offset, length);
            }
        }

        private void SendStreamPortion(Stream stream, long offset, long length)
        {
            if (offset > 0)
            {
                stream.Seek(offset, SeekOrigin.Begin);
            }

            byte[] buffer = new byte[64 * 1024];
            long remaining = length;
            while (remaining > 0)
            {
                int toRead = remaining < buffer.Length ? (int)remaining : buffer.Length;
                int read = stream.Read(buffer, 0, toRead);
                if (read <= 0)
                {
                    break;
                }
                _context.Response.OutputStream.Write(buffer, 0, read);
                remaining -= read;
            }
        }

        public override void FlushResponse(bool finalFlush)
        {
            try
            {
                _context.Response.OutputStream.Flush();
            }
            catch (Exception)
            {
                // Client may have disconnected; nothing useful to do here.
            }
        }

        public override void EndOfRequest()
        {
            try
            {
                _context.Response.OutputStream.Close();
            }
            catch (Exception)
            {
            }

            try
            {
                _context.Response.Close();
            }
            catch (Exception)
            {
            }
        }

        public override void CloseConnection()
        {
            // HttpListener owns the underlying connection lifetime.
        }
    }
}
