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

        public static string GetAppDirectory()
        {
            try
            {
                string asmLocation = typeof(ManagerConfig).Assembly.Location;
                if (!string.IsNullOrEmpty(asmLocation))
                {
                    string dir = Path.GetDirectoryName(asmLocation);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    {
                        return dir;
                    }
                }
            }
            catch { }

            string startup = AppDomain.CurrentDomain.BaseDirectory;
            return !string.IsNullOrEmpty(startup) ? startup : ".";
        }

        public static string ConfigPath()
        {
            return Path.Combine(GetAppDirectory(), ConfigFileName);
        }

        public static string ResolveRootAbsolute(string root)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                return Path.Combine(GetAppDirectory(), "wwwroot");
            }
            string trimmed = root.Trim();
            if (Path.IsPathRooted(trimmed))
            {
                return Path.GetFullPath(trimmed);
            }
            return Path.GetFullPath(Path.Combine(GetAppDirectory(), trimmed));
        }

        public static string FormatRootPath(string root)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                return "wwwroot";
            }

            try
            {
                string fullPath = ResolveRootAbsolute(root.Trim());
                string appDir = Path.GetFullPath(GetAppDirectory()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string cleanTarget = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                // If pointing to the exact main EXE root directory
                if (string.Equals(cleanTarget, appDir, StringComparison.OrdinalIgnoreCase))
                {
                    return ".";
                }

                // If pointing to a subfolder inside the main EXE folder (e.g. wwwroot, site2)
                string prefix = appDir + Path.DirectorySeparatorChar;
                if (cleanTarget.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return cleanTarget.Substring(prefix.Length);
                }

                // Other than current folder root path of main EXE -> use absolute path
                return fullPath;
            }
            catch
            {
                return root;
            }
        }

        public static string RelativizePath(string absolutePath)
        {
            return FormatRootPath(absolutePath);
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
                        bool needsResave = false;
                        foreach (SiteEntry s in loaded)
                        {
                            string formatted = FormatRootPath(s.Root);
                            if (!string.Equals(s.Root, formatted, StringComparison.Ordinal))
                            {
                                s.Root = formatted;
                                needsResave = true;
                            }
                        }
                        config.Sites = loaded;
                        if (needsResave)
                        {
                            config.Save();
                        }
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

            string settingsTxt = Path.Combine(GetAppDirectory(), "Settings.txt");
            string serverConfig = Path.Combine(GetAppDirectory(), "server.config");

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
                        if (trimmed.Length == 0 || trimmed.StartsWith("#") || trimmed.StartsWith(";")) continue;
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

            list.Add(new SiteEntry("Default Website", port, FormatRootPath(root), false));
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
