param(
    [Parameter(Mandatory = $false)]
    [string]$GodotBin = $env:GODOT_BIN
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($GodotBin)) {
    throw "Set GODOT_BIN or pass -GodotBin with the Godot 4.7 .NET executable."
}

$version = & $GodotBin --version
if ($LASTEXITCODE -ne 0 -or -not $version.StartsWith("4.7.")) {
    throw "Godot 4.7 .NET is required; found: $version"
}

dotnet build "$repoRoot/src/Marvel.Godot/Marvel.Godot.csproj" --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

foreach ($viewport in @("1280x720", "1600x900")) {
    foreach ($scale in @("standard", "large", "extra-large")) {
        $env:MARVEL_UI_SCALE = $scale
        $env:MARVEL_SMOKE_VIEWPORT = $viewport
        & $GodotBin --headless --path "$repoRoot/src/Marvel.Godot" `
            --script res://smoke/local_game_smoke.gd
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
}
exit 0
