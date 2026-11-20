using System.Buffers.Binary;
namespace Shared;

// Legacy starter API retained for the existing smoke test.
public sealed record Message(long RequestId, byte[] Payload);
public static class Protocol
{
    public const int PayloadSize = 1024;
    public const int MaxPayloadSize = 65536;
    public const int IdSize = 8;
    public const int PrefixSize = 4;

    public static async Task WriteAsync(Stream stream, Message message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(message.Payload);
        if (message.Payload.Length is < 1 or > MaxPayloadSize)
            throw new ProtocolException("Payload length outside allowed range.");
        byte[] frame = new byte[PrefixSize + IdSize + message.Payload.Length];
        BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(0, PrefixSize), IdSize + message.Payload.Length);
        BinaryPrimitives.WriteInt64BigEndian(frame.AsSpan(PrefixSize, IdSize), message.RequestId);
        message.Payload.CopyTo(frame, PrefixSize + IdSize);
        await stream.WriteAsync(frame.AsMemory(), ct);
    }

    public static async Task<Message?> ReadAsync(Stream stream, CancellationToken ct = default)
    {
        byte[] prefix = new byte[PrefixSize];
        int first = await stream.ReadAsync(prefix.AsMemory(), ct);
        if (first == 0) return null;
        try
        {
            await stream.ReadExactlyAsync(prefix.AsMemory(first), ct);
            int length = BinaryPrimitives.ReadInt32BigEndian(prefix);
            if (length < IdSize + 1 || length > IdSize + MaxPayloadSize)
                throw new ProtocolException("Invalid frame length.");
            byte[] body = new byte[length];
            await stream.ReadExactlyAsync(body.AsMemory(), ct);
            return new Message(BinaryPrimitives.ReadInt64BigEndian(body.AsSpan(0, IdSize)), body[IdSize..]);
        }
        catch (EndOfStreamException ex) { throw new ProtocolException("Incomplete frame.", ex); }
    }
    public static Task WriteRequestAsync(Stream stream, RequestMessage request, CancellationToken ct = default)
        => WriteAsync(stream, new Message(request.RequestId, request.Payload), ct);
    public static Task WriteResponseAsync(Stream stream, ResponseMessage response, CancellationToken ct = default)
        => WriteAsync(stream, new Message(response.RequestId, response.Payload), ct);
    public static async Task<RequestMessage?> ReadRequestAsync(Stream stream, CancellationToken ct = default)
    {
        var m = await ReadAsync(stream, ct);
        return m is null ? null : new RequestMessage(m.RequestId, m.Payload);
    }
    public static async Task<ResponseMessage?> ReadResponseAsync(Stream stream, CancellationToken ct = default)
    {
        var m = await ReadAsync(stream, ct);
        return m is null ? null : new ResponseMessage(m.RequestId, m.Payload);
    }
    public static bool Matches(Message expected, Message? actual) => actual is not null &&
        expected.RequestId == actual.RequestId && expected.Payload.AsSpan().SequenceEqual(actual.Payload);
    public static bool Matches(RequestMessage expected, ResponseMessage? actual) => actual is not null &&
        expected.RequestId == actual.RequestId && expected.Payload.AsSpan().SequenceEqual(actual.Payload);
}
