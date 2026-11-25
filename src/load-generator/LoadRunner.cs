using Shared;

namespace LoadGenerator;

public sealed record RunOutput(BenchmarkResult Result, IReadOnlyList<RequestResult> Requests);

public sealed class LoadRunner
{
    private readonly BenchmarkConfig _config;
    private readonly int? _serverPid;

    public LoadRunner(BenchmarkConfig config, int? serverPid)
    {
        _config = config;
        _serverPid = serverPid;
    }

    public async Task<RunOutput> RunOnceAsync(int repeatIndex, CancellationToken ct = default)
    {
        byte[] payload = PayloadGenerator.Create(_config.PayloadSizeBytes);
        var collector = new MetricsCollector();
        var cpu = _serverPid is int pid ? new CpuMonitor(pid, _config.CpuSampleIntervalMs) : null;

        using var stop = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var clients = Enumerable.Range(0, _config.ClientCount)
            .Select(i => new TcpLoadClient(i, _config, payload, collector))
            .ToList();

        try
        {
            string?[] connectErrors = await Task.WhenAll(clients.Select(c => c.TryConnectAsync(stop.Token)));
            int connected = connectErrors.Count(e => e is null);
            if (connected == 0)
                throw new InvalidOperationException(
                    $"Không Client nào kết nối được tới {_config.Host}:{_config.Port}. Server đã chạy chưa? ({connectErrors[0]})");
            if (connected < clients.Count)
                Console.WriteLine($"  [cảnh báo] chỉ {connected}/{clients.Count} Client kết nối được, các Client còn lại sẽ thử lại trong lúc chạy.");

            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Task[] running = clients.Select(c => c.RunAsync(gate.Task, stop.Token)).ToArray();
            gate.SetResult();

            await Task.Delay(TimeSpan.FromSeconds(_config.WarmupSeconds), stop.Token)
            cpu?.Start();
            collector.MarkMeasurementStart();
            await Task.Delay(TimeSpan.FromSeconds(_config.DurationSeconds), stop.Token);
            MetricsSnapshot snap = collector.Complete();
            CpuSnapshot cpuSnap = cpu is null ? new CpuSnapshot(0, 0, 0) : await cpu.StopAsync();

            stop.Cancel();
            await Task.WhenAll(running);

            var result = new BenchmarkResult(
                _config, repeatIndex, snap.MeasurementSeconds,
                snap.TotalRequests, snap.SuccessfulRequests, snap.FailedRequests,
                snap.AvgLatencyMs, snap.P95LatencyMs, snap.ThroughputRps, snap.ErrorRate,
                cpuSnap.AveragePercent, cpuSnap.MaxPercent, cpuSnap.SampleCount,
                snap.StartedUtc, snap.EndedUtc);

            return new RunOutput(result, snap.Results);
        }
        finally
        {
            stop.Cancel();
            foreach (var c in clients) c.Dispose();
        }
    }
}
