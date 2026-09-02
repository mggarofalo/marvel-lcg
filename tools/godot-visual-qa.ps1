param(
    [Parameter(Mandatory = $false)]
    [string]$GodotBin = $env:GODOT_BIN,
    [Parameter(Mandatory = $false)]
    [string]$CaptureDir = $env:MARVEL_SMOKE_CAPTURE_DIR
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
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
foreach ($motion in @("enabled", "disabled")) {
    $env:MARVEL_UI_SCALE = "standard"
    $env:MARVEL_SMOKE_VIEWPORT = "1280x720"
    $env:MARVEL_SMOKE_MOTION = $motion
    $env:MARVEL_SMOKE_CAPTURE_DIR = $CaptureDir
    & $GodotBin --rendering-method gl_compatibility `
        --path "$repoRoot/src/Marvel.Godot" `
        --script res://smoke/local_game_smoke.gd
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Output "Rendered visual checkpoints: $CaptureDir"
