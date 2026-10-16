param([Parameter(Mandatory = $true)][string]$GodotBin, [switch]$NoBuild)
& "$PSScriptRoot/native-cases.ps1" -GodotBin $GodotBin -NoBuild:$NoBuild -Case @('source-tableau-*', 'source-vacancy-*', 'source-drag-hit-*', 'source-weighted-gesture-*')
