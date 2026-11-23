internal static class MetricsTests
{
    public static Task<bool> Run()
    {
        // Chi kiem tra cong thuc bang so mau, chua kiem tra MetricsCollector cua nhom
        double[] latency = { 10, 20, 30, 40, 50 };
        double average = latency.Average();
        var sorted = latency.OrderBy(x => x).ToArray();
        int p95Index = (int)Math.Ceiling(sorted.Length * 0.95) - 1;
        double p95 = sorted[p95Index];

        int success = 90;
        int total = 100;
        double seconds = 3;
        double throughput = success / seconds;
        double errorRate = (total - success) * 100.0 / total;

        bool ok = average == 30 && p95 == 50 &&
                  throughput == 30 && errorRate == 10;
        return Task.FromResult(ok);
    }
}
