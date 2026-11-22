using System.Net.Sockets;
using Shared;

internal static class TestHelper
{
    public static string Host = "127.0.0.1";
    public static int Port = 9000;

    public static async Task<TcpClient> ConnectAsync(int timeoutMs = 7000)
    {
        var client = new TcpClient { NoDelay = true };
        using var timeout = new CancellationTokenSource(timeoutMs);
        try
        {
            await client.ConnectAsync(Host, Port, timeout.Token);
            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    public static byte[] MakePayload(int seed)
    {
        byte[] data = new byte[Protocol.PayloadSize];
        for (int i = 0; i < data.Length; i++)
            data[i] = (byte)((i + seed) % 256);
        return data;
    }

    public static async Task<bool> EchoAsync(long id)
    {
        using var client = await ConnectAsync();
        using var timeout = new CancellationTokenSource(7000);
        using var stream = client.GetStream();
        var request = new Message(id, MakePayload((int)(id % 100)));
        await Protocol.WriteAsync(stream, request, timeout.Token);
        var response = await Protocol.ReadAsync(stream, timeout.Token);
        return Protocol.Matches(request, response);
    }

    public static async Task<bool> ServerClosedAsync(TcpClient client, int waitMs = 7000)
    {
        using var timeout = new CancellationTokenSource(waitMs);
        try
        {
            byte[] buffer = new byte[1];
            int count = await client.GetStream().ReadAsync(buffer.AsMemory(), timeout.Token);
            return count == 0;
        }
        catch (IOException) { return true; }
        catch (SocketException) { return true; }
        catch (OperationCanceledException) { return false; }
    }
}
