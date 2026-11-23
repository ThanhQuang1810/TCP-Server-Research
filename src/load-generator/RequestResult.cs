namespace LoadGenerator;

public sealed record RequestResult(
    long RequestId,
    int ClientId,
    double LatencyMs,
    bool IsSuccess,
    string? ErrorType,
    string? ErrorMessage,
    DateTime TimestampUtc)
{

    public static RequestResult Success(long requestId, int clientId, double latencyMs) =>
        new(requestId, clientId, latencyMs, IsSuccess: true, null, null, DateTime.UtcNow);

    public static RequestResult Failure(
        long requestId, int clientId, double latencyMs,
        string errorType, string? errorMessage = null) =>
        new(requestId, clientId, latencyMs, IsSuccess: false, errorType, errorMessage, DateTime.UtcNow);
}