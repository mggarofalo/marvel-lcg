param([Parameter(Mandatory = $true)][string]$GodotBin, [switch]$NoBuild)
& "$PSScriptRoot/native-cases.ps1" -GodotBin $GodotBin -NoBuild:$NoBuild -Case @('card-face-sample-*', 'card-live-state-*', 'card-source-*', 'card-inspection-*', 'card-visual-sample-*')
