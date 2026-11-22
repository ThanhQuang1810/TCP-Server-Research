using System.Buffers.Binary;
using Shared;

internal static class ProtocolTests
{
    public static async Task<bool> Run()
    {
        var first = new Message(123456789L, TestHelper.MakePayload(2));
        var second = new Message(987654321L, TestHelper.MakePayload(3));
        using var stream = new MemoryStream();
        await Protocol.WriteAsync(stream, first);
        await Protocol.WriteAsync(stream, second);
        byte[] bytes = stream.ToArray();

        int length = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(0, 4));
        long id = BinaryPrimitives.ReadInt64BigEndian(bytes.AsSpan(4, 8));
        if (length != 8 + Protocol.PayloadSize || id != first.RequestId) return false;

        stream.Position = 0;
        var a = await Protocol.ReadAsync(stream);
        var b = await Protocol.ReadAsync(stream);
        var end = await Protocol.ReadAsync(stream);
        if (!Protocol.Matches(first, a) || !Protocol.Matches(second, b) || end != null)
            return false;

        using var small = new SmallReadStream(bytes);
        var splitMessage = await Protocol.ReadAsync(small);
        if (!Protocol.Matches(first, splitMessage)) return false;

        try
        {
            using var broken = new MemoryStream(new byte[] { 0, 0 });
            await Protocol.ReadAsync(broken);
            return false;
        }
        catch (ProtocolException) { return true; }
    }

    // Gia lap viec TCP tra ve 1 byte moi lan doc
    private class SmallReadStream : MemoryStream
    {
        public SmallReadStream(byte[] data) : base(data) { }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            return base.ReadAsync(buffer.Slice(0, Math.Min(1, buffer.Length)), cancellationToken);
        }
    }
}
