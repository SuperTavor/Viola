$ErrorActionPreference = "Stop"

Write-Host "Starting Viola Release Build..." -ForegroundColor Cyan

# Define paths
$RootDir = $PSScriptRoot
$OutputDir = Join-Path $RootDir "output"
$CliProject = Join-Path $RootDir "Viola.CLI/Viola.CLI.csproj"
$WinFormsProject = Join-Path $RootDir "Viola.WinForms/Viola.WinForms.csproj"
$IssFile = Join-Path $RootDir "Viola.WinForms-Setup.iss"

# Clean Output Directory
if (Test-Path $OutputDir) {
    Write-Host "Cleaning output directory..." -ForegroundColor Yellow
    Remove-Item $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir | Out-Null

# 1. Build CLI (Portable, Framework Dependent)
Write-Host "Building Viola.CLI (Portable)..." -ForegroundColor Cyan
$CliOut = Join-Path $OutputDir "CLI-Small"
dotnet publish $CliProject -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o $CliOut
if ($LASTEXITCODE -ne 0) { throw "CLI Build Failed" }

Copy-Item (Join-Path $CliOut "Viola.CLI.exe") (Join-Path $OutputDir "Viola.CLI-Portable.exe") -Force
Write-Host "Viola.CLI-Portable.exe created." -ForegroundColor Green

# 2. Build WinForms (Portable, Framework Dependent)
Write-Host "Building Viola.WinForms (Portable)..." -ForegroundColor Cyan
$WinFormsOut = Join-Path $OutputDir "WinForms-Small"
dotnet publish $WinFormsProject -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o $WinFormsOut
if ($LASTEXITCODE -ne 0) { throw "WinForms Build Failed" }

Copy-Item (Join-Path $WinFormsOut "Viola.WinForms.exe") (Join-Path $OutputDir "Viola.WinForms-Portable.exe") -Force
Write-Host "Viola.WinForms-Portable.exe created." -ForegroundColor Green

# 3. Build Setup Installer
Write-Host "Building Setup Installer..." -ForegroundColor Cyan
$IsccPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

if (-not (Test-Path $IsccPath)) {
    Write-Warning "Inno Setup Compiler (ISCC.exe) not found at default location: $IsccPath"
    Write-Warning "Please install Inno Setup or update the path in this script."
    exit 1
}

& $IsccPath $IssFile
if ($LASTEXITCODE -ne 0) { throw "Setup Creation Failed" }

Write-Host "Viola.WinForms-Setup.exe created." -ForegroundColor Green

# Summary
Write-Host "`nBuild Complete! Release files are in: $OutputDir" -ForegroundColor Green
Get-ChildItem "$OutputDir/*.exe" | Select-Object Name, @{Name="Size (KB)";Expression={[math]::Round($_.Length/1KB, 0)}}
