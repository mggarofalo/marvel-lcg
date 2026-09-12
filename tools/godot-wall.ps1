[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$probes = Join-Path $repoRoot 'tests/godot-wall'
$log = [IO.Path]::GetTempFileName()
$failures = 0

function Test-GodotWall {
    param(
        [Parameter(Mandatory = $true)][string]$Description,
        [Parameter(Mandatory = $true)][string]$ExpectedVerdict,
        [Parameter(Mandatory = $true)][string]$Project
    )

    & dotnet build $Project --nologo --verbosity quiet *> $log
    $exitCode = $LASTEXITCODE

    if ($exitCode -eq 0) {
        if ($ExpectedVerdict -ceq 'pass') {
            Write-Host "  ok    $Description"
            return
        }

        Write-Host "  FAIL  ${Description}: the build succeeded, but $ExpectedVerdict had to stop it."
        $script:failures++
        return
    }

    if ($ExpectedVerdict -ceq 'pass') {
        Write-Host "  FAIL  ${Description}: the build failed and should not have."
        Get-Content -LiteralPath $log | ForEach-Object { Write-Host "        $_" }
        $script:failures++
        return
    }

    if (Select-String -LiteralPath $log -SimpleMatch "error $ExpectedVerdict" -Quiet) {
        Write-Host "  ok    $Description"
        return
    }

    Write-Host "  FAIL  ${Description}: the build failed, but not with $ExpectedVerdict."
    Get-Content -LiteralPath $log | ForEach-Object { Write-Host "        $_" }
    $script:failures++
}

Get-Command dotnet -ErrorAction Stop | Out-Null

try {
    Write-Host 'The Godot wall:'

    Test-GodotWall `
        -Description 'an engine-shaped project cannot opt out of the wall' `
        -ExpectedVerdict 'MARVELWALLOPT' `
        -Project (Join-Path $probes 'Marvel.WallProbe.OptOut/Marvel.WallProbe.OptOut.csproj')
    Test-GodotWall `
        -Description 'a transitive GodotSharp reference stops the build' `
        -ExpectedVerdict 'MARVELWALL' `
        -Project (Join-Path $probes 'Marvel.WallProbe/Marvel.WallProbe.csproj')
    Test-GodotWall `
        -Description 'the shared session journal stays below the wall' `
        -ExpectedVerdict 'pass' `
        -Project (Join-Path $probes 'Marvel.WallProbe.Session/Marvel.WallProbe.Session.csproj')
    Test-GodotWall `
        -Description 'shared presentation stays below the Godot implementation' `
        -ExpectedVerdict 'pass' `
        -Project (Join-Path $probes 'Marvel.WallProbe.Presentation/Marvel.WallProbe.Presentation.csproj')
    Test-GodotWall `
        -Description 'the presentation layer may opt out' `
        -ExpectedVerdict 'pass' `
        -Project (Join-Path $probes 'Marvel.WallProbe.Allowed/Marvel.WallProbe.Allowed.csproj')

    Write-Host 'The runtime floor:'

    Test-GodotWall `
        -Description 'a framework above the floor stops the build' `
        -ExpectedVerdict 'MARVELTFM' `
        -Project (Join-Path $probes 'Marvel.WallProbe.Future/Marvel.WallProbe.Future.csproj')

    if ($failures -ne 0) {
        Write-Host
        Write-Host "$failures of the wall's gates did not behave as specified."
        exit 1
    }

    Write-Host
    Write-Host "The wall holds, and every gate was watched firing."
}
finally {
    Remove-Item -LiteralPath $log -Force -ErrorAction SilentlyContinue
}
