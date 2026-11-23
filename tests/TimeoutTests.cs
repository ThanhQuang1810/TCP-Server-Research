using System.Buffers.Binary;
using Shared;

internal static class TimeoutTests
{
    public static async Task<bool> Run()
    {
        // Server hien dang dat timeout 5 giay trong ClientHandler.cs
        using (var client = await TestHelper.ConnectAsync())
        {
            if (!await TestHelper.ServerClosedAsync(client, 8000)) return false;
        }

        using (var client = await TestHelper.ConnectAsync())
        {
            byte[] prefix = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(prefix, 8 + Protocol.PayloadSize);
            await client.GetStream().WriteAsync(prefix);
            await client.GetStream().WriteAsync(new byte[12]); // Gui chua du frame
            if (!await TestHelper.ServerClosedAsync(client, 8000)) return false;
        }

        return await TestHelper.EchoAsync(8001);
    }
}
