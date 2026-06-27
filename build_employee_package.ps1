$basePath = 'd:\DATA\bimlab_dynamo\DynLock-full (1)\DynLock-full'
$outputPath = Join-Path $basePath 'dist'

# Create dist folder if not exists
if (-not (Test-Path $outputPath)) { 
    New-Item -ItemType Directory -Path $outputPath -Force | Out-Null 
}

Write-Host "Creating Employee Package..."

# Create Employee Package directory
$empPath = Join-Path $outputPath 'DynLock_Employee_Package'
if (Test-Path $empPath) { 
    Remove-Item -Path $empPath -Recurse -Force 
}
New-Item -ItemType Directory -Path $empPath -Force | Out-Null

# Copy DLL files
Copy-Item -Path "$basePath\src\DynLock.Addin\bin\Release\DynLock.Addin.dll" -Destination $empPath -Force
Copy-Item -Path "$basePath\src\DynLock.Addin\bin\Release\DynLock.Core.dll" -Destination $empPath -Force
Copy-Item -Path "$basePath\src\DynLock.Addin\bin\Release\Newtonsoft.Json.dll" -Destination $empPath -Force

# Copy .addin file
Copy-Item -Path "$basePath\install\DynLock.addin" -Destination $empPath -Force

# Copy installer exe
$installerPath = "$basePath\src\DynLock.Installer\bin\Release\net48\BIMLab Player.exe"
Copy-Item -Path $installerPath -Destination $empPath -Force

# Copy documentation
Copy-Item -Path "$basePath\HUONG_DAN_NHAN_VIEN.txt" -Destination $empPath -Force
Copy-Item -Path "$basePath\HUONG_DAN_SU_DUNG.txt" -Destination $empPath -Force
Copy-Item -Path "$basePath\HUONG_DAN_TONG_THE.txt" -Destination $empPath -Force
Copy-Item -Path "$basePath\README_NHAN_VIEN.txt" -Destination $empPath -Force
Copy-Item -Path "$basePath\HUONG_DAN_TEAM_LEAD.txt" -Destination $empPath -Force

# Create ZIP
$zipEmp = Join-Path $outputPath 'DynLock_Employee_Package.zip'
if (Test-Path $zipEmp) { 
    Remove-Item -Path $zipEmp -Force 
}
Compress-Archive -Path $empPath -DestinationPath $zipEmp -Force

Write-Host "Employee Package created: $zipEmp"
Write-Host ""
Write-Host "Package contents:"
Get-ChildItem -Path $empPath | Select-Object Name
