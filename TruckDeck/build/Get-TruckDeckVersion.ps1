# Reads the display version from AssemblyInfo (single source of truth).
param(
    [string]$Root = (Split-Path $PSScriptRoot -Parent)
)

$assemblyInfo = Join-Path $Root "TruckDeck.Server\Properties\AssemblyInfo.cs"
if (Test-Path $assemblyInfo) {
    $text = Get-Content $assemblyInfo -Raw
    if ($text -match 'AssemblyInformationalVersion\("([^"]+)"\)') {
        return $Matches[1]
    }
}

$csproj = Join-Path $Root "TruckDeck.Server\Funbit.Ets.Telemetry.Server.csproj"
if (Test-Path $csproj) {
    [xml]$xml = Get-Content $csproj
    $v = $xml.Project.PropertyGroup.ApplicationVersion | Where-Object { $_ } | Select-Object -First 1
    if ($v) {
        $s = [string]$v
        if ($s -match '^(\d+\.\d+\.\d+)\.0$') { return $Matches[1] }
        return $s
    }
}

return "unknown"
