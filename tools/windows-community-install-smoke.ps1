[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Package,
    [Parameter(Mandatory = $true)][string]$Certificate,
    [ValidateRange(1, 300)][int]$SmokeSeconds = 8,
    [switch]$Interactive
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$PackageName = 'mggarofalo.MarvelChampions'
$Publisher = 'CN=Marvel Champions Community'

function Assert-Administrator {
    $principal = [Security.Principal.WindowsPrincipal]::new(
        [Security.Principal.WindowsIdentity]::GetCurrent())
    if (-not $principal.IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'the Windows community install smoke requires an elevated PowerShell window'
    }
}

function Assert-ArtifactHash {
    param([Parameter(Mandatory = $true)][string]$Path)
    $hashFile = "$Path.sha256"
    if (-not (Test-Path -LiteralPath $hashFile -PathType Leaf)) {
        throw "artifact hash file was not found: $hashFile"
    }
    $line = [IO.File]::ReadAllText($hashFile).Trim()
    if ($line -cnotmatch '^(?<hash>[0-9a-f]{64}) \*(?<name>[^\\/]+)$') {
        throw "artifact hash file is malformed: $hashFile"
    }
    if ($Matches['name'] -cne (Split-Path -Leaf $Path)) {
        throw "artifact hash names a different file: $hashFile"
    }
    $actual = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -cne $Matches['hash']) {
        throw "artifact hash mismatch: $Path"
    }
}

Assert-Administrator
$packagePath = (Resolve-Path -LiteralPath $Package).Path
$certificatePath = (Resolve-Path -LiteralPath $Certificate).Path
Assert-ArtifactHash $packagePath
Assert-ArtifactHash $certificatePath

$publicCertificate = [Security.Cryptography.X509Certificates.X509Certificate2]::new(
    $certificatePath)
if ($publicCertificate.Subject -cne $Publisher -or $publicCertificate.HasPrivateKey) {
    throw 'the public certificate does not match the community package publisher'
}
$thumbprint = $publicCertificate.Thumbprint

if (Get-AppxPackage -Name $PackageName) {
    throw 'the community package is already installed; use a disposable clean account'
}
if (Test-Path -LiteralPath "Cert:\LocalMachine\TrustedPeople\$thumbprint") {
    throw 'the release certificate is already trusted; use a disposable clean system'
}

$installedPackage = $null
$launchedProcess = $null
$trustInstalled = $false
try {
    Import-Certificate `
        -FilePath $certificatePath `
        -CertStoreLocation 'Cert:\LocalMachine\TrustedPeople' | Out-Null
    $trustInstalled = $true

    Add-AppxPackage -Path $packagePath
    $installedPackage = Get-AppxPackage -Name $PackageName
    if ($null -eq $installedPackage -or
        $installedPackage.Publisher -cne $Publisher) {
        throw 'the installed package identity does not match the community artifact'
    }

    $applicationData = Join-Path $env:LOCALAPPDATA `
        "Packages\$($installedPackage.PackageFamilyName)"
    $priorProcesses = @(Get-Process MarvelChampions -ErrorAction SilentlyContinue).Id
    Start-Process explorer.exe `
        "shell:AppsFolder\$($installedPackage.PackageFamilyName)!MarvelChampions"
    Start-Sleep -Seconds $SmokeSeconds
    $launchedProcess = Get-Process MarvelChampions -ErrorAction SilentlyContinue |
        Where-Object Id -NotIn $priorProcesses |
        Select-Object -First 1
    if ($null -eq $launchedProcess -or $launchedProcess.HasExited) {
        throw 'the packaged entry point did not remain running for the smoke interval'
    }

    Write-Output "installed $($installedPackage.PackageFullName)"
    Write-Output "launched process $($launchedProcess.Id)"
    if ($Interactive) {
        Read-Host 'Complete the bounded game smoke, close the client, then press Enter'
    }
}
finally {
    $cleanupFailures = @()
    if ($null -ne $launchedProcess -and -not $launchedProcess.HasExited) {
        try {
            Stop-Process -Id $launchedProcess.Id -Force -ErrorAction Stop
            $launchedProcess.WaitForExit()
        }
        catch {
            $cleanupFailures += 'packaged process cleanup failed'
        }
    }
    if ($null -ne $installedPackage) {
        try {
            Remove-AppxPackage -Package $installedPackage.PackageFullName -ErrorAction Stop
        }
        catch {
            $cleanupFailures += 'package cleanup failed'
        }
    }
    if ($trustInstalled) {
        try {
            Remove-Item `
                -LiteralPath "Cert:\LocalMachine\TrustedPeople\$thumbprint" `
                -Force `
                -ErrorAction Stop
        }
        catch {
            $cleanupFailures += 'certificate trust cleanup failed'
        }
    }

    if (Get-AppxPackage -Name $PackageName) {
        $cleanupFailures += 'package registration was retained'
    }
    if (Test-Path -LiteralPath "Cert:\LocalMachine\TrustedPeople\$thumbprint") {
        $cleanupFailures += 'certificate trust was retained'
    }
    if ($null -ne $installedPackage -and
        (Test-Path -LiteralPath $applicationData)) {
        $cleanupFailures += 'package application data was retained'
    }
    $publicCertificate.Dispose()

    if ($cleanupFailures.Count -gt 0) {
        throw ($cleanupFailures -join '; ')
    }
}

Write-Output 'WINDOWS_COMMUNITY_INSTALL_SMOKE_OK'
