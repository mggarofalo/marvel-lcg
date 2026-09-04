[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$InputPackage,
    [Parameter(Mandatory = $true)][string]$Output,
    [Parameter(Mandatory = $true)][string]$Publisher,
    [string]$WindowsSdkBin
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$WindowsSdkVersion = '10.0.26100.0'

function Invoke-Checked {
    param([Parameter(Mandatory = $true)][scriptblock]$Command)
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "external command failed with exit code $LASTEXITCODE"
    }
}

function Find-WindowsSdkTool {
    param([string]$Name)
    $sdkBin = if ($WindowsSdkBin) {
        [IO.Path]::GetFullPath($WindowsSdkBin)
    }
    else {
        Join-Path ${env:ProgramFiles(x86)} `
            "Windows Kits\10\bin\$WindowsSdkVersion\x64"
    }
    if ((Split-Path -Leaf $sdkBin) -cne 'x64' -or
        (Split-Path -Leaf (Split-Path -Parent $sdkBin)) -cne $WindowsSdkVersion) {
        throw "WindowsSdkBin must identify pinned Windows SDK $WindowsSdkVersion x64 tools"
    }
    $tool = Join-Path $sdkBin $Name
    if (-not (Test-Path -LiteralPath $tool -PathType Leaf)) {
        throw "$Name was not found in pinned Windows SDK $WindowsSdkVersion"
    }
    return $tool
}

function Assert-PayloadHashes {
    param([string]$Root)
    foreach ($line in Get-Content -LiteralPath (Join-Path $Root 'payload-sha256.txt')) {
        if ($line -cnotmatch '^(?<hash>[0-9a-f]{64}) \*(?<path>.+)$') {
            throw 'packaged payload hash manifest is malformed'
        }
        $file = Join-Path $Root $Matches['path'].Replace('/', '\')
        $actual = (Get-FileHash $file -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actual -cne $Matches['hash']) {
            throw "packaged payload differs at $($Matches['path'])"
        }
    }
}

function Add-PublicCertificateToStore {
    param(
        [Security.Cryptography.X509Certificates.X509Certificate2]$Certificate,
        [Security.Cryptography.X509Certificates.StoreName]$Name
    )
    $store = [Security.Cryptography.X509Certificates.X509Store]::new(
        $Name,
        [Security.Cryptography.X509Certificates.StoreLocation]::CurrentUser)
    try {
        $store.Open([Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
        $store.Add($Certificate)
    }
    finally {
        $store.Close()
    }
}

function Remove-PublicCertificateFromStore {
    param(
        [string]$Thumbprint,
        [Security.Cryptography.X509Certificates.StoreName]$Name
    )
    $store = [Security.Cryptography.X509Certificates.X509Store]::new(
        $Name,
        [Security.Cryptography.X509Certificates.StoreLocation]::CurrentUser)
    try {
        $store.Open([Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
        $matches = $store.Certificates.Find(
            [Security.Cryptography.X509Certificates.X509FindType]::FindByThumbprint,
            $Thumbprint,
            $false)
        foreach ($match in $matches) {
            $store.Remove($match)
        }
        $remaining = $store.Certificates.Find(
            [Security.Cryptography.X509Certificates.X509FindType]::FindByThumbprint,
            $Thumbprint,
            $false)
        if ($remaining.Count -ne 0) { throw 'retained' }
    }
    finally {
        $store.Close()
    }
}

if (-not (Test-Path -LiteralPath $InputPackage -PathType Leaf)) {
    throw 'unsigned MSIX input was not found'
}
if ($Publisher -cnotmatch '^CN=[^,=]+$') {
    throw 'community publisher must be one simple CN subject'
}

$outputParent = Split-Path -Parent ([IO.Path]::GetFullPath($Output))
if (-not (Test-Path -LiteralPath $outputParent -PathType Container)) {
    throw 'output parent directory does not exist'
}
$output = Join-Path $outputParent (Split-Path -Leaf $Output)
if (Test-Path -LiteralPath $output) {
    throw 'output already exists'
}

$makeAppx = Find-WindowsSdkTool 'makeappx.exe'
$signTool = Find-WindowsSdkTool 'signtool.exe'
$scratch = Join-Path ([IO.Path]::GetTempPath()) `
    ('marvel-windows-community-' + [guid]::NewGuid().ToString('N'))
$privateCertificate = $null
$publicStoreCertificate = $null
$trustedAdded = $false
New-Item -ItemType Directory -Path $scratch | Out-Null
try {
    $payload = Join-Path $scratch 'payload'
    Invoke-Checked { & $makeAppx unpack /p $InputPackage /d $payload /o }
    if (Test-Path -LiteralPath (Join-Path $payload 'AppxSignature.p7x')) {
        throw 'community signing accepts only unsigned package inputs'
    }
    Assert-PayloadHashes $payload

    [xml]$manifest = Get-Content -LiteralPath (Join-Path $payload 'AppxManifest.xml')
    $identity = $manifest.Package.Identity
    if ($identity.Name -cne 'mggarofalo.MarvelChampions' -or
        $identity.Publisher -cne $Publisher) {
        throw 'MSIX identity or publisher does not match the community release identity'
    }
    $releaseIdentity = Get-Content -LiteralPath (Join-Path $payload 'release-manifest.json') -Raw |
        ConvertFrom-Json
    if ($releaseIdentity.channel -notin @('preview', 'stable')) {
        throw 'only preview or stable inputs may enter community signing'
    }

    New-Item -ItemType Directory -Path $output | Out-Null
    $version = [string]$releaseIdentity.product_version
    $package = Join-Path $output "MarvelChampions-$version-windows-x64-community.msix"
    Copy-Item -LiteralPath $InputPackage -Destination $package

    # This key exists only in the runner's CurrentUser store, is explicitly
    # non-exportable, and is removed in finally. Only the public certificate is
    # a release artifact.
    $privateCertificate = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject $Publisher `
        -CertStoreLocation 'Cert:\CurrentUser\My' `
        -HashAlgorithm SHA256 `
        -KeyAlgorithm RSA `
        -KeyLength 3072 `
        -KeyExportPolicy NonExportable `
        -NotAfter (Get-Date).AddYears(10)
    if ($privateCertificate.Subject -cne $Publisher -or -not $privateCertificate.HasPrivateKey) {
        throw 'ephemeral certificate does not match the package publisher'
    }

    $publicCertificate = Join-Path $output `
        "MarvelChampions-$version-windows-x64-community.cer"
    Export-Certificate -Cert $privateCertificate -FilePath $publicCertificate -Type CERT |
        Out-Null
    $publicStoreCertificate = [Security.Cryptography.X509Certificates.X509Certificate2]::new(
        $publicCertificate)
    Add-PublicCertificateToStore $publicStoreCertificate TrustedPeople
    $trustedAdded = $true
    Write-Output 'temporary package trust installed'

    # The input was proven signature-free above. This exact invocation creates
    # its only signature and has no timestamp option or later timestamp operation.
    Invoke-Checked {
        & $signTool sign /fd SHA256 /sha1 $privateCertificate.Thumbprint $package
    }
    $verification = @(& $signTool verify /pa /all $package 2>&1)
    $verificationExit = $LASTEXITCODE
    $verification | ForEach-Object { Write-Output $_ }
    $verificationText = $verification -join "`n"
    $expectedUntrusted = $verificationText -match `
        '(?s)certificate chain processed.*terminated in a root\s+certificate which is not trusted by the trust provider'
    if ($verificationExit -ne 1 -or -not $expectedUntrusted) {
        throw 'package did not produce the expected self-signed trust verdict'
    }
    Write-Output 'expected self-signed untrusted-root verdict verified'

    $verified = Join-Path $scratch 'verified'
    Invoke-Checked { & $makeAppx unpack /p $package /d $verified /o }
    Assert-PayloadHashes $verified
    if (-not (Test-Path -LiteralPath (Join-Path $verified 'AppxSignature.p7x'))) {
        throw 'signed package has no package signature'
    }
    Write-Output 'signed payload integrity verified'

    $inputHash = (Get-FileHash $InputPackage -Algorithm SHA256).Hash.ToLowerInvariant()
    $packageHash = (Get-FileHash $package -Algorithm SHA256).Hash.ToLowerInvariant()
    $certificateHash = (Get-FileHash $publicCertificate -Algorithm SHA256).Hash.ToLowerInvariant()
    foreach ($artifact in @($package, $publicCertificate)) {
        $hash = (Get-FileHash $artifact -Algorithm SHA256).Hash.ToLowerInvariant()
        [IO.File]::WriteAllText(
            "$artifact.sha256",
            "$hash *$(Split-Path -Leaf $artifact)`n",
            [Text.UTF8Encoding]::new($false))
    }
    $provenance = [ordered]@{
        format = 'marvel-desktop-community-signing'
        schema = 1
        product_version = $version
        commit = $releaseIdentity.commit
        trust = 'self-signed-public-certificate-required'
        timestamp = 'none'
        publisher = $Publisher
        unsigned_input_sha256 = $inputHash
        artifact_sha256 = $packageHash
        public_certificate_sha256 = $certificateHash
    } | ConvertTo-Json
    [IO.File]::WriteAllText(
        "$package.provenance.json",
        "$provenance`n",
        [Text.UTF8Encoding]::new($false))
    Write-Output $package
}
finally {
    $cleanupFailures = @()
    if ($trustedAdded) {
        try {
            Write-Output 'removing temporary package trust'
            Remove-PublicCertificateFromStore $privateCertificate.Thumbprint TrustedPeople
            Write-Output 'temporary package trust removed'
        }
        catch {
            $cleanupFailures += 'temporary package trust cleanup failed'
        }
    }
    if ($null -ne $privateCertificate) {
        try {
            Write-Output 'deleting ephemeral signing key'
            $privatePath = "Cert:\CurrentUser\My\$($privateCertificate.Thumbprint)"
            $privateCertificate.Dispose()
            $privateCertificate = $null
            Remove-Item $privatePath -DeleteKey -Confirm:$false -ErrorAction Stop
            if (Test-Path $privatePath) { throw 'retained' }
            Write-Output 'ephemeral signing key deleted'
        }
        catch {
            $cleanupFailures += 'ephemeral signing key cleanup failed'
        }
    }
    if ($null -ne $publicStoreCertificate) {
        $publicStoreCertificate.Dispose()
    }
    Remove-Item -LiteralPath $scratch -Recurse -Force -ErrorAction SilentlyContinue
    if ($cleanupFailures.Count -gt 0) {
        throw ($cleanupFailures -join '; ')
    }
    Write-Output 'temporary certificate trust and private key removed'
}
