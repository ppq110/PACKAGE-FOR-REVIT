param(
    [Parameter(Mandatory = $true)]
    [string]$SuperAdminEmail,

    [Parameter(Mandatory = $true)]
    [string]$LegacySupabaseUrl,

    [Parameter(Mandatory = $true)]
    [string]$LegacySupabaseAnonKey,

    [string]$AuthServerUrl = "http://localhost:5050"
)

$ErrorActionPreference = "Stop"

$configRoot = Join-Path $env:ProgramData "BIMLab\DynLock"
$authConfig = Join-Path $configRoot "authserver.json"

New-Item -ItemType Directory -Force -Path $configRoot | Out-Null

$config = [ordered]@{
    AuthServerUrl = $AuthServerUrl
    SuperAdminEmail = $SuperAdminEmail
}

$config | ConvertTo-Json | Set-Content -Encoding UTF8 $authConfig

$env:DYNLOCK_LEGACY_SUPABASE_URL = $LegacySupabaseUrl
$env:DYNLOCK_LEGACY_SUPABASE_ANON_KEY = $LegacySupabaseAnonKey

dotnet run --project "$PSScriptRoot\src\DynLock.AuthServer\DynLock.AuthServer.csproj" -c Release -- --import-supabase
