# Stop standalone Python input bridge (does not close TruckDeck.exe).
Get-CimInstance Win32_Process -Filter "Name='python.exe'" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -match 'bridge\.py' } |
    ForEach-Object {
        Write-Host "Stopping python bridge PID $($_.ProcessId)"
        Stop-Process -Id $_.ProcessId -Force
    }

if (Get-NetTCPConnection -LocalPort 25556 -State Listen -ErrorAction SilentlyContinue) {
    Write-Host "Port 25556 still in use (likely TruckDeck.exe). Close TruckDeck to free it."
} else {
    Write-Host "Port 25556 is free."
}
