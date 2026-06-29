param(
    [string]$DateStamp = (Get-Date -Format "dd-MM-yyyy"),
    [string]$AuthServerUrl = "http://192.168.110.213:5051",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$scriptArgs = @("-DateStamp", $DateStamp, "-AuthServerUrl", $AuthServerUrl)
if ($SkipBuild) {
    $scriptArgs += "-SkipBuild"
}

Write-Host "This script now builds both ready-to-send packages: Leader + Member." -ForegroundColor Cyan
& "$PSScriptRoot\rebuild_and_dist.ps1" @scriptArgs
