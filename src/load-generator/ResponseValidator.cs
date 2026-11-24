using Shared;

namespace LoadGenerator;
public static class ErrorTypes
{
    public const string Timeout = "Timeout";
    public const string Disconnected = "Disconnected";
    public const string InvalidFrame = "InvalidFrame";
    public const string IdMismatch = "IdMismatch";
    public const string LengthMismatch = "LengthMismatch";
    public const string PayloadMismatch = "PayloadMismatch";
    public const string ConnectFailed = "ConnectFailed";
}

public static class ResponseValidator
{
  
    public static string? Validate(RequestMessage request, ResponseMessage? response)
    {
        if (response is null) return ErrorTypes.Disconnected;
        if (response.RequestId != request.RequestId) return ErrorTypes.IdMismatch;
        if (response.Payload.Length != request.Payload.Length) return ErrorTypes.LengthMismatch;
        if (!request.Payload.AsSpan().SequenceEqual(response.Payload)) return ErrorTypes.PayloadMismatch;
        return null;
    }
}
