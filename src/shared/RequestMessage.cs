namespace Shared;

public sealed record RequestMessage(long RequestId, byte[] Payload);
