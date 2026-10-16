param([string]$PlatformGates, [string]$NativeJourneys)
$ErrorActionPreference = 'Stop'
if ($PlatformGates -cne 'success' -or $NativeJourneys -cne 'success') {
    throw "Native integration requires every platform gate and journey shard: gates=$PlatformGates journeys=$NativeJourneys"
}
Write-Host 'NATIVE_INTEGRATION_OK all-platform-gates=true all-journey-shards=true'
