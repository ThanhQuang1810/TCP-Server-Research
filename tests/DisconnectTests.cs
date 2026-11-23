using System.Buffers.Binary;
using System.Net.Sockets;
using Shared;

internal static class DisconnectTests
{
    public static async Task<bool> Run()
    {
        using (var client = await TestHelper.ConnectAsync())
        {
            byte[] prefix = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(prefix, 8 + Protocol.PayloadSize);
            await client.GetStream().WriteAsync(prefix);
            await client.GetStream().WriteAsync(new byte[15]);
            // Client dong khi Server dang cho du du lieu
            client.Client.LingerState = new LingerOption(true, 0);
        }

        await Task.Delay(200);
        return await TestHelper.EchoAsync(9001);
    }
}
