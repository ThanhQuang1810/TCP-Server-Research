namespace Shared;
public sealed record BenchmarkConfig
{
	public string ExperimentName { get; init; } = "clients-1";
	public string Host { get; init; } = "127.0.0.1";
	public int Port { get; init; } = 9000;
	public int ClientCount { get; init; } = 1;
	public int PayloadSizeBytes { get; init; } = Protocol.PayloadSize;
	public int WarmupSeconds { get; init; } = 5;
	public int DurationSeconds { get; init; } = 20;
	public int RepeatCount { get; init; } = 3;
	public int ConnectTimeoutMs { get; init; } = 5000;
	public int RequestTimeoutMs { get; init; } = 5000;
	public int CpuSampleIntervalMs { get; init; } = 1000;
	public bool TcpNoDelay { get; init; } = true;
	public bool ValidateResponse { get; init; } = true;
	public string OutputDirectory { get; init; } = "experiments/raw-data";
}
