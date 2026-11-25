# TCP Server Research - run 1/10/50 clients x 3 repetitions
# Run from the repository root: .\experiments\run-experiments.ps1
# Or: powershell -ExecutionPolicy Bypass -File .\experiments\run-experiments.ps1
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [int]$StartupTimeoutSeconds = 15
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$serverProject = Join-Path $repoRoot 'src/server/Server.csproj'
$loadProject = Join-Path $repoRoot 'src/load-generator/LoadGenerator.csproj'
$serverDll = Join-Path $repoRoot "src/server/bin/$Configuration/net10.0/Server.dll"
$loadDll = Join-Path $repoRoot "src/load-generator/bin/$Configuration/net10.0/LoadGenerator.dll"
$sessionId = Get-Date -Format 'yyyyMMdd-HHmmss'
$sessionDir = Join-Path $repoRoot "experiments/raw-data/session-$sessionId"
$logsDir = Join-Path $sessionDir 'logs'
New-Item -ItemType Directory -Force -Path $logsDir | Out-Null

$serverProcess = $null
try {
    Write-Host '[1/4] Building Server and LoadGenerator...'
    & dotnet build $serverProject -c $Configuration --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Server build failed.' }
    & dotnet build $loadProject -c $Configuration --nologo
    if ($LASTEXITCODE -ne 0) { throw 'LoadGenerator build failed.' }

    if (-not (Test-Path $serverDll)) { throw "Server DLL not found: $serverDll" }
    if (-not (Test-Path $loadDll)) { throw "LoadGenerator DLL not found: $loadDll" }

    Write-Host '[2/4] Starting TCP Server in its own process...'
    $serverProcess = Start-Process -FilePath 'dotnet' `
        -ArgumentList @("`"$serverDll`"", '127.0.0.1', '9000') `
        -WorkingDirectory $repoRoot -PassThru -WindowStyle Hidden `
        -RedirectStandardOutput (Join-Path $logsDir 'server.stdout.log') `
        -RedirectStandardError (Join-Path $logsDir 'server.stderr.log')

    $ready = $false
    $deadline = (Get-Date).AddSeconds($StartupTimeoutSeconds)
    while ((Get-Date) -lt $deadline -and -not $ready) {
        if ($serverProcess.HasExited) { throw "Server exited early ($($serverProcess.ExitCode)). Check server.stderr.log." }
        $client = [System.Net.Sockets.TcpClient]::new()
        try {
            $async = $client.BeginConnect('127.0.0.1', 9000, $null, $null)
            if ($async.AsyncWaitHandle.WaitOne(400)) {
                try { $client.EndConnect($async); $ready = $true } catch { }
            }
        }
        finally { $client.Dispose() }
        if (-not $ready) { Start-Sleep -Milliseconds 300 }
    }
    if (-not $ready) { throw 'Server did not listen on 127.0.0.1:9000 in time.' }

    Write-Host "Server PID: $($serverProcess.Id)"
    Write-Host '[3/4] Running benchmark scenarios...'
    foreach ($clientCount in @(1, 10, 50)) {
        $original = Join-Path $repoRoot "experiments/configs/clients-$clientCount.json"
        if (-not (Test-Path $original)) { throw "Config not found: $original" }
        $cfg = Get-Content -LiteralPath $original -Raw | ConvertFrom-Json
        if ($cfg.ClientCount -ne $clientCount) { throw "Incorrect ClientCount in $original" }
        if ($cfg.RepeatCount -ne 3) { throw "Expected RepeatCount=3 in $original; got $($cfg.RepeatCount)" }
        $cfg.OutputDirectory = $sessionDir
        $configCopy = Join-Path $sessionDir "clients-$clientCount.json"
        [System.IO.File]::WriteAllText($configCopy, ($cfg | ConvertTo-Json -Depth 10), [System.Text.UTF8Encoding]::new($false))
        Write-Host "Running $clientCount clients x 3 repeats..."
        & dotnet $loadDll $configCopy $serverProcess.Id *>&1 | Tee-Object -FilePath (Join-Path $logsDir "clients-$clientCount.log")
        if ($LASTEXITCODE -ne 0) { throw "LoadGenerator failed for $clientCount clients with exit code $LASTEXITCODE" }
    }

    Write-Host '[4/4] Validating output...'
    $summaryPath = Join-Path $sessionDir 'summary.csv'
    if (-not (Test-Path $summaryPath)) { throw 'summary.csv was not generated.' }
    $rows = @(Import-Csv -LiteralPath $summaryPath)
    if ($rows.Count -ne 9) { throw "Expected 9 summary rows, found $($rows.Count)." }
    foreach ($n in @(1,10,50)) {
        if (@($rows | Where-Object { [int]$_.client_count -eq $n }).Count -ne 3) {
            throw "Expected exactly three summary rows for $n clients."
        }
    }
    Write-Host "SUCCESS: 9 experiment runs recorded in $summaryPath" -ForegroundColor Green
}
catch {
    Write-Error $_
    throw
}
finally {
    if ($null -ne $serverProcess) {
        $serverProcess.Refresh()
        if (-not $serverProcess.HasExited) {
            Stop-Process -Id $serverProcess.Id -Force -ErrorAction SilentlyContinue
            Write-Host "Stopped TCP Server PID $($serverProcess.Id)."
        }
        $serverProcess.Dispose()
    }
    Write-Host "Session output: $sessionDir"
}
