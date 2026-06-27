$ErrorActionPreference = "Stop"
$base = $PSScriptRoot

Write-Host "=== 1. Build DynLock.Core ===" -ForegroundColor Cyan
dotnet build "$base\src\DynLock.Core\DynLock.Core.csproj" -c Release `
    --source https://api.nuget.org/v3/index.json `
    /p:UseSharedCompilation=false

Write-Host ""
Write-Host "=== 2. Build DynLock.Addin (net48 + net8.0-windows) ===" -ForegroundColor Cyan
dotnet build "$base\src\DynLock.Addin\DynLock.Addin.csproj" -c Release `
    --source https://api.nuget.org/v3/index.json `
    /p:UseSharedCompilation=false

Write-Host ""
Write-Host "=== 3. Build DynLock.EncryptorGui (BIMLab Studio) ===" -ForegroundColor Cyan
dotnet build "$base\src\DynLock.EncryptorGui\DynLock.EncryptorGui.csproj" -c Release `
    --source https://api.nuget.org/v3/index.json `
    /p:UseSharedCompilation=false

Write-Host ""
Write-Host "=== 4. Build DynLock.Installer (BIMLab Player - embeds Addin) ===" -ForegroundColor Cyan
dotnet build "$base\src\DynLock.Installer\DynLock.Installer.csproj" -c Release `
    --source https://api.nuget.org/v3/index.json `
    /p:UseSharedCompilation=false

Write-Host ""
Write-Host "=== 5. Update dist folders ===" -ForegroundColor Cyan

$distMember = "$base\dist\BIMLab_Player_Member"
$distLeader = "$base\dist\BIMLab_Studio_Leader"

$playerExe = "$base\src\DynLock.Installer\bin\Release\net48\BIMLab Player.exe"
if (Test-Path $playerExe) {
    Copy-Item $playerExe $distMember -Force
    Write-Host "  Copied: BIMLab Player.exe -> BIMLab_Player_Member"
} else {
    Write-Warning "  NOT FOUND: $playerExe"
}

$studioExe = "$base\src\DynLock.EncryptorGui\bin\Release\net48\BIMLab Studio.exe"
if (Test-Path $studioExe) {
    Copy-Item $studioExe $distLeader -Force
    Write-Host "  Copied: BIMLab Studio.exe -> BIMLab_Studio_Leader"
} else {
    Write-Warning "  NOT FOUND: $studioExe"
}

Write-Host ""
Write-Host "=== 6. Rebuild zip files ===" -ForegroundColor Cyan

$zipMember = "$base\dist\BIMLab_Player_Member.zip"
$zipLeader = "$base\dist\BIMLab_Studio_Leader.zip"

if (Test-Path $zipMember) { Remove-Item $zipMember -Force }
if (Test-Path $zipLeader) { Remove-Item $zipLeader -Force }

Compress-Archive -Path $distMember -DestinationPath $zipMember -Force
Compress-Archive -Path $distLeader -DestinationPath $zipLeader -Force

Write-Host "  Created: BIMLab_Player_Member.zip"
Write-Host "  Created: BIMLab_Studio_Leader.zip"

Write-Host ""
Write-Host "=== Done! ===" -ForegroundColor Green
Write-Host "dist/ contains 2 folders + 2 zip files ready to send."
