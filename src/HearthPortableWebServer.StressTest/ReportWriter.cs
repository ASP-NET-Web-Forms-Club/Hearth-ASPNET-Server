using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HearthPortableWebServer.StressTest
{
    /// <summary>
    /// Formats the collected stage results into a human-readable log report.
    /// </summary>
    internal static class ReportWriter
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string BuildReport(Options options, List<StageResult> results, DateTime timestamp)
        {
            StageResult peak = FindPeak(results);

            long totalRequests = 0;
            long totalFailures = 0;
            long totalBytes = 0;
            Dictionary<int, long> combinedStatus = new Dictionary<int, long>();

            for (int i = 0; i < results.Count; i++)
            {
                StageResult r = results[i];
                totalRequests += r.Requests;
                totalFailures += r.Failures;
                totalBytes += r.BytesReceived;
                foreach (KeyValuePair<int, long> kv in r.StatusCounts)
                {
                    long current;
                    if (combinedStatus.TryGetValue(kv.Key, out current))
                    {
                        combinedStatus[kv.Key] = current + kv.Value;
                    }
                    else
                    {
                        combinedStatus[kv.Key] = kv.Value;
                    }
                }
            }

            StringBuilder sb = new StringBuilder();
            string line = new string('=', 92);
            string thin = new string('-', 92);

            sb.AppendLine(line);
            sb.AppendLine(" Hearth Portable ASP.NET Web Server - Stress Test Report");
            sb.AppendLine(line);
            sb.AppendLine(" Generated  : " + timestamp.ToString("yyyy-MM-dd HH:mm:ss", Inv));
            sb.AppendLine(" Target URL : " + options.Url);
            sb.AppendLine(" Duration   : " + options.DurationSeconds.ToString(Inv) + " s measured per level (warmup "
                          + options.WarmupSeconds.ToString(Inv) + " s)");
            sb.AppendLine(" Machine    : " + Environment.ProcessorCount.ToString(Inv) + " logical CPUs, "
                          + (Environment.Is64BitProcess ? "x64" : "x86") + " test client");
            sb.AppendLine(thin);
            sb.AppendLine();
            sb.AppendLine(" Per-level results (latency in milliseconds):");
            sb.AppendLine();
            sb.AppendLine(Header());
            sb.AppendLine(HeaderRule());

            for (int i = 0; i < results.Count; i++)
            {
                sb.AppendLine(FormatRow(results[i]));
            }

            sb.AppendLine();
            sb.AppendLine(thin);
            sb.AppendLine(" PEAK THROUGHPUT");
            if (peak != null)
            {
                sb.AppendLine("   Concurrency level : " + peak.Concurrency.ToString(Inv));
                sb.AppendLine("   Requests / second : " + peak.RequestsPerSecond.ToString("N1", Inv));
                sb.AppendLine("   Requests / minute : " + peak.RequestsPerMinute.ToString("N0", Inv));
                sb.AppendLine("   Avg latency       : " + peak.LatencyAvg.ToString("N2", Inv) + " ms");
                sb.AppendLine("   p99 latency       : " + peak.LatencyP99.ToString("N2", Inv) + " ms");
            }
            sb.AppendLine(thin);
            sb.AppendLine(" Status code distribution (all levels):");
            foreach (KeyValuePair<int, long> kv in combinedStatus)
            {
                string label = kv.Key == 0 ? "ERR (transport/timeout)" : kv.Key.ToString(Inv);
                sb.AppendLine("   " + label.PadRight(24) + " : " + kv.Value.ToString("N0", Inv));
            }
            sb.AppendLine(thin);
            sb.AppendLine(" Total requests sent : " + totalRequests.ToString("N0", Inv));
            sb.AppendLine(" Total failures      : " + totalFailures.ToString("N0", Inv));
            sb.AppendLine(" Total data received : " + (totalBytes / 1048576.0).ToString("N1", Inv) + " MB");
            sb.AppendLine(line);

            return sb.ToString();
        }

        public static StageResult FindPeak(List<StageResult> results)
        {
            StageResult peak = null;
            for (int i = 0; i < results.Count; i++)
            {
                if (peak == null || results[i].RequestsPerSecond > peak.RequestsPerSecond)
                {
                    peak = results[i];
                }
            }
            return peak;
        }

        private static string Header()
        {
            return " " +
                "Conc".PadLeft(5) + " " +
                "Requests".PadLeft(10) + " " +
                "Req/sec".PadLeft(11) + " " +
                "Req/min".PadLeft(13) + " " +
                "OK".PadLeft(8) + " " +
                "Fail".PadLeft(6) + " " +
                "Avg".PadLeft(8) + " " +
                "p50".PadLeft(7) + " " +
                "p90".PadLeft(7) + " " +
                "p95".PadLeft(7) + " " +
                "p99".PadLeft(7) + " " +
                "Max".PadLeft(8);
        }

        private static string HeaderRule()
        {
            return " " +
                new string('-', 5) + " " +
                new string('-', 10) + " " +
                new string('-', 11) + " " +
                new string('-', 13) + " " +
                new string('-', 8) + " " +
                new string('-', 6) + " " +
                new string('-', 8) + " " +
                new string('-', 7) + " " +
                new string('-', 7) + " " +
                new string('-', 7) + " " +
                new string('-', 7) + " " +
                new string('-', 8);
        }

        private static string FormatRow(StageResult r)
        {
            return " " +
                r.Concurrency.ToString(Inv).PadLeft(5) + " " +
                r.Requests.ToString("N0", Inv).PadLeft(10) + " " +
                r.RequestsPerSecond.ToString("N1", Inv).PadLeft(11) + " " +
                r.RequestsPerMinute.ToString("N0", Inv).PadLeft(13) + " " +
                r.Successes.ToString("N0", Inv).PadLeft(8) + " " +
                r.Failures.ToString("N0", Inv).PadLeft(6) + " " +
                r.LatencyAvg.ToString("N2", Inv).PadLeft(8) + " " +
                r.LatencyP50.ToString("N1", Inv).PadLeft(7) + " " +
                r.LatencyP90.ToString("N1", Inv).PadLeft(7) + " " +
                r.LatencyP95.ToString("N1", Inv).PadLeft(7) + " " +
                r.LatencyP99.ToString("N1", Inv).PadLeft(7) + " " +
                r.LatencyMax.ToString("N1", Inv).PadLeft(8);
        }
    }
}