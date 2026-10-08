param([Parameter(Mandatory = $true)][string]$GodotBin)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
. "$PSScriptRoot/godot-smoke-diagnostics.ps1"
$temporary = Join-Path ([IO.Path]::GetTempPath()) ("marvel-b1-sources-" + [guid]::NewGuid().ToString("N"))
[IO.Directory]::CreateDirectory($temporary) | Out-Null
$fixture = Join-Path $temporary "sources.json"
$log = Join-Path $temporary "godot.log"
$previousFixture = $env:MARVEL_B1_SOURCE_FIXTURE
$previousScale = $env:MARVEL_UI_SCALE
try {
    $env:MARVEL_B1_SOURCE_FIXTURE = $fixture
    dotnet test "$repoRoot/tests/Marvel.Godot.Tests/Marvel.Godot.Tests.csproj" -c Release --nologo `
        --filter FullyQualifiedName~CardSourceFixtureTests
    if ($LASTEXITCODE -ne 0) { throw "Could not project native source fixtures." }
    foreach ($scale in @("50", "80", "100", "150")) {
        $env:MARVEL_UI_SCALE = $scale
        $output = & $GodotBin --headless --audio-driver Dummy --log-file $log `
            --path "$repoRoot/src/Marvel.Godot" --script res://smoke/card_source_smoke.gd `
            -- "--marvel-b1-sources=$fixture" 2>&1
        $output | Write-Output
        if ($LASTEXITCODE -ne 0 -or (Test-GodotSmokeDiagnostics $output) `
            -or -not ($output -match "B1_SOURCES_OK")) {
            throw "Native B1 source fixtures failed at $scale%."
        }
    }
}
finally {
    $env:MARVEL_B1_SOURCE_FIXTURE = $previousFixture
    $env:MARVEL_UI_SCALE = $previousScale
    Remove-Item $temporary -Recurse -Force -ErrorAction SilentlyContinue
}
