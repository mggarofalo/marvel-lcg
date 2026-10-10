param([Parameter(Mandatory = $true)][string]$GodotBin)
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
. "$PSScriptRoot/godot-smoke-diagnostics.ps1"
$temporary = Join-Path ([IO.Path]::GetTempPath()) ("marvel-tableau-" + [guid]::NewGuid().ToString("N"))
[IO.Directory]::CreateDirectory($temporary) | Out-Null
$fixture = Join-Path $temporary "tableau.json"
$previousFixture = $env:MARVEL_TABLEAU_FIXTURE
$previousScale = $env:MARVEL_UI_SCALE
try {
    $env:MARVEL_TABLEAU_FIXTURE = $fixture
    dotnet test "$repoRoot/tests/Marvel.Godot.Tests/Marvel.Godot.Tests.csproj" -c Release --nologo `
        --filter FullyQualifiedName~SourceTableauTests
    if ($LASTEXITCODE -ne 0) { throw "Could not project native tableau fixtures." }
    foreach ($scale in @("100", "150")) {
        $env:MARVEL_UI_SCALE = $scale
        foreach ($layout in @("ordinary", "narrow")) {
            $arguments = @("--headless", "--audio-driver", "Dummy", "--path", "$repoRoot/src/Marvel.Godot",
                "--script", "res://smoke/source_tableau_smoke.gd", "--", "--marvel-tableau=$fixture")
            if ($layout -eq "narrow") { $arguments += "--marvel-tableau-narrow" }
            $output = & $GodotBin @arguments 2>&1
            $output | Write-Output
            if ($LASTEXITCODE -ne 0 -or (Test-GodotSmokeDiagnostics $output) `
                -or -not ($output -match "TABLEAU.*OK")) {
                throw "Native tableau fixture failed at $scale% / $layout. Diagnostics retained in $temporary."
            }
        }
    }
}
finally {
    $env:MARVEL_TABLEAU_FIXTURE = $previousFixture
    $env:MARVEL_UI_SCALE = $previousScale
}
