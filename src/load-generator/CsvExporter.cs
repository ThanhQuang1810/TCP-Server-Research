using System.Globalization;
using System.Text;
using Shared;

namespace LoadGenerator;

public sealed class CsvExporter
{
    private const string SummaryHeader =
        "experiment_name,host,port,client_count,payload_size_bytes," +
        "warmup_seconds,duration_seconds,repeat_index,measurement_seconds," +
        "total_requests,successful_requests,failed_requests," +
        "avg_latency_ms,p95_latency_ms,throughput_rps,error_rate," +
        "cpu_percent_avg,cpu_percent_max,cpu_sample_count," +
        "started_utc,ended_utc";

    private const string RequestHeader =
        "timestamp_utc,request_id,client_id,latency_ms,success,error_type,error_message";

    private readonly string _outputDirectory;

    public CsvExporter(string outputDirectory)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("OutputDirectory rỗng.", nameof(outputDirectory));
        _outputDirectory = ResolveAndCreate(outputDirectory);
    }

    public string OutputDirectory => _outputDirectory;

    public void EnsureCanWrite(string experimentName, int repeatIndex)
    {
        if (string.IsNullOrWhiteSpace(experimentName))
            throw new ArgumentException("ExperimentName rỗng.", nameof(experimentName));
        if (experimentName.Contains(',') || experimentName.Contains('"'))
            throw new ArgumentException(
                "ExperimentName không được chứa ',' hoặc '\"' để giữ CSV hợp lệ.",
                nameof(experimentName));
        if (repeatIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(repeatIndex), "RepeatIndex phải >= 0 (0-based).");

        var path = Path.Combine(_outputDirectory, "summary.csv");
        if (File.Exists(path) && new FileInfo(path).Length > 0)
        {
            EnsureNoDuplicateRun(path, experimentName, repeatIndex);
        }
    }

    public string ExportRequests(
        BenchmarkConfig config, int repeatIndex, IEnumerable<RequestResult> results)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (results == null) throw new ArgumentNullException(nameof(results));

        var runId = BuildRunId(config.ExperimentName, repeatIndex);
        var path = Path.Combine(_outputDirectory, $"{runId}-requests.csv");

        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));

        writer.WriteLine(RequestHeader);
        foreach (var r in results)
        {
            writer.Write(FormatIso(r.TimestampUtc)); writer.Write(',');
            writer.Write(r.RequestId.ToString(CultureInfo.InvariantCulture)); writer.Write(',');
            writer.Write(r.ClientId.ToString(CultureInfo.InvariantCulture)); writer.Write(',');
            writer.Write(r.LatencyMs.ToString("F3", CultureInfo.InvariantCulture)); writer.Write(',');
            writer.Write(r.IsSuccess ? "true" : "false"); writer.Write(',');
            writer.Write(Escape(r.ErrorType)); writer.Write(',');
            writer.Write(Escape(r.ErrorMessage));
            writer.WriteLine();
        }
        writer.Flush();
        return path;
    }


    public string ExportSummary(BenchmarkResult result)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));

        var path = Path.Combine(_outputDirectory, "summary.csv");

        var needHeader = !File.Exists(path) || new FileInfo(path).Length == 0;

        if (!needHeader)
            EnsureNoDuplicateRun(path, result.Config.ExperimentName, result.RepeatIndex);

        var sb = new StringBuilder();
        sb.Append(Escape(result.Config.ExperimentName)).Append(',');
        sb.Append(Escape(result.Config.Host)).Append(',');
        sb.Append(result.Config.Port.ToString(CultureInfo.InvariantCulture)).Append(',');
        sb.Append(result.Config.ClientCount.ToString(CultureInfo.InvariantCulture)).Append(',');
        sb.Append(result.Config.PayloadSizeBytes.ToString(CultureInfo.InvariantCulture)).Append(',');
        sb.Append(result.Config.WarmupSeconds.ToString(CultureInfo.InvariantCulture)).Append(',');
        sb.Append(result.Config.DurationSeconds.ToString(CultureInfo.InvariantCulture)).Append(',');
        sb.Append(result.RepeatIndex.ToString(CultureInfo.InvariantCulture)).Append(',');
        sb.Append(result.MeasurementSeconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',');
        sb.Append(result.TotalRequests.ToString(CultureInfo.InvariantCulture)).Append(',');
        sb.Append(result.SuccessfulRequests.ToString(CultureInfo.InvariantCulture)).Append(',');
        sb.Append(result.FailedRequests.ToString(CultureInfo.InvariantCulture)).Append(',');
        sb.Append(result.AvgLatencyMs.ToString("F3", CultureInfo.InvariantCulture)).Append(',');
        sb.Append(result.P95LatencyMs.ToString("F3", CultureInfo.InvariantCulture)).Append(',');
        sb.Append(result.ThroughputRps.ToString("F2", CultureInfo.InvariantCulture)).Append(',');
        sb.Append(result.ErrorRate.ToString("F6", CultureInfo.InvariantCulture)).Append(',');
        sb.Append(result.CpuPercentAvg.ToString("F2", CultureInfo.InvariantCulture)).Append(',');
        sb.Append(result.CpuPercentMax.ToString("F2", CultureInfo.InvariantCulture)).Append(',');
        sb.Append(result.CpuSampleCount.ToString(CultureInfo.InvariantCulture)).Append(',');
        sb.Append(FormatIso(result.StartedUtc)).Append(',');
        sb.Append(FormatIso(result.EndedUtc));

        if (needHeader)
            File.WriteAllText(path, SummaryHeader + Environment.NewLine + sb + Environment.NewLine,
                new UTF8Encoding(false));
        else
            File.AppendAllText(path, sb + Environment.NewLine, new UTF8Encoding(false));

        return path;
    }

    private static string BuildRunId(string experimentName, int repeatIndex)
    {
        var safe = experimentName;
        foreach (var c in Path.GetInvalidFileNameChars())
            safe = safe.Replace(c, '_');
        var ts = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
        return $"{safe}-r{repeatIndex}-{ts}";
    }

    private static string FormatIso(DateTime utc) =>
        utc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);


    private static void EnsureNoDuplicateRun(string summaryPath, string experimentName, int repeatIndex)
    {
        var targetRepeat = repeatIndex.ToString(CultureInfo.InvariantCulture);

        using var stream = new FileStream(summaryPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        _ = reader.ReadLine();
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.Length == 0) continue;
            var parts = SplitCsvLine(line);

            if (parts.Count >= 8 && parts[0] == experimentName && parts[7] == targetRepeat)
            {
                throw new IOException(
                    $"Đã tồn tại lượt '{experimentName}' repeat={repeatIndex} trong {summaryPath}. " +
                    "Để không ghi đè hãy xoá summary.csv hoặc dùng RepeatIndex khác.");
            }
        }
    }

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuote = false;
        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuote)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQuote = false;
                }
                else sb.Append(c);
            }
            else
            {
                if (c == '"') inQuote = true;
                else if (c == ',') { result.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
        }
        result.Add(sb.ToString());
        return result;
    }

    private static string ResolveAndCreate(string outputDirectory)
    {
        var resolved = Path.IsPathRooted(outputDirectory)
            ? outputDirectory
            : ResolveRelativeToRepoRoot(outputDirectory);
        Directory.CreateDirectory(resolved);
        return resolved;
    }

    private static string ResolveRelativeToRepoRoot(string relativePath)
    {
        var current = new DirectoryInfo(Environment.CurrentDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "TCPResearch.slnx")))
                return Path.GetFullPath(Path.Combine(current.FullName, relativePath));
            current = current.Parent;
        }
        return Path.GetFullPath(relativePath);
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var needQuote = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
        return needQuote ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
    }
    public static void ValidateConfig(BenchmarkConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (config.ClientCount <= 0)
            throw new ArgumentException("ClientCount phải > 0.", nameof(config));
        if (config.DurationSeconds <= 0)
            throw new ArgumentException("DurationSeconds phải > 0.", nameof(config));
        if (config.WarmupSeconds < 0)
            throw new ArgumentException("WarmupSeconds phải >= 0.", nameof(config));
        if (string.IsNullOrWhiteSpace(config.ExperimentName))
            throw new ArgumentException("ExperimentName rỗng.", nameof(config));
    }
}