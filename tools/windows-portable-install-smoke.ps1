[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Archive
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ([string]::IsNullOrWhiteSpace($env:MARVEL_ENGINE_ENDPOINT)) {
    throw 'MARVEL_ENGINE_ENDPOINT must name the disposable test server'
}

$archivePath = (Resolve-Path -LiteralPath $Archive).Path
$hashFile = "$archivePath.sha256"
if (-not (Test-Path -LiteralPath $hashFile -PathType Leaf)) {
    throw 'portable artifact hash file was not found'
}
$line = [IO.File]::ReadAllText($hashFile).Trim()
if ($line -cnotmatch '^(?<hash>[0-9a-f]{64}) \*(?<name>[^\\/]+)$' -or
    $Matches['name'] -cne (Split-Path -Leaf $archivePath)) {
    throw 'portable artifact hash file is malformed or names another file'
}
$actual = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actual -cne $Matches['hash']) { throw 'portable artifact hash mismatch' }

$installRoot = Join-Path $env:RUNNER_TEMP "marvel-portable-$([Guid]::NewGuid())"
$stdout = Join-Path $env:RUNNER_TEMP "marvel-portable-$([Guid]::NewGuid()).out"
$stderr = Join-Path $env:RUNNER_TEMP "marvel-portable-$([Guid]::NewGuid()).err"
try {
    Expand-Archive -LiteralPath $archivePath -DestinationPath $installRoot
    $executable = Join-Path $installRoot 'MarvelChampions.exe'
    if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
        throw 'portable application executable is absent'
    }
    $process = Start-Process -FilePath $executable `
        -ArgumentList '--script', 'res://smoke/hosted_multiplayer_smoke.gd' `
        -RedirectStandardOutput $stdout `
        -RedirectStandardError $stderr `
        -PassThru
    if (-not $process.WaitForExit(120000)) {
        Stop-Process -Id $process.Id -Force
        throw 'portable application game smoke timed out'
    }
    if ($process.ExitCode -ne 0 -or
        -not (Select-String -LiteralPath $stdout -SimpleMatch 'HOSTED_MULTIPLAYER_SMOKE_OK')) {
        Get-Content -LiteralPath $stdout, $stderr -ErrorAction SilentlyContinue
        throw 'portable application did not complete its hosted game smoke'
    }
}
finally {
    Remove-Item -LiteralPath $installRoot -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $stdout, $stderr -Force -ErrorAction SilentlyContinue
}
if (Test-Path -LiteralPath $installRoot) { throw 'portable application removal failed' }
Write-Output 'WINDOWS_PORTABLE_INSTALL_SMOKE_OK'
