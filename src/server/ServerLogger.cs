using System;

internal sealed class ServerLogger
{
    private readonly object _lock = new();
    private readonly bool _logRequests;

    public ServerLogger(bool logRequests = false)
    {
        _logRequests = logRequests;
    }

    public void Info(string message)
    {
        Write("INFO", message);
    }

    public void Warning(string message)
    {
        Write("WARNING", message);
    }

    public void Error(string message)
    {
        Write("ERROR", message);
    }

    public void ClientConnected(string endpoint, int currentConnections)
    {
        Info($"Client connected: {endpoint} | Connections: {currentConnections}");
    }

    public void ClientDisconnected(string endpoint, int currentConnections)
    {
        Info($"Client disconnected: {endpoint} | Connections: {currentConnections}");
    }

    public void RequestReceived(long requestId, int payloadSize)
    {
        if (_logRequests)
        {
            Info($"Request ID: {requestId} | Payload: {payloadSize} bytes");
        }
    }

    private void Write(string level, string message)
    {
        lock (_lock)
        {
            string log = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";

            if (level == "ERROR")
                Console.Error.WriteLine(log);
            else
                Console.WriteLine(log);
        }
    }
}
