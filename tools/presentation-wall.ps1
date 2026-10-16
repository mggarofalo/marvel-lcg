[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$probes = Join-Path $repoRoot 'tests/presentation-wall'
$log = [IO.Path]::GetTempFileName()
$failures = 0

function Test-PresentationWall {
    param(
        [Parameter(Mandatory = $true)][string]$Role,
        [Parameter(Mandatory = $true)][string]$ExpectedError,
        [Parameter(Mandatory = $true)][string]$Project
    )

    & dotnet build $Project --nologo --verbosity quiet *> $log
    $exitCode = $LASTEXITCODE

    if ($exitCode -eq 0) {
        Write-Host "  FAIL  $Role succeeded, but $ExpectedError had to stop it."
        $script:failures++
        return
    }

    if (Select-String -LiteralPath $log -SimpleMatch "error ${ExpectedError}:" -Quiet) {
        Write-Host "  ok    $Role fails with $ExpectedError"
        return
    }

    Write-Host "  FAIL  $Role failed, but not with $ExpectedError."
    Get-Content -LiteralPath $log | ForEach-Object { Write-Host "        $_" }
    $script:failures++
}

Get-Command dotnet -ErrorAction Stop | Out-Null

try {
    Write-Host 'The presentation project wall:'

    foreach ($role in @('View', 'Decisions', 'Client', 'Godot')) {
        & dotnet build (Join-Path $probes 'Marvel.PresentationProbe.Allowed/Marvel.PresentationProbe.Allowed.csproj') `
            "-p:MarvelAllowedProbeRole=$role" --nologo --verbosity quiet *> $log
        if ($LASTEXITCODE -eq 0) { Write-Host "  ok    $role consumes engine-owned target validation" }
        else {
            Write-Host "  FAIL  $role cannot consume its allowed contract"
            Get-Content -LiteralPath $log
            $script:failures++
        }
    }
    & dotnet build (Join-Path $probes 'Marvel.PresentationProbe.InfrastructureAllowed/Marvel.PresentationProbe.InfrastructureAllowed.csproj') `
        --nologo --verbosity quiet *> $log
    if ($LASTEXITCODE -eq 0) { Write-Host '  ok    infrastructure consumes transport without hosting state' }
    else {
        Write-Host '  FAIL  legitimate infrastructure could not compile'
        Get-Content -LiteralPath $log
        $script:failures++
    }

    Test-PresentationWall `
        -Role "View's forbidden engine reference" `
        -ExpectedError 'MARVELPRESENTATION' `
        -Project (Join-Path $probes 'Marvel.PresentationProbe.View/Marvel.PresentationProbe.View.csproj')
    Test-PresentationWall `
        -Role "Decisions' forbidden engine reference" `
        -ExpectedError 'MARVELPRESENTATION' `
        -Project (Join-Path $probes 'Marvel.PresentationProbe.Decisions/Marvel.PresentationProbe.Decisions.csproj')
    Test-PresentationWall `
        -Role "Client's forbidden engine reference" `
        -ExpectedError 'MARVELPRESENTATION' `
        -Project (Join-Path $probes 'Marvel.PresentationProbe.Client/Marvel.PresentationProbe.Client.csproj')
    Test-PresentationWall `
        -Role "Godot's forbidden engine reference" `
        -ExpectedError 'MARVELPRESENTATION' `
        -Project (Join-Path $probes 'Marvel.PresentationProbe.Godot/Marvel.PresentationProbe.Godot.csproj')
    Test-PresentationWall `
        -Role 'a presentation project cannot expose transitive references' `
        -ExpectedError 'MARVELPRESENTATIONCONFIG' `
        -Project (Join-Path $probes 'Marvel.PresentationProbe.Transitive/Marvel.PresentationProbe.Transitive.csproj')

    Test-PresentationWall -Role "Client cannot depend on authoritative host" -ExpectedError 'MARVELPRESENTATION' `
        -Project (Join-Path $probes 'Marvel.PresentationProbe.ClientHost/Marvel.PresentationProbe.ClientHost.csproj')
    Test-PresentationWall -Role "Infrastructure cannot transitively depend on host" -ExpectedError 'MARVELINFRASTRUCTURE' `
        -Project (Join-Path $probes 'Marvel.PresentationProbe.Infrastructure/Marvel.PresentationProbe.Infrastructure.csproj')

    if ($failures -ne 0) {
        Write-Host
        Write-Host "$failures presentation project gates did not behave as specified."
        exit 1
    }

    Write-Host
    Write-Host 'Allowed contracts compiled; every forbidden presentation dependency and configuration was rejected.'
}
finally {
    Remove-Item -LiteralPath $log -Force -ErrorAction SilentlyContinue
}

exit 0
