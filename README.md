# TCP Server Research — starter
.NET 10 SDK. Run locally on 127.0.0.1 only for this experiment.

## Smoke test
Open PowerShell in repository root:
```powershell
dotnet build TCPResearch.slnx
# Terminal 1
dotnet run --project src/server -- 127.0.0.1 9000
# Terminal 2
dotnet run --project src/load-generator -- 127.0.0.1 9000
```
Expected: PASS: echo matches. The current generator only tests ONE request; no benchmark results yet. The starter has not been compiled in the authoring environment (no .NET SDK installed).

## Protocol
4-byte signed big-endian length (1032), 8-byte signed big-endian request ID, 1024-byte payload. Echo same frame. Reject other lengths before allocation. Each logical client has a separate TCP connection. ReadExactlyAsync handles partial reads. One outstanding request per logical client for RTT measurements.

## Team contract
Member 3 calls Shared.Protocol.WriteAsync / ReadAsync / Matches. Member 4 agrees with member 3 on metric interface before implementation. Request records should include run_id, clients, client_id, request_id, status, latency_ms; run summary should include attempts, successes, errors, elapsed_seconds, avg_latency_ms, p95_latency_ms, throughput_req_s, error_rate_pct, server_cpu_pct. Count measurement-phase attempts only; exclude warm-up and keep a consistent boundary rule. Measure CPU of server PID only, normalized by logical processor count. Config JSON files are templates; parsing is TODO. Do not modify protocol without group agreement.

## Remaining work
Concurrent runner, metrics, CSV, CPU, test automation, experimental data, analysis, report and slides. Run 1/10/50 clients three times each with 5s warm-up and 20s measurement. Keep conditions fixed. Cite course material and official TCP documents; disclose AI assistance. See docs/PhanCong.md.
