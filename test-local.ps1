#!/usr/bin/env pwsh
# Build the Puhu.GitKraken plugin, deploy it into the local Puhu plugin folder,
# and launch the Puhu host so you can test the plugin end-to-end.
#
# Usage:
#   ./test-local.ps1                # build (Debug) + deploy + run host
#   ./test-local.ps1 -Configuration Release
#   ./test-local.ps1 -NoRun         # just build + deploy, don't launch the host
#   ./test-local.ps1 -NoBuild       # skip build, just deploy existing dll + run

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
$pluginsDir    = Join-Path $HOME ".servus/plugins"

# 1. Build the plugin
if (-not $NoBuild) {
    Write-Host "==> Building Puhu.GitKraken ($Configuration)..." -ForegroundColor Cyan
    dotnet build $pluginProject -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw "Plugin build failed." }
}

# 2. Deploy only Puhu.GitKraken.dll — the host supplies all shared deps
#    (Akka, R3, Termina, ...) via the Puhu.Plugin SDK it references.
$builtDll = Join-Path $repoRoot "src/Puhu.GitKraken/bin/$Configuration/net10.0/Puhu.GitKraken.dll"
if (-not (Test-Path $builtDll)) { throw "Built plugin not found at $builtDll" }

if (-not (Test-Path $pluginsDir)) { New-Item -ItemType Directory -Force -Path $pluginsDir | Out-Null }

Write-Host "==> Deploying Puhu.GitKraken.dll -> $pluginsDir" -ForegroundColor Cyan
Copy-Item $builtDll (Join-Path $pluginsDir "Puhu.GitKraken.dll") -Force
$builtPdb = [System.IO.Path]::ChangeExtension($builtDll, ".pdb")
if (Test-Path $builtPdb) { Copy-Item $builtPdb (Join-Path $pluginsDir "Puhu.GitKraken.pdb") -Force }

