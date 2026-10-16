$ErrorActionPreference = 'Stop'
$runner = Join-Path $PSScriptRoot 'native-cases.ps1'

function Read-NativeList {
    param([hashtable]$Arguments)
    $json = (& $runner -List @Arguments) -join "`n"
    return @($json | ConvertFrom-Json -AsHashtable)
}

function Assert-NativePartition {
    param([string]$Profile, [string[]]$Selectors)
    $arguments = @{ Profile = $Profile; Case = $Selectors }
    $whole = @(Read-NativeList $arguments)
    $expected = @{}
    foreach ($entry in $whole) { $expected[$entry.id] = ConvertTo-Json $entry -Depth 15 -Compress }
    [string[]]$orderedIds = @($whole | ForEach-Object { $_.id })
    [Array]::Sort($orderedIds, [StringComparer]::Ordinal)
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    for ($shard = 0; $shard -lt 2; $shard++) {
        $part = @(Read-NativeList ($arguments + @{ ShardCount = 2; ShardIndex = $shard }))
        if ($Selectors[0] -eq '*' -and $part.Count -eq 0) { throw 'A CI native shard is empty.' }
        $expectedIds = @(for ($index = $shard; $index -lt $orderedIds.Length; $index += 2) { $orderedIds[$index] })
        if ((ConvertTo-Json -InputObject @($part | ForEach-Object { $_.id }) -Compress) -cne
            (ConvertTo-Json -InputObject $expectedIds -Compress)) {
            throw 'Native shard membership or order differs from ordinal round-robin assignment.'
        }
        foreach ($entry in $part) {
            if (-not $seen.Add($entry.id)) { throw "Native shard overlap: $($entry.id)" }
            if (-not $expected.ContainsKey($entry.id) -or
                (ConvertTo-Json $entry -Depth 15 -Compress) -cne $expected[$entry.id]) {
                throw "Native shard changed an admitted case or its metadata: $($entry.id)"
            }
        }
    }
    if ($seen.Count -ne $whole.Count -or @($whole | Where-Object { -not $seen.Contains($_.id) }).Count -gt 0) {
        throw 'Native shard union differs from the complete selected profile.'
    }
    Write-Host "NATIVE_SHARD_UNION_OK profile=$Profile selectors=$($Selectors -join ',') cases=$($whole.Count)"
}

foreach ($profile in @('representative', 'exhaustive', 'inspection-all-scales')) {
    $whole = @(Read-NativeList @{ Profile = $profile })
    Assert-NativePartition $profile @('*')
    Assert-NativePartition $profile @($whole[0].id)
    Assert-NativePartition $profile @($whole[0].id, $whole[-1].id)
}

$singleCase = @(Read-NativeList @{ Profile = 'representative' })[0].id
$emptyRejected = $false
try {
    & $runner -Profile representative -Case $singleCase -ShardCount 2 -ShardIndex 1 -NoBuild
}
catch {
    if ($_.Exception.Message -cne 'No native cases in the selected shard.') { throw }
    $emptyRejected = $true
}
if (-not $emptyRejected) { throw 'An empty native execution shard reported success.' }

# The real entry point must validate selectors before slicing, including empty shard selections.
foreach ($invalid in @(
    @{ ShardCount = 0; ShardIndex = 0 },
    @{ ShardCount = 2; ShardIndex = -1 },
    @{ ShardCount = 2; ShardIndex = 2 },
    @{ ShardCount = 2; ShardIndex = 0; Case = @('missing-obligation') },
    @{ ShardCount = 2; ShardIndex = 1; Case = @('*', 'missing-obligation') }
)) {
    $expected = if ($invalid.ContainsKey('Case')) {
        if ($invalid.Case.Count -eq 1) { 'No native cases match this profile and selector.' }
        else { 'Unknown native case selector: missing-obligation' }
    } else { 'Native shard requires count >= 1 and 0 <= index < count.' }
    $rejected = $false
    try { Read-NativeList $invalid | Out-Null }
    catch {
        if ($_.Exception.Message -cne $expected) { throw "Unexpected native shard rejection: $($_.Exception.Message)" }
        $rejected = $true
    }
    if (-not $rejected) { throw 'Invalid native shard selection was admitted.' }
}

$resultGate = Join-Path $PSScriptRoot 'native-integration-result.ps1'
& $resultGate -PlatformGates success -NativeJourneys success
foreach ($state in @('failure', 'skipped', 'cancelled', '')) {
    foreach ($dependency in @('PlatformGates', 'NativeJourneys')) {
        $arguments = @{ PlatformGates = 'success'; NativeJourneys = 'success' }
        $arguments[$dependency] = $state
        $rejected = $false
        try { & $resultGate @arguments }
        catch {
            if (-not $_.Exception.Message.StartsWith('Native integration requires every platform gate and journey shard:', [StringComparison]::Ordinal)) { throw }
            $rejected = $true
        }
        if (-not $rejected) { throw "Native integration accepted $dependency with result '$state'." }
    }
}
Write-Host 'NATIVE_SHARD_TESTS_OK exact-unions=true metadata-preserved=true invalid-selectors-rejected=true aggregator-fails-closed=true'
