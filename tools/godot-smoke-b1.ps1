param([Parameter(Mandatory = $true)][string]$GodotBin)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
. "$PSScriptRoot/godot-smoke-diagnostics.ps1"

& "$PSScriptRoot/godot-smoke-card-faces.ps1" -GodotBin $GodotBin
& "$PSScriptRoot/godot-smoke-live-state.ps1" -GodotBin $GodotBin
& "$PSScriptRoot/godot-smoke-sources.ps1" -GodotBin $GodotBin
& "$PSScriptRoot/godot-smoke-inspection.ps1" -GodotBin $GodotBin

$previousScale = $env:MARVEL_UI_SCALE
try {
    foreach ($scale in @("50", "80", "100", "150")) {
        $env:MARVEL_UI_SCALE = $scale
        $output = & $GodotBin --headless --audio-driver Dummy --path "$repoRoot/src/Marvel.Godot" `
            --script res://smoke/card_visual_sample_smoke.gd -- --marvel-b1-sample 2>&1
        $output | Write-Output
        if ($LASTEXITCODE -ne 0 -or (Test-GodotSmokeDiagnostics $output) `
            -or -not ($output -match "B1_PRIMITIVES_OK") `
            -or -not ($output -match "B1_PRIMITIVES_INPUT_OK")) {
            throw "Native B1 typography and resource glyphs failed at $scale%."
        }
    }
}
finally {
    $env:MARVEL_UI_SCALE = $previousScale
}
