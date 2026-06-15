#!/usr/bin/env pwsh
# Build the Puhu.GitKraken plugin, deploy it into the local Puhu plugin folder,
# and launch the Puhu host so you can test the plugin end-to-end.
#
# Unlike Puhu.Btop (single DLL), this plugin requires LibGit2Sharp + its native
# git2 binary. We deploy to a subdirectory so the PluginLoadContext can resolve both.
#
# Usage:
#   ./test-local.ps1                # build (Debug) + deploy + run host
#   ./test-local.ps1 -Configuration Release
#   ./test-local.ps1 -NoRun         # just build + deploy, don't launch the host
#   ./test-local.ps1 -NoBuild       # skip build, just deploy existing output + run

[CmdletBinding()]
param(
    [string]$Configuration = "Debug",
    [switch]$NoRun,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$repoRoot = $PSScriptRoot

$pluginProject = Join-Path $repoRoot "src/Puhu.GitKraken/Puhu.GitKraken.csproj"
$hostProject   = Join-Path $repoRoot "lib/puhu/src/Puhu/Puhu.csproj"
$pluginsDir    = Join-Path $HOME ".servus/plugins/gitkraken"
$publishDir    = Join-Path $repoRoot "src/Puhu.GitKraken/bin/$Configuration/net10.0/publish"

# 1. Publish the plugin (includes all dependencies + native binaries)
if (-not $NoBuild) {
    Write-Host "==> Publishing Puhu.GitKraken ($Configuration)..." -ForegroundColor Cyan
    dotnet publish $pluginProject -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw "Plugin publish failed." }
}

if (-not (Test-Path $publishDir)) { throw "Publish output not found at $publishDir" }

# 2. Deploy to subdirectory — only plugin-specific files (host supplies shared deps)
if (-not (Test-Path $pluginsDir)) { New-Item -ItemType Directory -Force -Path $pluginsDir | Out-Null }

$rid = [System.Runtime.InteropServices.RuntimeInformation]::RuntimeIdentifier
$filesToDeploy = @(
    "Puhu.GitKraken.dll",
    "Puhu.GitKraken.pdb",
    "LibGit2Sharp.dll"
)

foreach ($file in $filesToDeploy) {
    $src = Join-Path $publishDir $file
    if (Test-Path $src) {
        Copy-Item $src (Join-Path $pluginsDir $file) -Force
        Write-Host "  -> $file" -ForegroundColor DarkGray
    }
}

# 3. Deploy native git2 binary preserving runtimes/ structure
$nativeSrc = Join-Path $publishDir "runtimes/$rid/native"
if (Test-Path $nativeSrc) {
    $nativeDst = Join-Path $pluginsDir "runtimes/$rid/native"
    if (-not (Test-Path $nativeDst)) { New-Item -ItemType Directory -Force -Path $nativeDst | Out-Null }
    foreach ($nativeFile in Get-ChildItem $nativeSrc -Filter "git2*") {
        Copy-Item $nativeFile.FullName (Join-Path $nativeDst $nativeFile.Name) -Force
        Write-Host "  -> runtimes/$rid/native/$($nativeFile.Name)" -ForegroundColor DarkGray
    }
}

Write-Host "==> Deployed to $pluginsDir" -ForegroundColor Cyan

# 4. Launch host
if (-not $NoRun) {
    Write-Host "==> Starting Puhu host..." -ForegroundColor Cyan
    dotnet run --project $hostProject -c $Configuration
}
