using System.Net.Sockets;
using Shared;

internal static class ClientHandler
{
    public static async Task RunAsync(
        TcpClient client,
        CancellationToken stop,
        int timeoutMs = 5000,
        bool tcpNoDelay = true,
        ServerLogger? logger = null,
        int maxPayloadSize = Protocol.MaxPayloadSize)
    {
        logger ??= new ServerLogger();

        using (client)
        {
            try
            {
                client.NoDelay = tcpNoDelay;

                using var stream = client.GetStream();

                while (!stop.IsCancellationRequested)
                {
                    using var timeout =
                        CancellationTokenSource
                            .CreateLinkedTokenSource(stop);

                    timeout.CancelAfter(timeoutMs);

                    var request = await Protocol.ReadRequestAsync(
                        stream, timeout.Token);

                    if (request is null)
                        break;

                    if (request.Payload.Length > maxPayloadSize)
                        throw new ProtocolException(
                            "Payload exceeds allowed size.");

                    logger.RequestReceived(
                        request.RequestId,
                        request.Payload.Length);

                    var response = new ResponseMessage(
                        request.RequestId,
                        request.Payload);

                    await Protocol.WriteResponseAsync(
                        stream, response, timeout.Token);
                }
            }
            catch (OperationCanceledException)
                when (stop.IsCancellationRequested)
            {
            }
            catch (OperationCanceledException)
            {
                logger.Warning("Client request timeout.");
            }
            catch (ProtocolException ex)
            {
                logger.Warning($"Protocol error: {ex.Message}");
            }
            catch (IOException ex)
            {
                logger.Error($"IO error: {ex.Message}");
            }
            catch (SocketException ex)
            {
                logger.Error($"Socket error: {ex.Message}");
            }
        }
    }
}
