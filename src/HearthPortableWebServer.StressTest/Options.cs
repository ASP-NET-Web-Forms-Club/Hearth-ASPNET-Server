using System;
using System.Collections.Generic;
using System.Globalization;

namespace HearthPortableWebServer.StressTest
{
    /// <summary>
    /// Parsed command-line options for the stress test.
    /// </summary>
    internal sealed class Options
    {
        public string Url = "http://localhost:8080/";
        public bool UrlProvided = false;   // true when --url was supplied on the command line
        public int DurationSeconds = 10;     // measured time at each concurrency level
        public int WarmupSeconds = 3;        // discarded; lets ASP.NET compile / JIT
        public int CooldownSeconds = 1;      // pause between levels
        public int TimeoutSeconds = 30;      // per-request timeout
        public List<int> Levels = new List<int>();
        public string OutPath = string.Empty;
        public bool ShowHelp = false;

        public static Options Parse(string[] args)
        {
            Options o = new Options();

            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i].ToLowerInvariant();
                switch (a)
                {
                    case "--url":
                    case "-u":
                        if (i + 1 < args.Length)
                        {
                            o.Url = NormalizeUrl(args[++i]);
                            o.UrlProvided = true;
                        }
                        break;
                    case "--duration":
                    case "-d":
                        if (i + 1 < args.Length) o.DurationSeconds = ParseInt(args[++i], o.DurationSeconds);
                        break;
                    case "--warmup":
                    case "-w":
                        if (i + 1 < args.Length) o.WarmupSeconds = ParseInt(args[++i], o.WarmupSeconds);
                        break;
                    case "--cooldown":
                        if (i + 1 < args.Length) o.CooldownSeconds = ParseInt(args[++i], o.CooldownSeconds);
                        break;
                    case "--timeout":
                    case "-t":
                        if (i + 1 < args.Length) o.TimeoutSeconds = ParseInt(args[++i], o.TimeoutSeconds);
                        break;
                    case "--levels":
                    case "-l":
                        if (i + 1 < args.Length) o.Levels = ParseLevels(args[++i]);
                        break;
                    case "--out":
                    case "-o":
                        if (i + 1 < args.Length) o.OutPath = args[++i];
                        break;
                    case "--help":
                    case "-h":
                    case "/?":
                        o.ShowHelp = true;
                        break;
                }
            }

            if (o.Levels.Count == 0)
            {
                int[] defaults = { 1, 2, 4, 8, 16, 32, 64, 128, 256 };
                for (int i = 0; i < defaults.Length; i++)
                {
                    o.Levels.Add(defaults[i]);
                }
            }

            return o;
        }

        /// <summary>
        /// Accepts loose host input ("localhost:8080", "10.0.0.5:80", "http://host/path")
        /// and returns a normalized absolute URL, or an empty string if it cannot be parsed.
        /// A bare host is normalized to "http://host:port/".
        /// </summary>
        public static string NormalizeUrl(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            string text = input.Trim();
            if (!text.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !text.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                text = "http://" + text;
            }

            Uri uri;
            if (Uri.TryCreate(text, UriKind.Absolute, out uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return uri.AbsoluteUri;
            }
            return string.Empty;
        }

        private static int ParseInt(string text, int fallback)
        {
            int value;
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value > 0)
            {
                return value;
            }
            return fallback;
        }

        private static List<int> ParseLevels(string text)
        {
            List<int> result = new List<int>();
            string[] parts = text.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                int value;
                if (int.TryParse(parts[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value > 0)
                {
                    result.Add(value);
                }
            }
            return result;
        }

        public static void PrintHelp()
        {
            Console.WriteLine("Hearth Portable ASP.NET Web Server - Stress Test");
            Console.WriteLine();
            Console.WriteLine("Usage: HearthPortableWebServer.StressTest.exe [options]");
            Console.WriteLine("  --url, -u <url>        Target URL (default http://localhost:8080/)");
            Console.WriteLine("  --duration, -d <sec>   Measured seconds per concurrency level (default 10)");
            Console.WriteLine("  --warmup, -w <sec>     Warmup seconds, results discarded (default 3)");
            Console.WriteLine("  --cooldown <sec>       Pause between levels (default 1)");
            Console.WriteLine("  --timeout, -t <sec>    Per-request timeout (default 30)");
            Console.WriteLine("  --levels, -l <list>    Comma list of concurrency levels");
            Console.WriteLine("                         (default 1,2,4,8,16,32,64,128,256)");
            Console.WriteLine("  --out, -o <path>       Report file path (default .\\stress-logs\\stress-<ts>.log)");
            Console.WriteLine("  --help, -h             Show this help");
        }
    }
}
