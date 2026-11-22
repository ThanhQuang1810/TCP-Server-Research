using Shared;

internal static class EchoTests
{
    public static async Task<bool> Run()
    {
        using var client = await TestHelper.ConnectAsync();
        using var timeout = new CancellationTokenSource(7000);
        using var stream = client.GetStream();

        for (int i = 1; i <= 3; i++)
        {
            var request = new RequestMessage(6000L + i, TestHelper.MakePayload(i));
            await Protocol.WriteRequestAsync(stream, request, timeout.Token);
            var response = await Protocol.ReadResponseAsync(stream, timeout.Token);
            if (!Protocol.Matches(request, response)) return false;
        }

        var original = new RequestMessage(1, TestHelper.MakePayload(8));
        var wrongId = new ResponseMessage(2, TestHelper.MakePayload(8));
        var wrongPayload = new ResponseMessage(1, TestHelper.MakePayload(9));
        return !Protocol.Matches(original, wrongId) &&
               !Protocol.Matches(original, wrongPayload);
    }
}