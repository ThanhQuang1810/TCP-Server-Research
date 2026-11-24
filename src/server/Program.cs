using System.Net;
using System.Text.Json;
using Shared;

try
{
    string configPath = Path.Combine(
        AppContext.BaseDirectory, "appsettings.json");

    var settings = JsonSerializer.Deserialize<ServerSettings>(
        File.ReadAllText(configPath))
        ?? throw new InvalidDataException("Invalid configuration.");

    string host = args.Length > 0 ? args[0] : settings.Host;
    int port = args.Length > 1 ? int.Parse(args[1]) : settings.Port;

    if (port is < 1 or > 65535 ||
        settings.MaxConnections <= 0 ||
        settings.MaxPayloadSize is < 1 or > Protocol.MaxPayloadSize ||
        settings.RequestTimeoutMs <= 0)
    {
        throw new InvalidDataException("Invalid server settings.");
    }

    var ip = IPAddress.Parse(host);

    using var stop = new CancellationTokenSource();

    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        stop.Cancel();
    };

    var logger = new ServerLogger(settings.LogRequests);

    var server = new TcpServer(
        ip,
        port,
        settings.MaxConnections,
        settings.RequestTimeoutMs,
        settings.TcpNoDelay,
        logger,
        settings.MaxPayloadSize);

    await server.RunAsync(stop.Token);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Server error: {ex.Message}");
    Environment.ExitCode = 1;
}

internal sealed class ServerSettings
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 9000;
    public int MaxConnections { get; set; } = 100;
    public int MaxPayloadSize { get; set; } = 65536;
    public int RequestTimeoutMs { get; set; } = 5000;
    public bool TcpNoDelay { get; set; } = true;
    public bool LogRequests { get; set; } = false;
}
