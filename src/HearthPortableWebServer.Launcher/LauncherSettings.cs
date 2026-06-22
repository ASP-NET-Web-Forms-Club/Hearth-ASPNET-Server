using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace HearthPortableWebServer.Launcher
{
    /// <summary>
    /// Persists launcher state to a plain-text "key=value" file (Settings.txt) next to
    /// the executable. Deliberately dependency-free: no JSON/XML serializer is shipped.
    /// Parsing splits each line on the FIRST '=' only; lines that are blank or start with
    /// '#' or ';' are treated as comments and ignored.
    ///
    /// The web root is stored as a path RELATIVE to the executable directory so the whole
    /// folder stays portable (e.g. "wwwroot" or "..\..\wwwroot"). It is resolved to an
    /// absolute path on load and re-relativized on save when possible.
    /// </summary>
    internal sealed class LauncherSettings
    {
        private const string FileName = "Settings.txt";

        public int Port { get; set; }
        public string RootRelative { get; set; }
        public string ServiceName { get; set; }

        public LauncherSettings()
        {
            // Defaults used when no settings file exists yet.
            Port = 8080;
            RootRelative = "wwwroot";
            // Service name is empty until a service is actually installed; it is then
            // recorded as the port-based name (IpcNames.ServiceName(port)).
            ServiceName = string.Empty;
        }

        private static string SettingsPath()
        {
            return Path.Combine(Application.StartupPath, FileName);
        }

        /// <summary>
        /// Resolves the stored relative root against the executable directory and returns
        /// an absolute path. Falls back gracefully if the stored value is empty.
        /// </summary>
        public string ResolveRootAbsolute()
        {
            string rel = string.IsNullOrEmpty(RootRelative) ? "wwwroot" : RootRelative;
            try
            {
                return Path.GetFullPath(Path.Combine(Application.StartupPath, rel));
            }
            catch
            {
                return Path.Combine(Application.StartupPath, "wwwroot");
            }
        }

        /// <summary>
        /// Stores an absolute root path as a path relative to the executable directory
        /// when possible; otherwise keeps the absolute path verbatim (e.g. a different
        /// drive, where no relative path exists).
        /// </summary>
        public void SetRootFromAbsolute(string absolute)
        {
            if (string.IsNullOrEmpty(absolute))
            {
                RootRelative = "wwwroot";
                return;
            }

            try
            {
                string baseDir = AppendSeparator(Application.StartupPath);
                Uri baseUri = new Uri(baseDir);
                Uri targetUri = new Uri(AppendSeparator(absolute));
                Uri relUri = baseUri.MakeRelativeUri(targetUri);
                string rel = Uri.UnescapeDataString(relUri.ToString())
                    .Replace('/', Path.DirectorySeparatorChar)
                    .TrimEnd(Path.DirectorySeparatorChar);

                // Cross-drive or otherwise non-relative: MakeRelativeUri keeps it absolute.
                if (rel.Length == 0)
                {
                    rel = ".";
                }
                RootRelative = rel;
            }
            catch
            {
                // If anything goes wrong, store the absolute path as-is.
                RootRelative = absolute;
            }
        }

        private static string AppendSeparator(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }
            if (path[path.Length - 1] == Path.DirectorySeparatorChar
                || path[path.Length - 1] == Path.AltDirectorySeparatorChar)
            {
                return path;
            }
            return path + Path.DirectorySeparatorChar;
        }

        public static LauncherSettings Load()
        {
            LauncherSettings settings = new LauncherSettings();
            string path = SettingsPath();

            try
            {
                if (!File.Exists(path))
                {
                    return settings;
                }

                Dictionary<string, string> map = new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);

                foreach (string raw in File.ReadAllLines(path))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line[0] == '#' || line[0] == ';')
                    {
                        continue;
                    }

                    int eq = line.IndexOf('=');
                    if (eq <= 0)
                    {
                        continue;
                    }

                    string key = line.Substring(0, eq).Trim();
                    string value = line.Substring(eq + 1).Trim();
                    if (key.Length > 0)
                    {
                        map[key] = value;
                    }
                }

                string portText;
                if (map.TryGetValue("port", out portText))
                {
                    int parsed;
                    if (int.TryParse(portText, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
                        && parsed >= 1 && parsed <= 65535)
                    {
                        settings.Port = parsed;
                    }
                }

                string rootText;
                if (map.TryGetValue("root", out rootText) && rootText.Length > 0)
                {
                    settings.RootRelative = rootText;
                }

                string svcText;
                if (map.TryGetValue("service", out svcText) && svcText.Length > 0)
                {
                    settings.ServiceName = svcText;
                }
            }
            catch
            {
                // Corrupt or unreadable file: fall back to defaults rather than crashing.
                return new LauncherSettings();
            }

            return settings;
        }

        public void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# Hearth Portable ASP.NET Web Server - launcher settings");
                sb.AppendLine("# Plain text. One key=value per line. Lines starting with # or ; are comments.");
                sb.AppendLine("# 'root' is relative to this program's folder (e.g. wwwroot or ..\\..\\wwwroot).");
                sb.AppendLine();
                sb.AppendLine("port=" + Port.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("root=" + (RootRelative ?? "wwwroot"));
                sb.AppendLine("service=" + (ServiceName ?? string.Empty));

                File.WriteAllText(SettingsPath(), sb.ToString(), new UTF8Encoding(false));
            }
            catch
            {
                // Persistence is best-effort; never let a write failure crash the UI.
            }
        }
    }
}
