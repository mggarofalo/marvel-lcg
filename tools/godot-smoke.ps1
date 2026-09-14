param(
    [Parameter(Mandatory = $false)]
    [string]$GodotBin = $env:GODOT_BIN,
    [Parameter(Mandatory = $false)]
    [switch]$Representative
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
. "$PSScriptRoot/godot-smoke-diagnostics.ps1"
if ([string]::IsNullOrWhiteSpace($GodotBin)) {
    throw "Set GODOT_BIN or pass -GodotBin with the Godot 4.7 .NET executable."
}

$version = & $GodotBin --version
if ($LASTEXITCODE -ne 0 -or -not $version.StartsWith("4.7.")) {
    throw "Godot 4.7 .NET is required; found: $version"
}

dotnet build "$repoRoot/src/Marvel.Godot/Marvel.Godot.csproj" --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

function Invoke-LocalSmoke {
    $output = & $GodotBin --headless --audio-driver Dummy --path "$repoRoot/src/Marvel.Godot" `
        --script res://smoke/local_game_smoke.gd 2>&1
    $output | Write-Output
    if ($LASTEXITCODE -ne 0 -or (Test-GodotSmokeDiagnostics $output)) {
        throw "Godot local smoke reported an unexpected failure diagnostic."
    }
}

$viewports = if ($Representative) {
    @("1280x720")
}
else {
    @("1040x680", "1280x720", "1600x900", "1920x1080")
}
$scales = if ($Representative) {
    @("100")
}
else {
    @("50", "60", "70", "80", "90", "100", "110", "120", "130", "140", "150")
}

foreach ($viewport in $viewports) {
    foreach ($scale in $scales) {
        $env:MARVEL_UI_SCALE = $scale
        $env:MARVEL_SMOKE_VIEWPORT = $viewport
        $env:MARVEL_SMOKE_MOTION = "enabled"
        Invoke-LocalSmoke
    }
}
$env:MARVEL_UI_SCALE = "100"
$env:MARVEL_SMOKE_VIEWPORT = "1280x720"
$env:MARVEL_SMOKE_MOTION = "disabled"
Invoke-LocalSmoke
$env:MARVEL_UI_SCALE = "100"
$env:MARVEL_SMOKE_VIEWPORT = "1920x1080"
$env:MARVEL_SMOKE_MOTION = "enabled"
$env:MARVEL_SMOKE_TWO_PLAYER = "true"
Invoke-LocalSmoke
Remove-Item Env:MARVEL_SMOKE_TWO_PLAYER -ErrorAction SilentlyContinue
exit 0
