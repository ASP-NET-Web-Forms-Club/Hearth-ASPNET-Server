using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using HearthPortableWebServer.Manager.Models;

namespace HearthPortableWebServer.Manager.Services
{
    public sealed class ManagerConfig
    {
        private const string ConfigFileName = "manager_sites.json";
        public List<SiteEntry> Sites { get; set; }

        public ManagerConfig()
        {
            Sites = new List<SiteEntry>();
        }

        public static string ConfigPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
        }

        public static string ResolveRootAbsolute(string root)
        {
            if (string.IsNullOrEmpty(root))
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
            }
            if (Path.IsPathRooted(root))
            {
                return Path.GetFullPath(root);
            }
            return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, root));
        }

        public static string RelativizePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
            {
                return "wwwroot";
            }
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                if (!baseDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    baseDir += Path.DirectorySeparatorChar;
                }
                Uri baseUri = new Uri(baseDir);
                Uri targetUri = new Uri(absolutePath.EndsWith(Path.DirectorySeparatorChar.ToString()) ? absolutePath : absolutePath + Path.DirectorySeparatorChar);
                Uri relUri = baseUri.MakeRelativeUri(targetUri);
                string rel = Uri.UnescapeDataString(relUri.ToString())
                    .Replace('/', Path.DirectorySeparatorChar)
                    .TrimEnd(Path.DirectorySeparatorChar);

                return string.IsNullOrEmpty(rel) ? "." : rel;
            }
            catch
            {
                return absolutePath;
            }
        }

        public static ManagerConfig Load()
        {
            string path = ConfigPath();
            ManagerConfig config = new ManagerConfig();

            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path, Encoding.UTF8);
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    List<SiteEntry> loaded = serializer.Deserialize<List<SiteEntry>>(json);
                    if (loaded != null && loaded.Count > 0)
                    {
                        config.Sites = loaded;
                        return config;
                    }
                }
                catch
                {
                    // Fall back if corrupted
                }
            }

            // Auto-migration: Check if existing Settings.txt or server.config exists
            config.Sites = TryMigrateLegacySettings();
            config.Save();
            return config;
        }

        private static List<SiteEntry> TryMigrateLegacySettings()
        {
            List<SiteEntry> list = new List<SiteEntry>();

            string settingsTxt = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.txt");
            string serverConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server.config");

            int port = 8080;
            string root = "wwwroot";

            if (File.Exists(settingsTxt))
            {
                try
                {
                    string[] lines = File.ReadAllLines(settingsTxt);
                    foreach (string line in lines)
                    {
                        string trimmed = line.Trim();
                        if (trimmed.Length == 0 || trimmed.StartsWith("#") || trimmed.StartsWith(";")) continue;
                        int eq = trimmed.IndexOf('=');
                        if (eq > 0)
                        {
                            string k = trimmed.Substring(0, eq).Trim();
                            string v = trimmed.Substring(eq + 1).Trim();
                            if (string.Equals(k, "port", StringComparison.OrdinalIgnoreCase))
                            {
                                int p;
                                if (int.TryParse(v, out p)) port = p;
                            }
                            else if (string.Equals(k, "root", StringComparison.OrdinalIgnoreCase))
                            {
                                root = v;
                            }
                        }
                    }
                }
                catch { }
            }
            else if (File.Exists(serverConfig))
            {
                try
                {
                    string[] lines = File.ReadAllLines(serverConfig);
                    foreach (string line in lines)
                    {
                        string trimmed = line.Trim();
                        int eq = trimmed.IndexOf('=');
                        if (eq > 0)
                        {
                            string k = trimmed.Substring(0, eq).Trim();
                            string v = trimmed.Substring(eq + 1).Trim();
                            if (string.Equals(k, "Port", StringComparison.OrdinalIgnoreCase))
                            {
                                int p;
                                if (int.TryParse(v, out p)) port = p;
                            }
                            else if (string.Equals(k, "Root", StringComparison.OrdinalIgnoreCase))
                            {
                                root = v;
                            }
                        }
                    }
                }
                catch { }
            }

            list.Add(new SiteEntry("Default Website", port, root, false));
            return list;
        }

        public void Save()
        {
            try
            {
                string path = ConfigPath();
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                string rawJson = serializer.Serialize(Sites);
                string formattedJson = PrettyPrintJson(rawJson);
                File.WriteAllText(path, formattedJson, Encoding.UTF8);
            }
            catch { }
        }

        private static string PrettyPrintJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return "[]";
            StringBuilder sb = new StringBuilder();
            int indent = 0;
            bool quoted = false;

            for (int i = 0; i < json.Length; i++)
            {
                char ch = json[i];
                if (ch == '"')
                {
                    bool isEscaped = (i > 0 && json[i - 1] == '\\');
                    if (!isEscaped) quoted = !quoted;
                    sb.Append(ch);
                }
                else if (!quoted)
                {
                    if (ch == '{' || ch == '[')
                    {
                        sb.Append(ch);
                        sb.AppendLine();
                        indent++;
                        sb.Append(new string(' ', indent * 2));
                    }
                    else if (ch == '}' || ch == ']')
                    {
                        sb.AppendLine();
                        indent--;
                        sb.Append(new string(' ', Math.Max(0, indent * 2)));
                        sb.Append(ch);
                    }
                    else if (ch == ',')
                    {
                        sb.Append(ch);
                        sb.AppendLine();
                        sb.Append(new string(' ', indent * 2));
                    }
                    else if (ch == ':')
                    {
                        sb.Append(": ");
                    }
                    else if (!char.IsWhiteSpace(ch))
                    {
                        sb.Append(ch);
                    }
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }
    }
}
