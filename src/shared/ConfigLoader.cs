using System.Text.Json;
using System.Text.Json.Serialization;
namespace Shared;
public static class ConfigLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };
    public static BenchmarkConfig Load(string path)
    {
        var config = JsonSerializer.Deserialize<BenchmarkConfig>(File.ReadAllText(path), Options)
            ?? throw new InvalidDataException("Empty configuration.");
        Validate(config);
        return config;
    }
    public static void Validate(BenchmarkConfig c)
    {
        ArgumentNullException.ThrowIfNull(c);
        if (string.IsNullOrWhiteSpace(c.ExperimentName) || c.ExperimentName.Any(ch => !char.IsAsciiLetterOrDigit(ch) && ch != '-' && ch != '_'))
            throw new InvalidDataException("Invalid experiment name.");
        if (string.IsNullOrWhiteSpace(c.Host) || c.Port is < 1 or > 65535)
            throw new InvalidDataException("Invalid host or port.");
        if (c.ClientCount is < 1 or > 100) throw new InvalidDataException("ClientCount must be 1..100.");
        if (c.PayloadSizeBytes != Protocol.PayloadSize) throw new InvalidDataException("Benchmark requires 1024-byte payload.");
        if (c.WarmupSeconds is < 0 or > 3600 || c.DurationSeconds is < 1 or > 3600 || c.RepeatCount is < 1 or > 100)
            throw new InvalidDataException("Invalid experiment duration or repetitions.");
        if (c.ConnectTimeoutMs is < 1 or > 600000 || c.RequestTimeoutMs is < 1 or > 600000 || c.CpuSampleIntervalMs is < 1 or > 600000)
            throw new InvalidDataException("Invalid timeout or sampling interval.");
        if (!c.ValidateResponse || string.IsNullOrWhiteSpace(c.OutputDirectory))
            throw new InvalidDataException("Response validation and output directory are required.");
    }
}
