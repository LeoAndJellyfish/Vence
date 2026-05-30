[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [ValidateSet("x86", "x64", "ARM64")]
    [string]$Platform = "x64",

    [string]$OutputDirectory = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$projectPath = Join-Path $repoRoot "src\Vence.App\Vence.App.csproj"

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot "artifacts\publish\Vence.App\$Configuration\$Platform"
}
elseif (-not [System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot $OutputDirectory
}

$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)

$runtimeIdentifier = "win-$($Platform.ToLowerInvariant())"

$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:DOTNET_CLI_HOME = Join-Path $repoRoot ".dotnet"

Write-Host "Restoring Vence.App..."
dotnet restore $projectPath `
    -m:1 `
    -p:Platform=$Platform `
    -r $runtimeIdentifier
if ($LASTEXITCODE -ne 0) {
    throw "dotnet restore failed with exit code $LASTEXITCODE."
}

Write-Host "Publishing Vence.App ($Configuration, $Platform, $runtimeIdentifier)..."
dotnet publish $projectPath `
    -m:1 `
    --no-restore `
    -c $Configuration `
    -p:Platform=$Platform `
    -r $runtimeIdentifier `
    --self-contained true `
    -o $OutputDirectory
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Write-Host "Publish output: $OutputDirectory"
