using System.Diagnostics;
using System.Text.Json;
using LoadGenerator;
using Shared;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: LoadGenerator <config.json> [serverPid]");
    return 2;
}

BenchmarkConfig config;
try { config = ConfigLoader.Load(args[0]); }
catch (Exception e) when (e is IOException or InvalidDataException or JsonException)
{
    Console.Error.WriteLine($"Không đọc được config '{args[0]}': {e.Message}");
    return 2;
}

int? serverPid = null;
if (args.Length > 1)
{
    if (!int.TryParse(args[1], out int pid))
    {
        Console.Error.WriteLine("serverPid phải là số nguyên.");
        return 2;
    }
    try { using var _ = Process.GetProcessById(pid); }
    catch (ArgumentException)
    {
        Console.Error.WriteLine($"Không có process nào với PID {pid}.");
        return 2;
    }
    serverPid = pid;
}
else
{
    Console.WriteLine("[cảnh báo] Không có serverPid: sẽ KHÔNG đo CPU (cột cpu_percent_* = 0).");
}

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try
{
    var exporter = new CsvExporter(config.OutputDirectory);

    for (int r = 0; r < config.RepeatCount; r++)
        exporter.EnsureCanWrite(config.ExperimentName, r);

    Console.WriteLine($"Experiment={config.ExperimentName} clients={config.ClientCount} " +
                      $"warmup={config.WarmupSeconds}s measure={config.DurationSeconds}s " +
                      $"repeat={config.RepeatCount} -> {config.Host}:{config.Port}");

    var runner = new LoadRunner(config, serverPid);

    for (int r = 0; r < config.RepeatCount; r++)
    {
        Console.WriteLine($"Run {r + 1}/{config.RepeatCount} ...");
        RunOutput output = await runner.RunOnceAsync(r, cts.Token);
        BenchmarkResult m = output.Result;

        exporter.ExportRequests(config, r, output.Requests);
        exporter.ExportSummary(m);

        Console.WriteLine(
            $"  total={m.TotalRequests} ok={m.SuccessfulRequests} err={m.FailedRequests} " +
            $"avg={m.AvgLatencyMs:F3}ms p95={m.P95LatencyMs:F3}ms " +
            $"thr={m.ThroughputRps:F1}rps errRate={m.ErrorRate:P3} cpu={m.CpuPercentAvg:F1}%");

        if (r < config.RepeatCount - 1)
            await Task.Delay(TimeSpan.FromSeconds(2), cts.Token);   
    }

    Console.WriteLine($"Xong. Dữ liệu ở: {exporter.OutputDirectory}");
    return 0;
}
catch (OperationCanceledException)
{
    Console.WriteLine("Đã hủy.");
    return 130;
}
catch (Exception e) when (e is InvalidOperationException or IOException)
{
    Console.Error.WriteLine($"Lỗi: {e.Message}");
    return 1;
}
