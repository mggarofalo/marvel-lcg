function Read-NativeManifest {
    param([string]$Path, [string]$GodotRoot)
    $manifest = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json -AsHashtable
    if ($manifest.schema -ne 1) { throw 'Unsupported native manifest schema.' }
    $root = [IO.Path]::GetFullPath($GodotRoot) + [IO.Path]::DirectorySeparatorChar
    $ids = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($entry in $manifest.cases) {
        if ($entry.id -notmatch '^[a-z0-9]+(?:-[a-z0-9]+)*$' -or -not $ids.Add($entry.id)) { throw "Invalid or duplicate native case: $($entry.id)" }
        if ($entry.markers.Count -eq 0 -or @($entry.markers | Where-Object { [string]::IsNullOrWhiteSpace($_) }).Count -gt 0) { throw "Missing native completion marker: $($entry.id)" }
        if ($entry.timeoutSeconds -le 0 -or $entry.timeoutSeconds -gt 600) { throw "Invalid native deadline: $($entry.id)" }
        if ($entry.scale -notin (50..150 | Where-Object { $_ % 10 -eq 0 })) { throw "Unsupported native scale: $($entry.id)" }
        if ($entry.players -notin @(1, 2) -or $entry.motion -notin @('enabled', 'disabled')) { throw "Invalid native input profile: $($entry.id)" }
        if ($entry.viewport -notmatch '^(\d+)x(\d+)$' -or [int]$Matches[1] -lt 1920 -or [int]$Matches[2] -lt 1080) { throw "Unsupported native viewport: $($entry.id)" }
        if ($entry.profiles.Count -eq 0 -or @($entry.profiles | Where-Object { $_ -notin @('representative', 'exhaustive', 'inspection-all-scales') }).Count -gt 0) { throw "Unknown native profile: $($entry.id)" }
        if (-not $entry.script.StartsWith('res://', [StringComparison]::Ordinal)) { throw "Invalid native script URI: $($entry.id)" }
        $script = [IO.Path]::GetFullPath((Join-Path $GodotRoot $entry.script.Substring(6)))
        if (-not $script.StartsWith($root, [StringComparison]::OrdinalIgnoreCase) -or -not (Test-Path -LiteralPath $script -PathType Leaf)) { throw "Native script escapes its project or does not exist: $($entry.id)" }
        if ($entry.fixture -and -not $manifest.fixtures.ContainsKey($entry.fixture)) { throw "Unknown native fixture: $($entry.id)" }
        if (-not $entry.fixture -and @($entry.arguments | Where-Object { $_ -and $_.Contains('{fixture}') }).Count -gt 0) { throw "Native fixture placeholder has no provider: $($entry.id)" }
    }
    foreach ($fixture in $manifest.fixtures.Values) {
        if ($fixture.file -match '[/\\:]' -or $fixture.file -in @('.', '..') -or
            $fixture.file -ne [IO.Path]::GetFileName($fixture.file) -or [string]::IsNullOrWhiteSpace($fixture.file)) { throw 'Native fixture must use a leaf filename.' }
        if ($fixture.environment -notmatch '^MARVEL_[A-Z0-9_]+$' -or [string]::IsNullOrWhiteSpace($fixture.testFilter)) { throw 'Invalid native fixture provider.' }
        if ($fixture.timeoutSeconds -le 0 -or $fixture.timeoutSeconds -gt 600) { throw 'Invalid native fixture deadline.' }
    }
    return $manifest
}

function Select-NativeShard {
    param([object[]]$Cases, [int]$ShardIndex, [int]$ShardCount)
    if ($ShardCount -lt 1 -or $ShardIndex -lt 0 -or $ShardIndex -ge $ShardCount) {
        throw 'Native shard requires count >= 1 and 0 <= index < count.'
    }
    # The unsharded hook keeps manifest order. Shards use ordinal ID order on every OS.
    if ($ShardCount -eq 1) { return $Cases }
    $byId = @{}
    foreach ($entry in $Cases) { $byId[$entry.id] = $entry }
    [string[]]$ids = @($Cases | ForEach-Object { $_.id })
    [Array]::Sort($ids, [StringComparer]::Ordinal)
    for ($index = 0; $index -lt $ids.Length; $index++) {
        if ($index % $ShardCount -eq $ShardIndex) { $byId[$ids[$index]] }
    }
}

function Resolve-NativeArguments {
    param([hashtable]$Entry, [hashtable]$FixturePaths)
    foreach ($argument in $Entry.arguments) {
        if ($argument.Contains('{fixture}')) {
            $argument.Replace('{fixture}', [string]$FixturePaths[$Entry.fixture])
        } else {
            $argument
        }
    }
}

function Reset-NativeFixtureOutput {
    param([string]$Path)
    if (Test-Path -LiteralPath $Path -PathType Container) { throw 'Native fixture output is a directory.' }
    if (Test-Path -LiteralPath $Path -PathType Leaf) { Remove-Item -LiteralPath $Path -Force }
}
