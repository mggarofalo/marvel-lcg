param([Parameter(Mandatory = $true)][string]$GodotBin)
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
. "$PSScriptRoot/godot-smoke-diagnostics.ps1"
$temporary = Join-Path ([IO.Path]::GetTempPath()) ("marvel-legal-tableau-" + [guid]::NewGuid().ToString("N"))
[IO.Directory]::CreateDirectory($temporary) | Out-Null
$fixture = Join-Path $temporary "tableau.json"
$previousFixture = $env:MARVEL_LEGAL_TABLEAU_FIXTURE
$previousScale = $env:MARVEL_UI_SCALE
try {
    $env:MARVEL_LEGAL_TABLEAU_FIXTURE = $fixture
    dotnet test "$repoRoot/tests/Marvel.Godot.Tests/Marvel.Godot.Tests.csproj" -c Release --nologo `
        --filter FullyQualifiedName~LegalIronManTableauTests
    if ($LASTEXITCODE -ne 0) { throw "Could not project reachable Iron Man tableau." }
    $env:MARVEL_UI_SCALE = "150"
    $output = & $GodotBin --headless --audio-driver Dummy --path "$repoRoot/src/Marvel.Godot" `
        --script res://smoke/legal_iron_man_tableau_smoke.gd -- "--marvel-tableau=$fixture" --marvel-tableau-legal 2>&1
    $output | Write-Output
    if ($LASTEXITCODE -ne 0 -or (Test-GodotSmokeDiagnostics $output) `
        -or -not ($output -match "LEGAL_IRON_MAN_TABLEAU_OK")) {
        throw "Reachable Iron Man tableau failed. Diagnostics retained in $temporary."
    }
}
finally {
    $env:MARVEL_LEGAL_TABLEAU_FIXTURE = $previousFixture
    $env:MARVEL_UI_SCALE = $previousScale
}
