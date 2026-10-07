param([Parameter(Mandatory = $true)][string]$GodotBin)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
. "$PSScriptRoot/godot-smoke-diagnostics.ps1"
$temporary = Join-Path ([IO.Path]::GetTempPath()) ("marvel-b1-faces-" + [guid]::NewGuid().ToString("N"))
[IO.Directory]::CreateDirectory($temporary) | Out-Null
$fixture = Join-Path $temporary "faces.json"
$log = Join-Path $temporary "godot.log"
$previousFixture = $env:MARVEL_B1_FACE_FIXTURE
$previousScale = $env:MARVEL_UI_SCALE
try {
    $env:MARVEL_B1_FACE_FIXTURE = $fixture
    dotnet test "$repoRoot/tests/Marvel.Godot.Tests/Marvel.Godot.Tests.csproj" -c Release --nologo `
        --filter FullyQualifiedName~CardFaceFixtureTests
    if ($LASTEXITCODE -ne 0) { throw "Could not project native card-face fixtures." }
    foreach ($scale in @("50", "80", "100", "150")) {
        $env:MARVEL_UI_SCALE = $scale
        $output = & $GodotBin --headless --audio-driver Dummy --log-file $log `
            --path "$repoRoot/src/Marvel.Godot" --script res://smoke/card_face_sample_smoke.gd `
            -- "--marvel-b1-faces=$fixture" 2>&1
        $output | Write-Output
        if ($LASTEXITCODE -ne 0 -or (Test-GodotSmokeDiagnostics $output) `
            -or -not ($output -match "B1_FACES_OK")) {
            throw "Native B1 card-face fixtures failed at $scale%."
        }
    }
}
finally {
    $env:MARVEL_B1_FACE_FIXTURE = $previousFixture
    $env:MARVEL_UI_SCALE = $previousScale
    Remove-Item $temporary -Recurse -Force -ErrorAction SilentlyContinue
}
