using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace HearthPortableWebServer.StressTest
{
    internal static class Program
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private static int Main(string[] args)
        {
            Options options = Options.Parse(args);
            if (options.ShowHelp)
            {
                Options.PrintHelp();
                return 0;
            }

            // When no --url was given on the command line, ask for the target host
            // interactively so it can be changed at runtime without rebuilding.
            if (!options.UrlProvided)
            {
                PromptForUrl(options);
            }

            try
            {
                return RunAsync(options).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Fatal error: " + ex.Message);
                return 1;
            }
        }

        private static void PromptForUrl(Options options)
        {
            Console.WriteLine("Hearth Portable ASP.NET Web Server - Stress Test");
            Console.WriteLine("Enter the target host to test. Examples:");
            Console.WriteLine("    localhost:8080        host:port        http://10.0.0.5/app/");
            Console.WriteLine();

            while (true)
            {
                Console.Write("Target host [" + options.Url + "]: ");
                string input = Console.ReadLine();

                // Empty input (or no console / piped EOF) keeps the default.
                if (input == null || input.Trim().Length == 0)
                {
                    return;
                }

                string normalized = Options.NormalizeUrl(input);
                if (normalized.Length > 0)
                {
                    options.Url = normalized;
                    return;
                }

                Console.WriteLine("  '" + input.Trim() + "' is not a valid address. Try e.g. localhost:8080");
            }
        }

        private static async Task<int> RunAsync(Options options)
        {
            int maxLevel = 1;
            for (int i = 0; i < options.Levels.Count; i++)
            {
                if (options.Levels[i] > maxLevel)
                {
                    maxLevel = options.Levels[i];
                }
            }

            // Raise the connection ceiling: the default of 2 per host would otherwise cap
            // throughput far below what the server can actually serve.
            ServicePointManager.DefaultConnectionLimit = Math.Max(maxLevel * 2, 4096);
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.UseNagleAlgorithm = false;

            Console.WriteLine("Hearth Portable ASP.NET Web Server - Stress Test");
            Console.WriteLine("  Target    : " + options.Url);
            Console.WriteLine("  Levels    : " + string.Join(", ", LevelsToStrings(options.Levels)));
            Console.WriteLine("  Duration  : " + options.DurationSeconds.ToString(Inv) + " s/level (warmup "
                              + options.WarmupSeconds.ToString(Inv) + " s)");
            Console.WriteLine("  Client    : " + Environment.ProcessorCount.ToString(Inv) + " logical CPUs");
            Console.WriteLine();

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                client.DefaultRequestHeaders.ConnectionClose = false;

                if (!await CheckReachableAsync(client, options.Url))
                {
                    Console.Error.WriteLine("ERROR: target is not reachable: " + options.Url);
                    Console.Error.WriteLine("Make sure the web server is running (e.g. HearthPortableWebServer.Host.exe --port 8080).");
                    return 2;
                }

                if (options.WarmupSeconds > 0)
                {
                    Console.WriteLine("Warming up for " + options.WarmupSeconds.ToString(Inv) + " s ...");
                    int warmConcurrency = maxLevel < 8 ? maxLevel : 8;
                    await StressEngine.RunStageAsync(client, options.Url, warmConcurrency, options.WarmupSeconds);
                }

                List<StageResult> results = new List<StageResult>();
                for (int i = 0; i < options.Levels.Count; i++)
                {
                    int level = options.Levels[i];

                    if (options.CooldownSeconds > 0)
                    {
                        await Task.Delay(options.CooldownSeconds * 1000);
                    }

                    Console.Write("Level " + level.ToString(Inv).PadLeft(4) + " : running " +
                                  options.DurationSeconds.ToString(Inv) + " s ... ");

                    StageResult result = await StressEngine.RunStageAsync(
                        client, options.Url, level, options.DurationSeconds);
                    results.Add(result);

                    Console.WriteLine(
                        result.RequestsPerSecond.ToString("N1", Inv) + " req/s | " +
                        result.RequestsPerMinute.ToString("N0", Inv) + " req/min | avg " +
                        result.LatencyAvg.ToString("N2", Inv) + " ms | p99 " +
                        result.LatencyP99.ToString("N2", Inv) + " ms | ok " +
                        result.Successes.ToString("N0", Inv) + " fail " +
                        result.Failures.ToString("N0", Inv));
                }

                DateTime now = DateTime.Now;
                string report = ReportWriter.BuildReport(options, results, now);

                string outPath = ResolveOutPath(options.OutPath, now);
                Directory.CreateDirectory(Path.GetDirectoryName(outPath));
                File.WriteAllText(outPath, report);

                Console.WriteLine();
                Console.WriteLine(report);
                Console.WriteLine("Report written to: " + outPath);

                StageResult peak = ReportWriter.FindPeak(results);
                if (peak != null)
                {
                    Console.WriteLine();
                    Console.WriteLine("==> MAX THROUGHPUT: " + peak.RequestsPerSecond.ToString("N1", Inv) +
                                      " req/s  (" + peak.RequestsPerMinute.ToString("N0", Inv) +
                                      " req/min) at concurrency " + peak.Concurrency.ToString(Inv));
                }
            }

            return 0;
        }

        private static async Task<bool> CheckReachableAsync(HttpClient client, string url)
        {
            try
            {
                using (HttpResponseMessage resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    await resp.Content.ReadAsByteArrayAsync();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string[] LevelsToStrings(List<int> levels)
        {
            string[] result = new string[levels.Count];
            for (int i = 0; i < levels.Count; i++)
            {
                result[i] = levels[i].ToString(Inv);
            }
            return result;
        }

        private static string ResolveOutPath(string requested, DateTime now)
        {
            if (!string.IsNullOrEmpty(requested))
            {
                return Path.GetFullPath(requested);
            }

            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "stress-logs");
            string file = "stress-" + now.ToString("yyyyMMdd-HHmmss", Inv) + ".log";
            return Path.Combine(dir, file);
        }
    }
}