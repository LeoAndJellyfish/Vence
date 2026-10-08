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
$projectPath = Join-Path $repoRoot "src\Vence.App\Vence.App.csproj"
$runtimeIdentifier = Get-VenceRuntimeIdentifier -Platform $Platform

Initialize-VenceDotNetEnvironment -RepoRoot $repoRoot

if (-not $NoRestore) {
    Write-Host "Restoring Vence.App ($Configuration, $Platform, $runtimeIdentifier)..."
    dotnet restore $projectPath -m:1 /p:Platform=$Platform -r $runtimeIdentifier
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore failed with exit code $LASTEXITCODE."
    }
}

Write-Host "Starting Vence.App for development ($Configuration, $Platform, $runtimeIdentifier)..."
dotnet run `
    --project $projectPath `
    --configuration $Configuration `
    --runtime $runtimeIdentifier `
    --no-restore `
    /p:Platform=$Platform
if ($LASTEXITCODE -ne 0) {
    throw "dotnet run failed with exit code $LASTEXITCODE."
}
