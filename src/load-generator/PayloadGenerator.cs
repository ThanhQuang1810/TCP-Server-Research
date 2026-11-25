using Shared;

namespace LoadGenerator;

public static class PayloadGenerator
{
    public const int DefaultSeed = 20260101;

    public static byte[] Create(int size = Protocol.PayloadSize, int seed = DefaultSeed)
    {
        if (size < 1 || size > Protocol.MaxPayloadSize)
            throw new ArgumentOutOfRangeException(nameof(size), "Payload ngoài giới hạn của Protocol.");

        var data = new byte[size];
        new Random(seed).NextBytes(data);
        return data;
    }
}
