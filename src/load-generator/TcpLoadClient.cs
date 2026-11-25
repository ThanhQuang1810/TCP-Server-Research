using System.Diagnostics;
using System.Net.Sockets;
using Shared;

namespace LoadGenerator;
public sealed class TcpLoadClient : IDisposable
{
    private readonly int _id;
    private readonly BenchmarkConfig _config;
    private readonly byte[] _payload;
    private readonly MetricsCollector _collector;

    private TcpClient? _tcp;
    private NetworkStream? _stream;
    private long _seq;

    public TcpLoadClient(int id, BenchmarkConfig config, byte[] payload, MetricsCollector collector)
    {
        _id = id;
        _config = config;
        _payload = payload;
        _collector = collector;
    }
    public async Task<string?> TryConnectAsync(CancellationToken ct)
    {
        CloseConnection();
        var tcp = new TcpClient { NoDelay = _config.TcpNoDelay };
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(_config.ConnectTimeoutMs);
            await tcp.ConnectAsync(_config.Host, _config.Port, cts.Token);
            _tcp = tcp;
            _stream = tcp.GetStream();
            return null;
        }
        catch (Exception e) when (e is SocketException or IOException or OperationCanceledException)
        {
            tcp.Dispose();
            return e.Message;
        }
    }
    public async Task RunAsync(Task startGate, CancellationToken stop)
    {
        try
        {
            await startGate.WaitAsync(stop);

            while (!stop.IsCancellationRequested)
            {
                if (_stream is null)
                {
                    string? connectError = await TryConnectAsync(stop);
                    if (connectError is not null)
                    {
                        if (stop.IsCancellationRequested) break;
                        _collector.Record(RequestResult.Failure(
                            0, _id, 0, ErrorTypes.ConnectFailed, connectError));
                        await Task.Delay(100, stop);   
                        continue;
                    }
                }

                RequestResult? result = await SendOneAsync(stop);
                if (result is null) break;            

                _collector.Record(result);

                if (!result.IsSuccess) CloseConnection();
            }
        }
        catch (OperationCanceledException) { /* kết thúc bình thường */ }
        finally { CloseConnection(); }
    }

    private async Task<RequestResult?> SendOneAsync(CancellationToken stop)
    {

        long requestId = ((long)_id << 32) | (uint)++_seq;
        var request = new RequestMessage(requestId, _payload);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stop);
        cts.CancelAfter(_config.RequestTimeoutMs);

        string? error;
        string? message = null;
        long t0 = Stopwatch.GetTimestamp();
        try
        {
            await Protocol.WriteRequestAsync(_stream!, request, cts.Token);
            ResponseMessage? response = await Protocol.ReadResponseAsync(_stream!, cts.Token);
            error = _config.ValidateResponse ? ResponseValidator.Validate(request, response)
                                             : (response is null ? ErrorTypes.Disconnected : null);
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
            return null;
        }
        catch (OperationCanceledException)
        {
            error = ErrorTypes.Timeout;
            message = $"No response within {_config.RequestTimeoutMs} ms";
        }
        catch (ProtocolException e)             
        {
            error = ErrorTypes.InvalidFrame;
            message = e.Message;
        }
        catch (Exception e) when (e is IOException or SocketException or ObjectDisposedException)
        {
            error = ErrorTypes.Disconnected;
            message = e.Message;
        }
        double latencyMs = Stopwatch.GetElapsedTime(t0).TotalMilliseconds;

        return error is null
            ? RequestResult.Success(requestId, _id, latencyMs)
            : RequestResult.Failure(requestId, _id, latencyMs, error, message);
    }

    private void CloseConnection()
    {
        _stream?.Dispose();
        _tcp?.Dispose();
        _stream = null;
        _tcp = null;
    }

    public void Dispose() => CloseConnection();
}
