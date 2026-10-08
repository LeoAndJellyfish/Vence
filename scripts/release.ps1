[CmdletBinding()]
param(
    [ValidateSet("x86", "x64", "ARM64")]
    [string]$Platform = "x64",

    [string]$OutputDirectory = "",

    [switch]$NoTests,

    [switch]$Unsigned
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "common.ps1")

$repoRoot = Get-VenceRepoRoot

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = "artifacts\publish\Vence.App\Release\$Platform-msix"
}

$resolvedOutputDirectory = Resolve-VencePath -RepoRoot $repoRoot -Path $OutputDirectory

if (-not $NoTests) {
    & (Join-Path $PSScriptRoot "test.ps1") -Configuration Release -Platform $Platform
    if ($LASTEXITCODE -ne 0) {
        throw "Release tests failed with exit code $LASTEXITCODE."
    }
}

& (Join-Path $PSScriptRoot "package-msix.ps1") `
    -Configuration Release `
    -Platform $Platform `
    -OutputDirectory $resolvedOutputDirectory `
    -Unsigned:$Unsigned
if ($LASTEXITCODE -ne 0) {
    throw "Release MSIX publish failed with exit code $LASTEXITCODE."
}
