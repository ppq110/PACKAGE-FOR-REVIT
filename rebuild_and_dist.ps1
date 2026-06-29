param(
    [string]$DateStamp = (Get-Date -Format "dd-MM-yyyy"),
    [string]$AuthServerUrl = "http://192.168.110.213:5050",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$base = $PSScriptRoot
$distRoot = Join-Path $base "dist"

function Build-Project($title, $project) {
    Write-Host ""
    Write-Host $title -ForegroundColor Cyan
    dotnet build $project -c Release `
        --source https://api.nuget.org/v3/index.json `
        /p:UseSharedCompilation=false
}

function Reset-Folder($path) {
    if (Test-Path $path) {
        Remove-Item $path -Recurse -Force
    }

    New-Item -ItemType Directory -Path $path -Force | Out-Null
}

function Write-Text($path, $content) {
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($path, $content.TrimStart(), $utf8NoBom)
}

if (-not $SkipBuild) {
    Build-Project "=== 1. Build DynLock.Core ===" "$base\src\DynLock.Core\DynLock.Core.csproj"
    Build-Project "=== 2. Build DynLock.Addin (net48 + net8.0-windows) ===" "$base\src\DynLock.Addin\DynLock.Addin.csproj"
    Build-Project "=== 3. Build DynLock.EncryptorGui (BIMLab Studio - Leader) ===" "$base\src\DynLock.EncryptorGui\DynLock.EncryptorGui.csproj"
    Build-Project "=== 4. Build DynLock.Installer (BIMLab Player - Member) ===" "$base\src\DynLock.Installer\DynLock.Installer.csproj"
}
else {
    Write-Host "SkipBuild enabled: packaging existing Release binaries." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== 5. Create ready-to-send packages ===" -ForegroundColor Cyan

New-Item -ItemType Directory -Path $distRoot -Force | Out-Null

$distMemberName = "BIMLab_Player_Member_$DateStamp"
$distLeaderName = "BIMLab_Studio_Leader_$DateStamp"
$distMember = Join-Path $distRoot $distMemberName
$distLeader = Join-Path $distRoot $distLeaderName

Reset-Folder $distMember
Reset-Folder $distLeader

$authServerUrl = $AuthServerUrl.Trim().TrimEnd('/')

$playerExe = "$base\src\DynLock.Installer\bin\Release\net48\BIMLab Player.exe"
$studioExe = "$base\src\DynLock.EncryptorGui\bin\Release\net48\BIMLab Studio.exe"
$studioNewtonsoft = "$base\src\DynLock.EncryptorGui\bin\Release\net48\Newtonsoft.Json.dll"

if (-not (Test-Path $playerExe)) {
    throw "Missing BIMLab Player build output: $playerExe"
}

if (-not (Test-Path $studioExe)) {
    throw "Missing BIMLab Studio build output: $studioExe"
}

Copy-Item $playerExe (Join-Path $distMember "BIMLab Player - $DateStamp.exe") -Force
Copy-Item $studioExe (Join-Path $distLeader "BIMLab Studio - $DateStamp.exe") -Force

if (Test-Path $studioNewtonsoft) {
    Copy-Item $studioNewtonsoft $distLeader -Force
}

$memberGuide = @"
==================================================================
  HUONG DAN CAI DAT & SU DUNG - BIMLab Player (Member) - $DateStamp
==================================================================

BIMLab Player dung cho Member de dang nhap Gmail, cai BIMLab Add-in vao Revit,
va load cac plugin .dynx do Leader gui.

------------------------------------------------------------------
CHUAN BI
------------------------------------------------------------------
  1) May server LAN $authServerUrl dang chay BIMLab Auth Server.
  2) App da nhung san dia chi Auth Server noi bo.
  3) Gmail Member da co trong database tren may server va dang active.
  4) Member can co file .dynx do Leader gui.

