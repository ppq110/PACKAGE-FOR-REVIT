param(
    [string]$SuperAdminEmail = "projectbim.bimlab@gmail.com",

    [Parameter(Mandatory = $true)]
    [string]$LegacySupabaseUrl,

    [Parameter(Mandatory = $true)]
    [string]$LegacySupabaseAnonKey,

    [string]$AuthServerUrl = "http://192.168.110.213:5050",

    [ValidateSet("sqlite", "postgres")]
    [string]$DatabaseProvider = "sqlite",

    [string]$PostgresConnectionString = ""
)

$ErrorActionPreference = "Stop"

$configRoot = Join-Path $env:ProgramData "BIMLab\DynLock"
$authConfig = Join-Path $configRoot "authserver.json"

New-Item -ItemType Directory -Force -Path $configRoot | Out-Null

$config = [ordered]@{
    AuthServerUrl = $AuthServerUrl
    SuperAdminEmail = $SuperAdminEmail
    DatabaseProvider = $DatabaseProvider
}

if ($DatabaseProvider -eq "postgres") {
    if (-not $PostgresConnectionString) {
        throw "PostgresConnectionString is required when -DatabaseProvider postgres."
    }

    $config.DatabaseConnectionString = $PostgresConnectionString
}

$config | ConvertTo-Json | Set-Content -Encoding UTF8 $authConfig

$env:DYNLOCK_LEGACY_SUPABASE_URL = $LegacySupabaseUrl
$env:DYNLOCK_LEGACY_SUPABASE_ANON_KEY = $LegacySupabaseAnonKey

dotnet run --project "$PSScriptRoot\src\DynLock.AuthServer\DynLock.AuthServer.csproj" -c Release -- --import-supabase
