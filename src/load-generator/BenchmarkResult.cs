using Shared;

namespace LoadGenerator;


public sealed record BenchmarkResult(
    BenchmarkConfig Config,
    int RepeatIndex,
    double MeasurementSeconds,
    int TotalRequests,
    int SuccessfulRequests,
    int FailedRequests,
    double AvgLatencyMs,
    double P95LatencyMs,
    double ThroughputRps,
    double ErrorRate,
    double CpuPercentAvg,
    double CpuPercentMax,
    int CpuSampleCount,
    DateTime StartedUtc,
    DateTime EndedUtc);