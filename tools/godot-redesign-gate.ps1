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
    $CaptureDir = Join-Path ([IO.Path]::GetTempPath()) ("marvel-redesign-gate-" + [guid]::NewGuid())
}

$version = & $GodotBin --version
if ($LASTEXITCODE -ne 0 -or -not $version.StartsWith("4.7.")) {
    throw "Godot 4.7 .NET is required; found: $version"
}

dotnet build "$repoRoot/src/Marvel.Godot/Marvel.Godot.csproj" --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
New-Item -ItemType Directory -Force -Path $CaptureDir | Out-Null

function Invoke-RedesignSmoke {
    param([string]$Scale, [bool]$TwoPlayer)

    $seat = if ($TwoPlayer) { "two-player" } else { "single-player" }
    $runCaptureDir = Join-Path $CaptureDir "$seat-$Scale"
    New-Item -ItemType Directory -Force -Path $runCaptureDir | Out-Null
    $env:MARVEL_REDESIGN_GATE = "true"
    $env:MARVEL_UI_SCALE = $Scale
    $env:MARVEL_SMOKE_VIEWPORT = "1920x1080"
    $env:MARVEL_SMOKE_MOTION = "enabled"
    $env:MARVEL_SMOKE_CAPTURE_DIR = $runCaptureDir
    if ($TwoPlayer) {
        $env:MARVEL_SMOKE_TWO_PLAYER = "true"
    } else {
        Remove-Item Env:MARVEL_SMOKE_TWO_PLAYER -ErrorAction SilentlyContinue
    }

    $output = & $GodotBin --audio-driver Dummy --rendering-method gl_compatibility `
        --resolution 1920x1080 --path "$repoRoot/src/Marvel.Godot" `
        --script res://smoke/local_game_smoke.gd 2>&1
    $output | Write-Output
    if ($LASTEXITCODE -ne 0 -or (Test-GodotSmokeDiagnostics $output)) {
        throw "Godot redesign gate failed for $seat at $Scale%."
    }
}

try {
	$failures = @()
    foreach ($twoPlayer in @($false, $true)) {
        foreach ($scale in @("100", "150")) {
			try {
				Invoke-RedesignSmoke -Scale $scale -TwoPlayer $twoPlayer
			} catch {
				$failures += $_.Exception.Message
				Write-Error $_.Exception.Message -ErrorAction Continue
			}
        }
    }
	if ($failures.Count -gt 0) {
		throw "Godot redesign gate failed in $($failures.Count) matrix case(s)."
	}
} finally {
    Remove-Item Env:MARVEL_REDESIGN_GATE -ErrorAction SilentlyContinue
    Remove-Item Env:MARVEL_SMOKE_TWO_PLAYER -ErrorAction SilentlyContinue
}

Write-Output "Rendered redesign checkpoints: $CaptureDir"
