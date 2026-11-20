using System.Buffers.Binary;
namespace Shared;

public sealed record Message(long RequestId, byte[] Payload);

// Network byte order (big endian); length includes ID + payload, excludes prefix.
public static class Protocol
{
    public const int PayloadSize = 1024;
    public const int MaxPayloadSize = 1024;
    public const int IdSize = 8;
    public static async Task WriteAsync(Stream stream, Message message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message.Payload);
        if (message.Payload.Length != PayloadSize) throw new InvalidDataException("Invalid payload size");
        byte[] frame = new byte[4 + IdSize + message.Payload.Length];
        BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(0, 4), IdSize + message.Payload.Length);
        BinaryPrimitives.WriteInt64BigEndian(frame.AsSpan(4, IdSize), message.RequestId);
        message.Payload.CopyTo(frame, 4 + IdSize);
        await stream.WriteAsync(frame.AsMemory(), ct);
    }
    // null = clean EOF before next frame; partial frame = exception.
    public static async Task<Message?> ReadAsync(Stream stream, CancellationToken ct)
    {
        byte[] prefix = new byte[4];
        int first = await stream.ReadAsync(prefix.AsMemory(0, 4), ct);
        if (first == 0) return null;
        await stream.ReadExactlyAsync(prefix.AsMemory(first), ct);
        int length = BinaryPrimitives.ReadInt32BigEndian(prefix);
        if (length != IdSize + PayloadSize) throw new InvalidDataException("Invalid frame length");
        byte[] body = new byte[length];
        await stream.ReadExactlyAsync(body.AsMemory(), ct);
        return new Message(BinaryPrimitives.ReadInt64BigEndian(body.AsSpan(0, IdSize)), body[IdSize..]);
    }
    public static bool Matches(Message expected, Message? actual) => actual is not null &&
        expected.RequestId == actual.RequestId && expected.Payload.AsSpan().SequenceEqual(actual.Payload);
}
