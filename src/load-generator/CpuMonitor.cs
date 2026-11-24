using System.Diagnostics;

namespace LoadGenerator;

public sealed record CpuSnapshot(
    double AveragePercent,
    double MaxPercent,
    int SampleCount);

public sealed class CpuMonitor
{
    private readonly int _serverPid;
    private readonly TimeSpan _interval;
    private readonly int _logicalCpuCount;
    private readonly List<double> _samples = new();
    private readonly object _lock = new();

    private Process? _process;
    private TimeSpan _lastCpuTime;
    private DateTime _lastWallUtc;
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private bool _started;

    public CpuMonitor(int serverPid, int sampleIntervalMs)
    {
        if (sampleIntervalMs <= 0)
            throw new ArgumentOutOfRangeException(nameof(sampleIntervalMs));
        _serverPid = serverPid;
        _interval = TimeSpan.FromMilliseconds(sampleIntervalMs);
        _logicalCpuCount = Math.Max(1, Environment.ProcessorCount);
    }

    public void Start()
    {
        if (_started) throw new InvalidOperationException("CpuMonitor đã Start().");

        try
        {
            _process = Process.GetProcessById(_serverPid);
            _process.Refresh();
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException(
                $"Server PID {_serverPid} không tồn tại. Kiểm tra lại PID truyền vào ", ex);
        }

        _lastCpuTime = _process.TotalProcessorTime;
        _lastWallUtc = DateTime.UtcNow;
        _started = true;

        _cts = new CancellationTokenSource();
        _loop = SamplingLoopAsync(_cts.Token);
    }

    public async Task<CpuSnapshot> StopAsync()
    {
        if (!_started) return new CpuSnapshot(0, 0, 0);

        if (_cts != null) _cts.Cancel();
        if (_loop != null)
        {
            try { await _loop.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
        }

        SampleOnce();

        lock (_lock)
        {
            if (_samples.Count == 0) return new CpuSnapshot(0, 0, 0);
            return new CpuSnapshot(_samples.Average(), _samples.Max(), _samples.Count);
        }
    }

    private async Task SamplingLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(_interval);
        try
        {
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                if (_process == null) break;
                SampleOnce();
            }
        }
        catch (OperationCanceledException) { }
    }

    private void SampleOnce()
    {
        var proc = _process;
        if (proc == null) return;
        try
        {
            proc.Refresh();
            var cpuTime = proc.TotalProcessorTime;
            var wallUtc = DateTime.UtcNow;

            var dCpu = (cpuTime - _lastCpuTime).TotalSeconds;
            var dWall = (wallUtc - _lastWallUtc).TotalSeconds;
            _lastCpuTime = cpuTime;
            _lastWallUtc = wallUtc;

            if (dWall <= 0 || dCpu < 0) return;
            var pct = dCpu / dWall / _logicalCpuCount * 100.0;
            if (pct < 0) pct = 0;

            lock (_lock) _samples.Add(pct);
        }
        catch (InvalidOperationException) { _process = null; }
        catch (ArgumentException) { _process = null; }
    }
}