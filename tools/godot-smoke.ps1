param([string]$GodotBin = $env:GODOT_BIN, [switch]$Representative,
    [string[]]$Case = @('*'), [switch]$List, [switch]$NoBuild, [string]$ResultsDirectory)
$profile = if ($Representative) { 'representative' } else { 'exhaustive' }
& "$PSScriptRoot/native-cases.ps1" -GodotBin $GodotBin -Profile $profile -Case $Case -List:$List -NoBuild:$NoBuild -ResultsDirectory $ResultsDirectory