------------------------------------------------------------------
BUOC 1 - DANG NHAP VA CAI ADD-IN
------------------------------------------------------------------
  1) Mo file:
       BIMLab Player - $DateStamp.exe

  2) Nhap Gmail Member, vi du ten@gmail.com.
     App se tu ket noi $authServerUrl de kiem tra Gmail trong database local.

  3) Sau khi dang nhap thanh cong, launcher cai dat se mo ra.

  4) Chon phien ban Revit can cai:
       - Revit 2024
       - Revit 2025
       - Revit 2026

  5) Bam "Cai add-in".

  6) App se cai BIMLab Add-in vao Revit. Neu khong ghi duoc scope may,
     app se cai theo scope user.

------------------------------------------------------------------
BUOC 2 - LOAD PLUGIN TRONG REVIT
------------------------------------------------------------------
  1) Mo Revit.

  2) Vao tab BIMLab.

  3) Bam Login va nhap Gmail dang ten@gmail.com.
     Add-in se tu dung Auth Server noi bo, khong can nhap server URL.

  4) Bam Load, chon file .dynx do Leader gui.

  5) Add-in se doc metadata trong .dynx va tao nut plugin tren ribbon.

  6) Bam nut plugin vua load de chay cong cu.

------------------------------------------------------------------
GHI CHU
------------------------------------------------------------------
  - Member khong thay Dynamo graph goc.
  - Neu cong cu co input, BIMLab Player se hien form nhap thong so.
  - Neu 2 plugin khac ten nhung cung ten panel, ca 2 se nam chung panel.

==================================================================
"@

$leaderGuide = @"
==================================================================
  HUONG DAN SU DUNG - BIMLab Studio (Leader) - $DateStamp
==================================================================

BIMLab Studio dung cho Leader de ma hoa file Dynamo .dyn thanh file .dynx.
File .dynx se chua graph da ma hoa va thong tin plugin: ten panel, ten plugin,
icon plugin.

------------------------------------------------------------------
CHUAN BI
------------------------------------------------------------------
  1) May server LAN $authServerUrl dang chay BIMLab Auth Server.
  2) App da nhung san dia chi Auth Server noi bo.
  3) Gmail Leader da co trong database tren may server va dang active.

------------------------------------------------------------------
CACH DUNG
------------------------------------------------------------------
  1) Mo file:
       BIMLab Studio - $DateStamp.exe

  2) Nhap Gmail Leader, vi du ten@gmail.com.
     App se tu ket noi $authServerUrl de kiem tra Gmail trong database local.

  3) Sau khi dang nhap thanh cong, them file .dyn bang nut "Them file .dyn"
     hoac keo-tha file .dyn vao app.

  4) Nhap du thong tin plugin:
       - Ten panel
       - Ten plugin
       - Icon plugin

  5) Bam "Ma hoa tat ca".

  6) File .dynx se duoc tao ngay canh file .dyn goc.

  7) Gui file .dynx cho member.

------------------------------------------------------------------
LUU Y
------------------------------------------------------------------
  - Chi gui file .dynx cho member, khong gui file .dyn goc.
  - Neu 2 plugin dung chung ten panel, ben member se duoc gom vao cung 1 panel.
  - Key ma hoa da nam san trong app. Leader khong can cau hinh key.

==================================================================
"@

Write-Text (Join-Path $distMember "Huong dan - BIMLab Player - $DateStamp.txt") $memberGuide
Write-Text (Join-Path $distLeader "Huong dan - BIMLab Studio - $DateStamp.txt") $leaderGuide

Write-Host ""
Write-Host "=== 6. Rebuild zip files ===" -ForegroundColor Cyan

$zipMember = Join-Path $distRoot "$distMemberName.zip"
$zipLeader = Join-Path $distRoot "$distLeaderName.zip"

if (Test-Path $zipMember) { Remove-Item $zipMember -Force }
if (Test-Path $zipLeader) { Remove-Item $zipLeader -Force }

Compress-Archive -Path "$distMember\*" -DestinationPath $zipMember -Force
Compress-Archive -Path "$distLeader\*" -DestinationPath $zipLeader -Force

Write-Host "  Ready: $distMember"
Write-Host "  Ready: $zipMember"
Write-Host "  Ready: $distLeader"
Write-Host "  Ready: $zipLeader"

Write-Host ""
Write-Host "=== Done. Leader + Member packages are ready to send. ===" -ForegroundColor Green
