namespace Shared;
public sealed class ProtocolException : IOException
{
	public ProtocolException(string message) : base(message) { }
	public ProtocolException(string message, Exception inner) : base(message, inner) { }
}
