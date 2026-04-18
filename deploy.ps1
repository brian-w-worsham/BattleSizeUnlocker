<#
.SYNOPSIS
    Build and deploy BattleSizeUnlocker mod to Bannerlord's Modules folder.
.DESCRIPTION
    Builds the project and copies the module descriptor and runtime assemblies to the game.
.PARAMETER GameFolder
    Path to your Bannerlord installation. Defaults to the local Steam installation.
.PARAMETER Configuration
    Build configuration. Default: Release.
#>
param(
    [string]$GameFolder = "C:\Games\steamapps\common\Mount & Blade II Bannerlord",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$ModuleName = "BattleSizeUnlocker"
$TargetModuleDir = Join-Path $GameFolder "Modules\$ModuleName"
$BinDir = Join-Path $TargetModuleDir "bin\Win64_Shipping_Client"

Write-Host "=== Building $ModuleName ===" -ForegroundColor Cyan

dotnet build "src\BattleSizeUnlocker\BattleSizeUnlocker.csproj" `
    -c $Configuration `
    -p:GameFolder="$GameFolder"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed!"
    exit 1
}

Write-Host "=== Deploying to $TargetModuleDir ===" -ForegroundColor Cyan

New-Item -ItemType Directory -Path $BinDir -Force | Out-Null

Copy-Item "Module\SubModule.xml" -Destination $TargetModuleDir -Force

$runtimeAssemblies = @(
    "src\BattleSizeUnlocker\bin\$Configuration\net472\BattleSizeUnlocker.dll",
    "src\BattleSizeUnlocker\bin\$Configuration\net472\ModLib.Definitions.dll"
)

foreach ($assembly in $runtimeAssemblies) {
    if (Test-Path $assembly) {
        Copy-Item $assembly -Destination $BinDir -Force
    }
}

Write-Host "=== Done! ===" -ForegroundColor Green
Write-Host "Module deployed to: $TargetModuleDir"
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Optional: install and enable ModLib if you want the in-game Mod Options menu"
Write-Host "  2. Launch Bannerlord"
Write-Host "  3. Enable 'BattleSizeUnlocker' in the launcher"
Write-Host "  4. If ModLib is installed, open Options > Mod Options and set the desired battle size"
Write-Host "  5. Start a field battle or custom battle to verify the larger cap"