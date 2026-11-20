namespace Shared;

public sealed record ResponseMessage(long RequestId, byte[] Payload);
