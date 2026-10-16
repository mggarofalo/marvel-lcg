param([Parameter(Mandatory = $true)][string]$GodotBin, [switch]$AllScales, [switch]$NoBuild)
$profile = if ($AllScales) { 'inspection-all-scales' } else { 'exhaustive' }
& "$PSScriptRoot/native-cases.ps1" -GodotBin $GodotBin -Profile $profile -Case 'card-inspection-*' -NoBuild:$NoBuild
