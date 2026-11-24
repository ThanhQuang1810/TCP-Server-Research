using System.Net;
using System.Net.Sockets;
using Shared;

internal sealed class TcpServer
{
    private readonly TcpListener _listener;
    private readonly ConnectionManager _connections;
    private readonly ServerLogger _logger;
    private readonly int _timeoutMs;
    private readonly bool _tcpNoDelay;
    private readonly int _maxPayloadSize;

    public TcpServer(
        IPAddress ip,
        int port,
        int maxConnections,
        int timeoutMs,
        bool tcpNoDelay,
        ServerLogger logger,
        int maxPayloadSize = Protocol.MaxPayloadSize)
    {
        _listener = new TcpListener(ip, port);
        _connections = new ConnectionManager(maxConnections);
        _logger = logger;
        _timeoutMs = timeoutMs;
        _tcpNoDelay = tcpNoDelay;
        _maxPayloadSize = maxPayloadSize;
    }

    public async Task RunAsync(CancellationToken stop)
    {
        var tasks = new List<Task>();

        using var shutdown =
            CancellationTokenSource.CreateLinkedTokenSource(stop);

        _listener.Start();

        try
        {
            Console.WriteLine(
                $"Server PID={Environment.ProcessId} listening on {_listener.LocalEndpoint}");

            while (!shutdown.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(
                    shutdown.Token);

                if (!_connections.TryAddConnection())
                {
                    client.Dispose();
                    _logger.Warning("Connection limit reached.");
                    continue;
                }

                tasks.RemoveAll(task => task.IsCompleted);

                tasks.Add(HandleClientAsync(
                    client, shutdown.Token));
            }
        }
        catch (OperationCanceledException)
            when (shutdown.IsCancellationRequested)
        {
        }
        finally
        {
            _listener.Stop();
            shutdown.Cancel();

            await Task.WhenAll(tasks);

            _logger.Info("Server stopped.");
        }
    }

    private async Task HandleClientAsync(
        TcpClient client,
        CancellationToken stop)
    {
        string endpoint = "Unknown";

        try
        {
            endpoint = client.Client.RemoteEndPoint?.ToString()
                ?? "Unknown";

            _logger.ClientConnected(
                endpoint,
                _connections.CurrentConnections);

            await ClientHandler.RunAsync(
                client,
                stop,
                _timeoutMs,
                _tcpNoDelay,
                _logger,
                _maxPayloadSize);
        }
        catch (Exception ex)
        {
            _logger.Error(
                $"Client {endpoint}: {ex.Message}");
        }
        finally
        {
            client.Dispose();

            _connections.RemoveConnection();

            _logger.ClientDisconnected(
                endpoint,
                _connections.CurrentConnections);
        }
    }
}
