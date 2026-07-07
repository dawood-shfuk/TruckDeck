# Returns the versioned build output folder: TruckDeck_build_{version}
param(
    [string]$Root = (Split-Path $PSScriptRoot -Parent)
)

$funbitRoot = Split-Path $Root -Parent
$version = & (Join-Path $PSScriptRoot "Get-TruckDeckVersion.ps1") -Root $Root
return Join-Path $funbitRoot "TruckDeck_build_$version"
