[CmdletBinding()]
param()

function Get-VenceRepoRoot {
    return (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

function Get-VenceAppProjectPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    return Join-Path $RepoRoot "src\Vence.App\Vence.App.csproj"
}

function Get-VencePackageManifestPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    return Join-Path $RepoRoot "src\Vence.App\Package.appxmanifest"
}

function Initialize-VenceDotNetEnvironment {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    $dotnetHome = Join-Path $RepoRoot ".dotnet"
    New-Item -ItemType Directory -Force -Path $dotnetHome | Out-Null

    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
    $env:DOTNET_CLI_HOME = $dotnetHome
    $env:WINAPP_CLI_TELEMETRY_OPTOUT = "1"
}

function Get-VenceRuntimeIdentifier {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet("x86", "x64", "ARM64")]
        [string]$Platform
    )

    return "win-$($Platform.ToLowerInvariant())"
}

function Resolve-VencePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,

        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $RepoRoot $Path))
}

function Get-VencePackagePublisher {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    $manifestPath = Get-VencePackageManifestPath -RepoRoot $RepoRoot
    [xml]$manifest = Get-Content -Path $manifestPath

    return $manifest.Package.Identity.Publisher
}

function New-VenceCodeSigningCertificate {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,

        [Parameter(Mandatory = $true)]
        [string]$CertificatePath,

        [Parameter(Mandatory = $true)]
        [string]$Password,

        [int]$ValidYears = 10,

        [switch]$Force
    )

    $resolvedCertificatePath = Resolve-VencePath -RepoRoot $RepoRoot -Path $CertificatePath
    $certificateDirectory = Split-Path -Parent $resolvedCertificatePath
    $cerPath = [System.IO.Path]::ChangeExtension($resolvedCertificatePath, ".cer")

    if ((Test-Path -LiteralPath $resolvedCertificatePath) -and -not $Force) {
        return @{
            PfxPath = $resolvedCertificatePath
            CerPath = $cerPath
        }
    }

    New-Item -ItemType Directory -Force -Path $certificateDirectory | Out-Null

    $publisher = Get-VencePackagePublisher -RepoRoot $RepoRoot
    try {
        $securePassword = ConvertTo-SecureString -String $Password -AsPlainText -Force
        $certificate = New-SelfSignedCertificate `
            -Type Custom `
            -Subject $publisher `
            -KeyAlgorithm RSA `
            -KeyLength 3072 `
            -HashAlgorithm SHA256 `
            -KeyUsage DigitalSignature `
            -CertStoreLocation Cert:\CurrentUser\My `
            -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}") `
            -NotAfter (Get-Date).AddYears($ValidYears)

        Export-PfxCertificate `
            -Cert $certificate `
            -FilePath $resolvedCertificatePath `
            -Password $securePassword | Out-Null
        Export-Certificate `
            -Cert $certificate `
            -FilePath $cerPath | Out-Null

        return @{
            PfxPath = $resolvedCertificatePath
            CerPath = $cerPath
        }
    }
    catch {
        Write-Warning "Windows certificate store generation failed. Falling back to file-only certificate generation. Details: $($_.Exception.Message)"
    }

    $rsa = [System.Security.Cryptography.RSA]::Create(3072)
    try {
        $request = [System.Security.Cryptography.X509Certificates.CertificateRequest]::new(
            $publisher,
            $rsa,
            [System.Security.Cryptography.HashAlgorithmName]::SHA256,
            [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)

        $request.CertificateExtensions.Add(
            [System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension]::new($false, $false, 0, $true))
        $request.CertificateExtensions.Add(
            [System.Security.Cryptography.X509Certificates.X509KeyUsageExtension]::new(
                [System.Security.Cryptography.X509Certificates.X509KeyUsageFlags]::DigitalSignature,
                $true))

        $codeSigningOid = [System.Security.Cryptography.Oid]::new("1.3.6.1.5.5.7.3.3")
        $enhancedKeyUsages = [System.Security.Cryptography.OidCollection]::new()
        [void]$enhancedKeyUsages.Add($codeSigningOid)
        $request.CertificateExtensions.Add(
            [System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension]::new($enhancedKeyUsages, $true))

        $notBefore = [System.DateTimeOffset]::Now.AddDays(-1)
        $notAfter = $notBefore.AddYears($ValidYears)
        $certificate = $request.CreateSelfSigned($notBefore, $notAfter)

        try {
            $pfxBytes = $certificate.Export(
                [System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx,
                $Password)
            [System.IO.File]::WriteAllBytes($resolvedCertificatePath, $pfxBytes)

            $cerBytes = $certificate.Export(
                [System.Security.Cryptography.X509Certificates.X509ContentType]::Cert)
            [System.IO.File]::WriteAllBytes($cerPath, $cerBytes)
        }
        finally {
            $certificate.Dispose()
        }
    }
    finally {
        $rsa.Dispose()
    }

    return @{
        PfxPath = $resolvedCertificatePath
        CerPath = $cerPath
    }
}
