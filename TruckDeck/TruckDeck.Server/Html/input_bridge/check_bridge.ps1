# Quick input-bridge diagnostic (run in PowerShell).
$ErrorActionPreference = 'Continue'
$port = 25556
$telemetryPort = 25555

Write-Host "=== TruckDeck Input Bridge check ===" -ForegroundColor Cyan

$td = Get-Process TruckDeck -ErrorAction SilentlyContinue
$py = Get-CimInstance Win32_Process -Filter "Name='python.exe'" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -match 'bridge\.py' }
Write-Host "TruckDeck.exe : $(if ($td) { "running (PID $($td.Id))" } else { 'not running' })"
Write-Host "Python bridge : $(if ($py) { "running (PID $($py.ProcessId))" } else { 'not running' })"

Write-Host "`nURL ACL:"
netsh http show urlacl | Select-String "25555|25556"

Write-Host "`nPort listeners:"
netstat -ano | Select-String ":$port\s|:$telemetryPort\s"

function Test-Bridge($base) {
    try {
        $r = Invoke-WebRequest -Uri "$base/health" -UseBasicParsing -TimeoutSec 3
        Write-Host "  $base/health -> $($r.StatusCode) $($r.Content)"
        return $true
    } catch {
        Write-Host "  $base/health -> FAIL ($($_.Exception.Message))" -ForegroundColor Yellow
        return $false
    }
}

Write-Host "`nHealth (local):"
$okLocal = Test-Bridge "http://127.0.0.1:$port"

$ip = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object {
    $_.IPAddress -notlike '127.*' -and $_.PrefixOrigin -ne 'WellKnown'
} | Select-Object -First 1).IPAddress
if ($ip) {
    Write-Host "Health (LAN $ip):"
    Test-Bridge "http://${ip}:$port" | Out-Null
}

if (-not $okLocal) {
    Write-Host "`nBridge is DOWN. Start ONE of:" -ForegroundColor Red
    Write-Host "  - TruckDeck.exe (built-in bridge, recommended)"
    Write-Host "  - Html\input_bridge\start_bridge.bat (standalone Python; close TruckDeck first)"
    if (-not (netsh http show urlacl | Select-String ":25556/")) {
        Write-Host "`nMissing URL ACL for port 25556. Run setup_bridge_ports.ps1 as Administrator once." -ForegroundColor Yellow
    }
    exit 1
}

try {
    $ev = Invoke-WebRequest -Uri "http://127.0.0.1:$port/api/dashboard/events" -UseBasicParsing -TimeoutSec 3
    Write-Host "`nDashboard events API: OK"
} catch {
    Write-Host "`nDashboard events API: MISSING (rebuild TruckDeck.exe)" -ForegroundColor Yellow
}

Write-Host "`nDone."
