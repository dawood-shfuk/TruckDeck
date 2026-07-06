function Resolve-WslDistro {
    param([string]$Requested = "")

    if ($Requested) {
        & wsl -d $Requested -e true 2>$null
        if ($LASTEXITCODE -eq 0) { return $Requested }
    }

    foreach ($name in @("TruckDeckUbuntu", "Ubuntu-24.04", "Ubuntu-22.04", "Ubuntu", "Debian")) {
        & wsl -d $name -e true 2>$null
        if ($LASTEXITCODE -eq 0) { return $name }
    }

    return $null
}
