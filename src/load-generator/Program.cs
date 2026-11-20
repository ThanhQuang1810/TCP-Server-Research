using System.Net.Sockets;
using Shared;
// Smoke test ONLY. Member 3 replaces this with concurrent benchmark entry point.
using var client = new TcpClient { NoDelay = true };
using var ct = new CancellationTokenSource(TimeSpan.FromSeconds(5));
await client.ConnectAsync(args.Length > 0 ? args[0] : "127.0.0.1", args.Length > 1 ? int.Parse(args[1]) : 9000, ct.Token);
using var stream = client.GetStream();
var request = new Message(1, new byte[Protocol.PayloadSize]);
await Protocol.WriteAsync(stream, request, ct.Token);
var response = await Protocol.ReadAsync(stream, ct.Token);
bool ok = Protocol.Matches(request, response);
Console.WriteLine(ok ? "PASS: echo matches" : "FAIL: echo mismatch");
return ok ? 0 : 1;
