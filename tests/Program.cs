using System.Text;

Console.OutputEncoding = Encoding.UTF8;
TestHelper.Host = args.Length > 0 ? args[0] : "127.0.0.1";
TestHelper.Port = args.Length > 1 && int.TryParse(args[1], out int port) ? port : 9000;

Console.WriteLine("KIEM THU TCP SERVER");
Console.WriteLine($"Server: {TestHelper.Host}:{TestHelper.Port}");
Console.WriteLine("Hay khoi dong Server truoc khi chay cac test mang.\n");

int passed = 0;
int failed = 0;
int skipped = 0;

async Task Check(string name, Func<Task<bool>> test)
{
    try
    {
        bool ok = await test();
        Console.WriteLine((ok ? "[PASS] " : "[FAIL] ") + name);
        if (ok) passed++;
        else failed++;
    }
    catch (Exception ex)
    {
        Console.WriteLine("[FAIL] " + name + " - " + ex.Message);
        failed++;
    }
}

await Check("ProtocolTests", ProtocolTests.Run);
await Check("MetricsTests (cong thuc du lieu mau)", MetricsTests.Run);

string file = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
    "../../../../src/load-generator/MetricsCollector.cs"));
if (File.Exists(file) && File.ReadAllText(file).Contains("TODO"))
{
    Console.WriteLine("[SKIP] MetricsCollector thuc te: file nhom chua code xong.");
    skipped++;
}

try
{
    using var checkClient = await TestHelper.ConnectAsync();
}
catch (Exception ex)
{
    Console.WriteLine("Khong ket noi duoc Server: " + ex.Message);
    Console.WriteLine("Chay: dotnet run --project src/server/Server.csproj");
    Console.WriteLine($"Tong ket: {passed} PASS, {failed} FAIL, {skipped} SKIP; test mang CHUA CHAY.");
    return 1;
}

await Check("ConnectionTests (1, 10, 50 clients)", ConnectionTests.Run);
await Check("EchoTests (requestId va payload)", EchoTests.Run);
await Check("InvalidMessageTests", InvalidMessageTests.Run);
await Check("TimeoutTests (khoang 10 giay)", TimeoutTests.Run);
await Check("DisconnectTests", DisconnectTests.Run);

Console.WriteLine($"\nTong ket: {passed} PASS, {failed} FAIL, {skipped} SKIP");
return failed == 0 ? 0 : 1;
