param(
    [string]$SuperAdminEmail = "projectbim.bimlab@gmail.com",

    [string]$AuthServerUrl = "http://192.168.110.213:5051",

    [string]$BindUrl = "http://0.0.0.0:5051",

    [ValidateSet("sqlite", "postgres")]
    [string]$DatabaseProvider = "sqlite",

    [string]$PostgresConnectionString = "",

    [string]$LegacySupabaseUrl = "",

    [string]$LegacySupabaseAnonKey = "",

    [switch]$StartServer
)

$ErrorActionPreference = "Stop"

$configRoot = Join-Path $env:ProgramData "BIMLab\DynLock"
$authConfig = Join-Path $configRoot "authserver.json"

New-Item -ItemType Directory -Force -Path $configRoot | Out-Null

$config = [ordered]@{
    AuthServerUrl = $AuthServerUrl.Trim().TrimEnd('/')
    SuperAdminEmail = $SuperAdminEmail.Trim().ToLowerInvariant()
    DatabaseProvider = $DatabaseProvider
}

if ($DatabaseProvider -eq "postgres") {
    if (-not $PostgresConnectionString) {
        throw "PostgresConnectionString is required when -DatabaseProvider postgres."
    }

    $config.DatabaseConnectionString = $PostgresConnectionString
}

$config | ConvertTo-Json | Set-Content -Encoding UTF8 $authConfig

Write-Host "[OK] Wrote config: $authConfig" -ForegroundColor Green
Write-Host "[OK] Auth server URL: $($config.AuthServerUrl)" -ForegroundColor Green
Write-Host "[OK] Super admin: $($config.SuperAdminEmail)" -ForegroundColor Green
Write-Host "[OK] Database provider: $($config.DatabaseProvider)" -ForegroundColor Green

if ($LegacySupabaseUrl -and $LegacySupabaseAnonKey) {
    Write-Host ""
    Write-Host "Importing/upserting Gmail list from Supabase into local auth.db..." -ForegroundColor Cyan

    $env:DYNLOCK_LEGACY_SUPABASE_URL = $LegacySupabaseUrl
    $env:DYNLOCK_LEGACY_SUPABASE_ANON_KEY = $LegacySupabaseAnonKey

    dotnet run --project "$PSScriptRoot\src\DynLock.AuthServer\DynLock.AuthServer.csproj" -c Release -- --import-supabase
}
else {
    Write-Host ""
    Write-Host "Skipped Supabase import. Pass -LegacySupabaseUrl and -LegacySupabaseAnonKey when you want to sync old Gmail data." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Open firewall once on server machine if needed:" -ForegroundColor Cyan
Write-Host '  netsh advfirewall firewall add rule name="BIMLab Auth Server 5051" dir=in action=allow protocol=TCP localport=5051'

if ($StartServer) {
    Write-Host ""
    Write-Host "Starting BIMLab Auth Server on $BindUrl ..." -ForegroundColor Cyan
    $env:DYNLOCK_AUTH_SERVER_BIND_URL = $BindUrl
    dotnet run --project "$PSScriptRoot\src\DynLock.AuthServer\DynLock.AuthServer.csproj" -c Release
}
else {
    Write-Host ""
    Write-Host "Start server with:" -ForegroundColor Cyan
    Write-Host "  .\run_auth_server.ps1 -BindUrl `"$BindUrl`""
}
