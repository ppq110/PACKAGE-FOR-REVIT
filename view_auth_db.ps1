param(
    [string]$AuthServerUrl = "http://192.168.110.213:5051",
    [string]$ManagerEmail = "projectbim.bimlab@gmail.com"
)

$ErrorActionPreference = "Stop"

$baseUrl = $AuthServerUrl.Trim().TrimEnd("/")

Write-Host "BIMLab Auth DB" -ForegroundColor Cyan
Write-Host "Server: $baseUrl"
Write-Host ""

Write-Host "Health:" -ForegroundColor Cyan
$health = Invoke-RestMethod "$baseUrl/api/health"
$health | Format-List

Write-Host ""
Write-Host "Authorized Gmail list:" -ForegroundColor Cyan
$leaders = Invoke-RestMethod "$baseUrl/api/leaders?managerEmail=$([uri]::EscapeDataString($ManagerEmail))"

if (-not $leaders -or $leaders.Count -eq 0) {
    Write-Host "No Gmail records found." -ForegroundColor Yellow
    return
}

$leaders |
    Select-Object `
        email,
        fullName,
        isActive,
        canManage,
        addedBy,
        createdAt,
        lastLogin |
    Format-Table -AutoSize
