namespace LoadGenerator;

public sealed record MetricsSnapshot(
    int TotalRequests,
    int SuccessfulRequests,
    int FailedRequests,
    double AvgLatencyMs,
    double P95LatencyMs,
    double ThroughputRps,
    double ErrorRate,
    double MeasurementSeconds,
    DateTime StartedUtc,
    DateTime EndedUtc,
    IReadOnlyList<RequestResult> Results);

public sealed class MetricsCollector
{
    private readonly object _lock = new();
    private readonly List<RequestResult> _results = new();
    private DateTime? _measurementStartUtc;
    private DateTime? _measurementEndUtc;
    private bool _measuring;

    public void MarkMeasurementStart(DateTime? startUtc = null)
    {
        lock (_lock)
        {
            if (_measuring)
                throw new InvalidOperationException(
                    "Measurement đang chạy, không thể start lại. Gọi Complete() trước.");
            _results.Clear();
            _measurementStartUtc = startUtc ?? DateTime.UtcNow;
            _measurementEndUtc = null;
            _measuring = true;
        }
    }

    public void Record(RequestResult result)
    {
        lock (_lock)
        {
            if (_measuring) _results.Add(result);
        }
    }

    public MetricsSnapshot Complete(DateTime? endUtc = null)
    {
        lock (_lock)
        {
            if (!_measuring)
                throw new InvalidOperationException(
                    "Measurement chưa chạy, không thể Complete().");
            _measuring = false;
            _measurementEndUtc = endUtc ?? DateTime.UtcNow;
            return Compute();
        }
    }

    private MetricsSnapshot Compute()
    {
        var total = _results.Count;
        var startUtc = _measurementStartUtc!.Value;
        var endUtc = _measurementEndUtc!.Value;

        if (total == 0)
            return new MetricsSnapshot(0, 0, 0, 0, 0, 0, 0, 0, startUtc, endUtc,
                Array.Empty<RequestResult>());

        var success = 0;
        var latencies = new List<double>(total);
        foreach (var r in _results)
        {
            if (r.IsSuccess) { success++; latencies.Add(r.LatencyMs); }
        }
        var failed = total - success;

        var elapsedSeconds = (endUtc - startUtc).TotalSeconds;
        if (elapsedSeconds <= 0) elapsedSeconds = 1e-9;

        var avg = 0.0;
        var p95 = 0.0;
        if (latencies.Count > 0)
        {
            latencies.Sort();
            avg = latencies.Average();
            p95 = PercentileLinear(latencies, 0.95);
        }

        return new MetricsSnapshot(
            total, success, failed, avg, p95,
            success / elapsedSeconds,
            (double)failed / total,
            elapsedSeconds,
            startUtc, endUtc,
            _results.ToArray());
    }

    private static double PercentileLinear(List<double> sorted, double p)
    {
        if (sorted.Count == 0) return 0;
        if (sorted.Count == 1) return sorted[0];
        var pos = p * (sorted.Count - 1);
        var lower = (int)Math.Floor(pos);
        var upper = (int)Math.Ceiling(pos);
        if (lower == upper) return sorted[lower];
        var w = pos - lower;
        return sorted[lower] * (1 - w) + sorted[upper] * w;
    }
}