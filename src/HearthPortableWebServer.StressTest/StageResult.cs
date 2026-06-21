using System.Collections.Generic;

namespace HearthPortableWebServer.StressTest
{
    /// <summary>
    /// Aggregated measurements for one concurrency level.
    /// </summary>
    internal sealed class StageResult
    {
        public int Concurrency;
        public long Requests;
        public long Successes;
        public long Failures;
        public long BytesReceived;
        public double ElapsedSeconds;

        public double RequestsPerSecond;
        public double RequestsPerMinute;

        // Latency in milliseconds.
        public double LatencyMin;
        public double LatencyAvg;
        public double LatencyP50;
        public double LatencyP90;
        public double LatencyP95;
        public double LatencyP99;
        public double LatencyMax;

        public Dictionary<int, long> StatusCounts = new Dictionary<int, long>();
    }
}
