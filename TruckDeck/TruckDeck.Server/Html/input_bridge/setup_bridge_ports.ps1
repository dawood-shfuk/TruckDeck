# One-time setup: HTTP URL ACL + firewall for telemetry (25555) and input bridge (25556).
# Must run as Administrator.
#Requires -RunAsAdministrator

$ErrorActionPreference = 'Stop'
$ports = @(25555, 25556)

Write-Host "Adding URL ACL and firewall rules for ports: $($ports -join ', ')"

foreach ($port in $ports) {
    $url = "http://+:$port/"
    $existing = netsh http show urlacl url=$url 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $existing) {
        Write-Host "  URL ACL: $url"
        netsh http add urlacl url=$url user=Everyone | Out-Null
    } else {
        Write-Host "  URL ACL already set: $url"
    }

    $ruleName = "TRUCKDECK (PORT $port)"
    $fw = netsh advfirewall firewall show rule name="$ruleName" 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  Firewall: $ruleName"
        netsh advfirewall firewall add rule name="$ruleName" dir=in action=allow protocol=TCP localport=$port remoteip=localsubnet | Out-Null
    } else {
        Write-Host "  Firewall rule exists: $ruleName"
    }
}

Write-Host "Done. Restart TruckDeck or start_bridge.bat."
