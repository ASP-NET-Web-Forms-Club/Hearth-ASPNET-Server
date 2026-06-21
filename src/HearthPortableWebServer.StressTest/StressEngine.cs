using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace HearthPortableWebServer.StressTest
{
    /// <summary>
    /// Drives a fixed number of concurrent virtual clients against the target URL for a
    /// set duration, then aggregates throughput and latency statistics.
    /// </summary>
    internal static class StressEngine
    {
        public static async Task<StageResult> RunStageAsync(
            HttpClient client, string url, int concurrency, int durationSeconds)
        {
            List<double>[] latencies = new List<double>[concurrency];
            long[] okCounts = new long[concurrency];
            long[] failCounts = new long[concurrency];
            long[] byteCounts = new long[concurrency];
            Dictionary<int, long>[] statusMaps = new Dictionary<int, long>[concurrency];

            double freq = Stopwatch.Frequency;
            long durationTicks = (long)(durationSeconds * freq);

            Stopwatch stageSw = Stopwatch.StartNew();

            Task[] workers = new Task[concurrency];
            for (int w = 0; w < concurrency; w++)
            {
                int idx = w;
                workers[idx] = RunWorkerAsync(client, url, idx, stageSw, durationTicks, freq,
                    latencies, okCounts, failCounts, byteCounts, statusMaps);
            }

            await Task.WhenAll(workers);
            stageSw.Stop();

            double elapsedSeconds = stageSw.ElapsedTicks / freq;
            return Aggregate(concurrency, elapsedSeconds, latencies, okCounts, failCounts, byteCounts, statusMaps);
        }

        private static async Task RunWorkerAsync(
            HttpClient client, string url, int idx, Stopwatch sw, long durationTicks, double freq,
            List<double>[] latencies, long[] okCounts, long[] failCounts, long[] byteCounts,
            Dictionary<int, long>[] statusMaps)
        {
            List<double> myLatencies = new List<double>(2048);
            Dictionary<int, long> myStatus = new Dictionary<int, long>();
            long myOk = 0;
            long myFail = 0;
            long myBytes = 0;

            while (sw.ElapsedTicks < durationTicks)
            {
                long t0 = sw.ElapsedTicks;
                int code = 0;
                try
                {
                    using (HttpResponseMessage resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                    {
                        byte[] data = await resp.Content.ReadAsByteArrayAsync();
                        myBytes += data.Length;
                        code = (int)resp.StatusCode;
                        if (code < 400)
                        {
                            myOk++;
                        }
                        else
                        {
                            myFail++;
                        }
                    }
                }
                catch (Exception)
                {
                    // Code 0 == transport error / timeout.
                    myFail++;
                }

                long t1 = sw.ElapsedTicks;
                myLatencies.Add((t1 - t0) * 1000.0 / freq);

                long current;
                if (myStatus.TryGetValue(code, out current))
                {
                    myStatus[code] = current + 1;
                }
                else
                {
                    myStatus[code] = 1;
                }
            }

            latencies[idx] = myLatencies;
            okCounts[idx] = myOk;
            failCounts[idx] = myFail;
            byteCounts[idx] = myBytes;
            statusMaps[idx] = myStatus;
        }

        private static StageResult Aggregate(
            int concurrency, double elapsedSeconds, List<double>[] latencies,
            long[] okCounts, long[] failCounts, long[] byteCounts, Dictionary<int, long>[] statusMaps)
        {
            StageResult result = new StageResult();
            result.Concurrency = concurrency;
            result.ElapsedSeconds = elapsedSeconds;

            long totalCount = 0;
            for (int i = 0; i < concurrency; i++)
            {
                if (latencies[i] != null)
                {
                    totalCount += latencies[i].Count;
                }
                result.Successes += okCounts[i];
                result.Failures += failCounts[i];
                result.BytesReceived += byteCounts[i];

                if (statusMaps[i] != null)
                {
                    foreach (KeyValuePair<int, long> kv in statusMaps[i])
                    {
                        long current;
                        if (result.StatusCounts.TryGetValue(kv.Key, out current))
                        {
                            result.StatusCounts[kv.Key] = current + kv.Value;
                        }
                        else
                        {
                            result.StatusCounts[kv.Key] = kv.Value;
                        }
                    }
                }
            }

            result.Requests = totalCount;
            result.RequestsPerSecond = elapsedSeconds > 0 ? totalCount / elapsedSeconds : 0;
            result.RequestsPerMinute = result.RequestsPerSecond * 60.0;

            // Merge all latency samples into one array for percentile math.
            double[] all = new double[totalCount];
            int pos = 0;
            double sum = 0;
            for (int i = 0; i < concurrency; i++)
            {
                if (latencies[i] == null)
                {
                    continue;
                }
                List<double> list = latencies[i];
                for (int j = 0; j < list.Count; j++)
                {
                    double v = list[j];
                    all[pos++] = v;
                    sum += v;
                }
            }

            if (totalCount > 0)
            {
                Array.Sort(all);
                result.LatencyMin = all[0];
                result.LatencyMax = all[totalCount - 1];
                result.LatencyAvg = sum / totalCount;
                result.LatencyP50 = Percentile(all, 50.0);
                result.LatencyP90 = Percentile(all, 90.0);
                result.LatencyP95 = Percentile(all, 95.0);
                result.LatencyP99 = Percentile(all, 99.0);
            }

            return result;
        }

        private static double Percentile(double[] sorted, double percentile)
        {
            int n = sorted.Length;
            if (n == 0)
            {
                return 0;
            }

            int rank = (int)Math.Ceiling(percentile / 100.0 * n) - 1;
            if (rank < 0)
            {
                rank = 0;
            }
            if (rank >= n)
            {
                rank = n - 1;
            }
            return sorted[rank];
        }
    }
}
