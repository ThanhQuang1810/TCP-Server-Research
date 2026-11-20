using System.Net.Sockets;
using Shared;
internal static class ClientHandler
{
    public static async Task RunAsync(TcpClient client, CancellationToken stop)
    {
        using (client)
        try {
            client.NoDelay = true;
            using var stream = client.GetStream();
            while (!stop.IsCancellationRequested) {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stop);
                timeout.CancelAfter(TimeSpan.FromSeconds(5));
                var message = await Protocol.ReadAsync(stream, timeout.Token);
                if (message is null) break;
                await Protocol.WriteAsync(stream, message, timeout.Token);
            }
        } catch (Exception e) when (e is IOException or SocketException or OperationCanceledException) {
            Console.Error.WriteLine(e.GetType().Name);
        }
    }
}
