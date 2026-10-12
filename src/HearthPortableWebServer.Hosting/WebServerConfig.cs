using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace HearthPortableWebServer.Hosting
{
    /// <summary>
    /// Minimal reader for the IIS-only <c>&lt;system.webServer&gt;</c> section of the site's
    /// root web.config. The ASP.NET runtime ignores this section, so Hearth interprets the
    /// subset it supports itself:
    /// <list type="bullet">
    /// <item><c>httpProtocol/customHeaders</c> (add / remove / clear)</item>
    /// <item><c>staticContent/mimeMap</c> and <c>staticContent/clientCache</c></item>
    /// <item><c>defaultDocument</c> (enabled, files add / remove / clear)</item>
    /// </list>
    /// Loaded once per worker AppDomain; a web.config change recycles the AppDomain,
    /// so edits are picked up automatically.
    /// </summary>
    internal sealed class WebServerConfig
    {
        private static readonly string[] BuiltInDefaultDocuments =
        {
            "Default.aspx", "default.aspx", "Index.aspx", "index.aspx",
            "index.html", "index.htm", "Default.htm"
        };

        // Extensions ASP.NET handles dynamically; staticContent settings never apply to them.
        private static readonly HashSet<string> DynamicExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".aspx", ".ashx", ".asmx", ".axd", ".svc", ".asax", ".ascx", ".master",
            ".cshtml", ".vbhtml", ".config", ".soap", ".rem", ".xamlx"
        };

        public List<KeyValuePair<string, string>> CustomHeaders { get; private set; }
        public Dictionary<string, string> MimeMaps { get; private set; }
        public string CacheControl { get; private set; }   // null = leave ASP.NET's value
        public string Expires { get; private set; }        // null = leave ASP.NET's value
        public bool DefaultDocumentEnabled { get; private set; }
        public List<string> DefaultDocuments { get; private set; }

        private WebServerConfig()
        {
            CustomHeaders = new List<KeyValuePair<string, string>>();
            MimeMaps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            DefaultDocumentEnabled = true;
            DefaultDocuments = new List<string>(BuiltInDefaultDocuments);
        }

        public static WebServerConfig Load(string physicalDir)
        {
            WebServerConfig cfg = new WebServerConfig();
            try
            {
                string path = Path.Combine(physicalDir, "web.config");
                if (!File.Exists(path))
                {
                    return cfg;
                }

                XmlDocument doc = new XmlDocument();
                doc.Load(path);
                XmlNode ws = doc.SelectSingleNode("/configuration/system.webServer");
                if (ws == null)
                {
                    return cfg;
                }

                cfg.ReadCustomHeaders(ws.SelectSingleNode("httpProtocol/customHeaders"));
                cfg.ReadStaticContent(ws.SelectSingleNode("staticContent"));
                cfg.ReadDefaultDocument(ws.SelectSingleNode("defaultDocument"));
            }
            catch (Exception)
            {
                // A malformed section must never take the site down; fall back to defaults.
            }
            return cfg;
        }

        private void ReadCustomHeaders(XmlNode node)
        {
            if (node == null) return;
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.NodeType != XmlNodeType.Element) continue;
                string name = Attr(child, "name");
                switch (child.Name)
                {
                    case "clear":
                        CustomHeaders.Clear();
                        break;
                    case "remove":
                        CustomHeaders.RemoveAll(h => string.Equals(h.Key, name, StringComparison.OrdinalIgnoreCase));
                        break;
                    case "add":
                        if (!string.IsNullOrEmpty(name))
                        {
                            CustomHeaders.RemoveAll(h => string.Equals(h.Key, name, StringComparison.OrdinalIgnoreCase));
                            CustomHeaders.Add(new KeyValuePair<string, string>(name, Attr(child, "value") ?? string.Empty));
                        }
                        break;
                }
            }
        }

        private void ReadStaticContent(XmlNode node)
        {
            if (node == null) return;
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.NodeType != XmlNodeType.Element) continue;
                string ext = NormalizeExtension(Attr(child, "fileExtension"));
                switch (child.Name)
                {
                    case "clear":
                        MimeMaps.Clear();
                        break;
                    case "remove":
                        if (ext != null) MimeMaps.Remove(ext);
                        break;
                    case "mimeMap":
                        string mime = Attr(child, "mimeType");
                        if (ext != null && !string.IsNullOrEmpty(mime)) MimeMaps[ext] = mime;
                        break;
                    case "clientCache":
                        ReadClientCache(child);
                        break;
                }
            }
        }

        private void ReadClientCache(XmlNode node)
        {
            string mode = Attr(node, "cacheControlMode") ?? string.Empty;
            string custom = Attr(node, "cacheControlCustom");

            if (string.Equals(mode, "UseMaxAge", StringComparison.OrdinalIgnoreCase))
            {
                TimeSpan maxAge;
                if (TimeSpan.TryParse(Attr(node, "cacheControlMaxAge") ?? string.Empty, CultureInfo.InvariantCulture, out maxAge))
                {
                    CacheControl = "max-age=" + ((long)maxAge.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }
            }
            else if (string.Equals(mode, "DisableCache", StringComparison.OrdinalIgnoreCase))
            {
                CacheControl = "no-cache";
            }
            else if (string.Equals(mode, "UseExpires", StringComparison.OrdinalIgnoreCase))
            {
                string expires = Attr(node, "httpExpires");
                if (!string.IsNullOrEmpty(expires)) Expires = expires;
            }

            if (!string.IsNullOrEmpty(custom))
            {
                CacheControl = string.IsNullOrEmpty(CacheControl) ? custom : CacheControl + ", " + custom;
            }
        }

        private void ReadDefaultDocument(XmlNode node)
        {
            if (node == null) return;
            string enabled = Attr(node, "enabled");
            if (string.Equals(enabled, "false", StringComparison.OrdinalIgnoreCase))
            {
                DefaultDocumentEnabled = false;
            }

            XmlNode files = node.SelectSingleNode("files");
            if (files == null) return;

            // Site entries take priority over the inherited built-in list (unless <clear/>).
            List<string> siteEntries = new List<string>();
            List<string> inherited = new List<string>(BuiltInDefaultDocuments);
            foreach (XmlNode child in files.ChildNodes)
            {
                if (child.NodeType != XmlNodeType.Element) continue;
                string value = Attr(child, "value");
                switch (child.Name)
                {
                    case "clear":
                        siteEntries.Clear();
                        inherited.Clear();
                        break;
                    case "remove":
                        siteEntries.RemoveAll(v => string.Equals(v, value, StringComparison.OrdinalIgnoreCase));
                        inherited.RemoveAll(v => string.Equals(v, value, StringComparison.OrdinalIgnoreCase));
                        break;
                    case "add":
                        if (!string.IsNullOrEmpty(value)) siteEntries.Add(value.Trim().TrimStart('/', '\\'));
                        break;
                }
            }

            siteEntries.AddRange(inherited);
            DefaultDocuments = siteEntries;
        }

        /// <summary>True when the request maps to an existing file that ASP.NET serves as static content.</summary>
        public static bool IsStaticFile(string physicalPath)
        {
            if (string.IsNullOrEmpty(physicalPath)) return false;
            try
            {
                return File.Exists(physicalPath) && !DynamicExtensions.Contains(Path.GetExtension(physicalPath));
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static string NormalizeExtension(string ext)
        {
            if (string.IsNullOrWhiteSpace(ext)) return null;
            ext = ext.Trim();
            return ext.StartsWith(".", StringComparison.Ordinal) ? ext : "." + ext;
        }

        private static string Attr(XmlNode node, string name)
        {
            XmlAttribute a = node.Attributes != null ? node.Attributes[name] : null;
            return a != null ? a.Value : null;
        }
    }
}
