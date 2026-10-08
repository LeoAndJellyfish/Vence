[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [ValidateSet("x86", "x64", "ARM64")]
    [string]$Platform = "x64",

    [switch]$NoRestore
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "common.ps1")

$repoRoot = Get-VenceRepoRoot
$solutionPath = Join-Path $repoRoot "Vence.sln"

Initialize-VenceDotNetEnvironment -RepoRoot $repoRoot

if (-not $NoRestore) {
    Write-Host "Restoring Vence.sln ($Configuration, $Platform)..."
    dotnet restore $solutionPath -m:1 /p:Platform=$Platform
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore failed with exit code $LASTEXITCODE."
    }
}

Write-Host "Testing Vence.sln ($Configuration, $Platform)..."
dotnet test $solutionPath `
    --no-restore `
    -c $Configuration `
    /p:Platform=$Platform `
    -m:1 `
    -v:minimal
if ($LASTEXITCODE -ne 0) {
    throw "dotnet test failed with exit code $LASTEXITCODE."
}
