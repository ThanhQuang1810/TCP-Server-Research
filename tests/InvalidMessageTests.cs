using System.Buffers.Binary;
using Shared;

internal static class InvalidMessageTests
{
    public static async Task<bool> Run()
    {
        if (!await SendWrongLength(8)) return false; // Chi co requestId, thieu payload
        if (!await SendWrongLength(Protocol.IdSize + Protocol.MaxPayloadSize + 1))
            return false;

        try
        {
            using var memory = new MemoryStream();
            await Protocol.WriteAsync(memory,
                new Message(1, new byte[Protocol.MaxPayloadSize + 1]));
            return false;
        }
        catch (ProtocolException) { }

        // Du lieu sai khong duoc lam Server ngung hoat dong
        return await TestHelper.EchoAsync(7001);
    }

    private static async Task<bool> SendWrongLength(int length)
    {
        using var client = await TestHelper.ConnectAsync();
        byte[] prefix = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(prefix, length);
        await client.GetStream().WriteAsync(prefix);
        return await TestHelper.ServerClosedAsync(client);
    }
}
