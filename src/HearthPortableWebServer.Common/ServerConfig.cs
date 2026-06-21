using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace HearthPortableWebServer.Common
{
    /// <summary>
    /// Simple key=value configuration persisted next to the Host executable.
    /// Used so the Windows Service (which receives no command-line arguments from
    /// the SCM) can discover which port / root to serve.
    /// </summary>
    public sealed class ServerConfig
    {
        public int Port { get; set; }
        public string Root { get; set; }

        public const int DefaultPort = 8080;

        public ServerConfig()
        {
            Port = DefaultPort;
            Root = string.Empty;
        }

        public ServerConfig(int port, string root)
        {
            Port = port;
            Root = root;
        }

        public static string DefaultConfigPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server.config");
        }

        public void Save(string path)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Port=").Append(Port.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
            sb.Append("Root=").Append(Root ?? string.Empty).Append("\r\n");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        public static ServerConfig Load(string path)
        {
            ServerConfig config = new ServerConfig();
            if (!File.Exists(path))
            {
                return config;
            }

            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrEmpty(line))
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

                if (string.Equals(key, "Port", StringComparison.OrdinalIgnoreCase))
                {
                    int parsed;
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                    {
                        config.Port = parsed;
                    }
                }
                else if (string.Equals(key, "Root", StringComparison.OrdinalIgnoreCase))
                {
                    config.Root = value;
                }
            }

            return config;
        }
    }
}
