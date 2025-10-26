#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build script for ConsoleInk PowerShell module

.DESCRIPTION
    This script builds the ConsoleInk.Net library and copies the DLL to the PowerShell module directory.

.PARAMETER Configuration
    Build configuration (Debug or Release). Default is Release.

.PARAMETER SkipBuild
    Skip building the library and only copy existing DLL.

.EXAMPLE
    ./build-psmodule.ps1

.EXAMPLE
    ./build-psmodule.ps1 -Configuration Debug
#>

param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [Parameter()]
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'

# Paths
$rootDir = $PSScriptRoot
$projectPath = Join-Path $rootDir 'src/ConsoleInk.Net/ConsoleInk.Net.csproj'
$dllSource = Join-Path $rootDir "src/ConsoleInk.Net/bin/$Configuration/netstandard2.0/ConsoleInk.Net.dll"
$xmlSource = Join-Path $rootDir "src/ConsoleInk.Net/bin/$Configuration/netstandard2.0/ConsoleInk.Net.xml"
$moduleLibDir = Join-Path $rootDir 'src/powershell-module/ConsoleInk/lib'
$dllDest = Join-Path $moduleLibDir 'ConsoleInk.Net.dll'
$xmlDest = Join-Path $moduleLibDir 'ConsoleInk.Net.xml'

Write-Host "Building ConsoleInk PowerShell Module" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host ""

# Build the library
if (-not $SkipBuild) {
    Write-Host "Building ConsoleInk.Net library..." -ForegroundColor Green
    dotnet build $projectPath -c $Configuration

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed with exit code $LASTEXITCODE"
    }
    Write-Host ""
}

# Ensure lib directory exists
if (-not (Test-Path $moduleLibDir)) {
    Write-Host "Creating lib directory..." -ForegroundColor Yellow
    New-Item -Path $moduleLibDir -ItemType Directory -Force | Out-Null
}

# Copy DLL
if (Test-Path $dllSource) {
    Write-Host "Copying DLL to PowerShell module..." -ForegroundColor Green
    Copy-Item $dllSource -Destination $dllDest -Force
    Write-Host "  $dllSource" -ForegroundColor Gray
    Write-Host "  -> $dllDest" -ForegroundColor Gray
} else {
    Write-Error "DLL not found at: $dllSource"
}

# Copy XML documentation if it exists
if (Test-Path $xmlSource) {
    Write-Host "Copying XML documentation..." -ForegroundColor Green
    Copy-Item $xmlSource -Destination $xmlDest -Force
    Write-Host "  $xmlSource" -ForegroundColor Gray
    Write-Host "  -> $xmlDest" -ForegroundColor Gray
}

Write-Host ""
Write-Host "PowerShell module build complete!" -ForegroundColor Green
Write-Host "Module location: $(Join-Path $rootDir 'src/powershell-module/ConsoleInk')" -ForegroundColor Cyan
