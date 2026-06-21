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
        private readonly string _queryString;

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
            return _uriPath;
        }

        public override string GetFilePathTranslated()
        {
            return MapPath(_uriPath);
        }

        public override string GetPathInfo()
        {
            return string.Empty;
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

        public override int ReadEntityBody(byte[] buffer, int size)
        {
            if (buffer == null || size <= 0)
            {
                return 0;
            }
            return _context.Request.InputStream.Read(buffer, 0, size);
        }

        public override int ReadEntityBody(byte[] buffer, int offset, int size)
        {
            if (buffer == null || size <= 0)
            {
                return 0;
            }
            return _context.Request.InputStream.Read(buffer, offset, size);
        }

        public override bool IsEntireEntityBodyIsPreloaded()
        {
            return false;
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
                default:
                    string header = _context.Request.Headers[name];
                    return header ?? string.Empty;
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
                    _context.Response.ContentType = value;
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
