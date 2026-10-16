function Assert-NativeEvidenceFresh {
    param([string]$Directory)
    if (Test-Path -LiteralPath (Join-Path $Directory 'results.jsonl')) {
        throw 'Native evidence already contains results.jsonl; choose a fresh results directory.'
    }
}

function Test-NativeCompletion {
    param([string]$Output, [string]$Marker)
    foreach ($raw in ($Output -split '\r?\n')) {
        $line = $raw.Trim()
        if ($Marker -eq '"operation":"resolve"') {
            if (-not $line.StartsWith('{', [StringComparison]::Ordinal)) { continue }
            try { $record = $line | ConvertFrom-Json -AsHashtable }
            catch { continue }
            if ($record.event_id -eq 'server.request.completed' -and $record.operation -eq 'resolve' -and
                $record.disposition -eq 'accepted' -and $record.save_committed -eq $true) { return $true }
            continue
        }
        $prefix = $Marker.TrimEnd()
        if ($line -eq $prefix -or $line.StartsWith($prefix + ' ', [StringComparison]::Ordinal)) { return $true }
    }
    return $false
}
