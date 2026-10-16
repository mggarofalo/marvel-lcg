param([Parameter(Mandatory = $true)][string]$GodotBin, [switch]$NoBuild)
& "$PSScriptRoot/native-cases.ps1" -GodotBin $GodotBin -NoBuild:$NoBuild -Case @('legal-iron-man-tableau-*')
