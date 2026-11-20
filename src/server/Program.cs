using System.Net;
using System.Net.Sockets;
using Shared;
// Starter echo server. Member 2 owns this file and final error handling.
var ip = IPAddress.Parse(args.Length > 0 ? args[0] : "127.0.0.1");
var port = args.Length > 1 ? int.Parse(args[1]) : 9000;
using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
var listener = new TcpListener(ip, port);
listener.Start();
Console.WriteLine($"Server PID={Environment.ProcessId} listening on {ip}:{port}");
try {
    while (!stop.IsCancellationRequested) {
        var client = await listener.AcceptTcpClientAsync(stop.Token);
        _ = ClientHandler.RunAsync(client, stop.Token);
    }
} catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
finally { listener.Stop(); }
