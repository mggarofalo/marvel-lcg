param(
    [Parameter(Mandatory = $false)]
    [string]$GodotBin = $env:GODOT_BIN,
    [Parameter(Mandatory = $false)]
    [string]$CaptureDir = $env:MARVEL_SMOKE_CAPTURE_DIR
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
. "$PSScriptRoot/godot-smoke-diagnostics.ps1"
if ([string]::IsNullOrWhiteSpace($GodotBin)) {
    throw "Set GODOT_BIN or pass -GodotBin with the Godot 4.7 .NET executable."
}
if ([string]::IsNullOrWhiteSpace($CaptureDir)) {
    $CaptureDir = Join-Path ([IO.Path]::GetTempPath()) ("marvel-visual-qa-" + [guid]::NewGuid())
}

$version = & $GodotBin --version
if ($LASTEXITCODE -ne 0 -or -not $version.StartsWith("4.7.")) {
    throw "Godot 4.7 .NET is required; found: $version"
}

dotnet build "$repoRoot/src/Marvel.Godot/Marvel.Godot.csproj" --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
New-Item -ItemType Directory -Force -Path $CaptureDir | Out-Null
function Invoke-VisualSmoke {
    param([string]$Script = "res://smoke/local_game_smoke.gd", [string]$Marker = "LOCAL_GAME_SMOKE_OK")
    $output = & $GodotBin --audio-driver Dummy --rendering-method gl_compatibility `
        --resolution $env:MARVEL_SMOKE_VIEWPORT `
        --path "$repoRoot/src/Marvel.Godot" `
        --script $Script 2>&1
    $output | Write-Output
    if ($LASTEXITCODE -ne 0 -or (Test-GodotSmokeDiagnostics $output) -or
        -not (($output -join "`n").Contains($Marker))) {
        throw "Godot visual smoke reported an unexpected failure diagnostic."
    }
}
foreach ($viewport in @("1920x1080")) {
    foreach ($motion in @("enabled", "disabled")) {
        $env:MARVEL_UI_SCALE = "compact"
        $env:MARVEL_SMOKE_VIEWPORT = $viewport
        $env:MARVEL_SMOKE_MOTION = $motion
        $env:MARVEL_SMOKE_CAPTURE_DIR = $CaptureDir
        Invoke-VisualSmoke
    }
}

foreach ($scale in @(50, 80, 100, 150)) {
    $env:MARVEL_UI_SCALE = [string]$scale
    Invoke-VisualSmoke -Script "res://smoke/text_resolution_smoke.gd" -Marker "TEXT_RESOLUTION_OK"
}

Write-Output "Rendered visual checkpoints: $CaptureDir"
