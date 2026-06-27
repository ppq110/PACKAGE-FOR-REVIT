param(
    [string]$BindUrl = "http://0.0.0.0:5050"
)

$ErrorActionPreference = "Stop"

$env:DYNLOCK_AUTH_SERVER_BIND_URL = $BindUrl
dotnet run --project "$PSScriptRoot\src\DynLock.AuthServer\DynLock.AuthServer.csproj" -c Release
