[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$InputPackage,
    [Parameter(Mandatory = $true)][string]$Output,
    [Parameter(Mandatory = $true)][string]$Publisher,
    [Parameter(Mandatory = $true)][string]$SigningThumbprint,
    [Parameter(Mandatory = $true)][string]$TimestampUrl
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
    $tool = Join-Path ${env:ProgramFiles(x86)} `
        "Windows Kits\10\bin\$WindowsSdkVersion\x64\$Name"
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

if (-not (Test-Path -LiteralPath $InputPackage -PathType Leaf)) {
    throw 'unsigned MSIX input was not found'
}
if ($SigningThumbprint -cnotmatch '^[0-9a-fA-F]{40,64}$') {
    throw 'signing thumbprint is malformed'
}
if (-not [Uri]::IsWellFormedUriString($TimestampUrl, [UriKind]::Absolute) -or
    ([Uri]$TimestampUrl).Scheme -ne 'https') {
    throw 'timestamp URL must be absolute HTTPS'
}
$certificate = Get-ChildItem -Path Cert:\CurrentUser\My\$SigningThumbprint -ErrorAction Stop
if ($certificate.Subject -cne $Publisher) {
    throw 'signing certificate subject does not match Publisher'
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
    ('marvel-windows-sign-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $scratch | Out-Null
try {
    $payload = Join-Path $scratch 'payload'
    Invoke-Checked { & $makeAppx unpack /p $InputPackage /d $payload /o }
    if (Test-Path -LiteralPath (Join-Path $payload 'AppxSignature.p7x')) {
        throw 'protected signing accepts only unsigned package inputs'
    }
    Assert-PayloadHashes $payload

    [xml]$manifest = Get-Content -LiteralPath (Join-Path $payload 'AppxManifest.xml')
    $identity = $manifest.Package.Identity
    if ($identity.Name -cne 'mggarofalo.MarvelChampions' -or
        $identity.Publisher -cne $Publisher) {
        throw 'MSIX identity or publisher does not match the protected release identity'
    }
    $releaseIdentity = Get-Content -LiteralPath (Join-Path $payload 'release-manifest.json') -Raw |
        ConvertFrom-Json
    if ($releaseIdentity.channel -notin @('preview', 'stable')) {
        throw 'only preview or stable inputs may enter protected signing'
    }

    foreach ($footprint in @(
        'AppxBlockMap.xml',
        '[Content_Types].xml',
        'AppxSignature.p7x')) {
        Remove-Item -LiteralPath (Join-Path $payload $footprint) -Force -ErrorAction SilentlyContinue
    }
    Remove-Item -LiteralPath (Join-Path $payload 'AppxMetadata') `
        -Recurse -Force -ErrorAction SilentlyContinue

    $signArguments = @(
        'sign', '/fd', 'SHA256', '/sha1', $SigningThumbprint,
        '/tr', $TimestampUrl, '/td', 'SHA256')
    foreach ($binary in Get-ChildItem -LiteralPath $payload -Recurse -File |
        Where-Object { $_.Extension -in @('.exe', '.dll') }) {
        Invoke-Checked { & $signTool @signArguments $binary.FullName }
        Invoke-Checked { & $signTool verify /pa /all $binary.FullName }
    }

    $payloadHashFile = Join-Path $payload 'payload-sha256.txt'
    $hashLines = Get-ChildItem -LiteralPath $payload -Recurse -File |
        Where-Object { $_.FullName -cne $payloadHashFile } |
        Sort-Object FullName |
        ForEach-Object {
            $relative = [IO.Path]::GetRelativePath($payload, $_.FullName).Replace('\', '/')
            '{0} *{1}' -f (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $relative
        }
    [IO.File]::WriteAllLines($payloadHashFile, $hashLines, [Text.UTF8Encoding]::new($false))

    New-Item -ItemType Directory -Path $output | Out-Null
    $version = [string]$releaseIdentity.product_version
    $package = Join-Path $output "MarvelChampions-$version-windows-x64.msix"
    Invoke-Checked { & $makeAppx pack /d $payload /p $package /o /h SHA256 }
    Invoke-Checked { & $signTool @signArguments $package }
    Invoke-Checked { & $signTool verify /pa /all $package }

    $verified = Join-Path $scratch 'verified'
    Invoke-Checked { & $makeAppx unpack /p $package /d $verified /o }
    Assert-PayloadHashes $verified
    if (-not (Test-Path -LiteralPath (Join-Path $verified 'AppxSignature.p7x'))) {
        throw 'signed package has no package signature'
    }

    $inputHash = (Get-FileHash $InputPackage -Algorithm SHA256).Hash.ToLowerInvariant()
    $packageHash = (Get-FileHash $package -Algorithm SHA256).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText(
        "$package.sha256",
        "$packageHash *$(Split-Path -Leaf $package)`n",
        [Text.UTF8Encoding]::new($false))
    $provenance = [ordered]@{
        format = 'marvel-desktop-signing'
        schema = 1
        product_version = $version
        commit = $releaseIdentity.commit
        unsigned_input_sha256 = $inputHash
        signed_artifact_sha256 = $packageHash
    } | ConvertTo-Json
    [IO.File]::WriteAllText(
        "$package.provenance.json",
        "$provenance`n",
        [Text.UTF8Encoding]::new($false))
    Write-Output $package
}
finally {
    Remove-Item -LiteralPath $scratch -Recurse -Force -ErrorAction SilentlyContinue
}
