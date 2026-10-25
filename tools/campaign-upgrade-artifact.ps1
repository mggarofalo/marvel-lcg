function Assert-CampaignDesktopArtifact {
    param([string]$Archive, [string]$ExpectedHash, [string]$Executable)
    if ($ExpectedHash -cnotmatch '^[a-fA-F0-9]{64}$' -or
        (Get-FileHash -LiteralPath $Archive).Hash -ine $ExpectedHash) {
        throw 'Client artifact checksum mismatch.'
    }
    $payload = Split-Path ([IO.Path]::GetFullPath($Executable)) -Parent
    $zip = [IO.Compression.ZipFile]::OpenRead($Archive)
    try {
        $entry = $zip.GetEntry('payload-sha256.txt')
        if (-not $entry) { throw 'Desktop artifact has no payload hash manifest.' }
        $reader = [IO.StreamReader]::new($entry.Open())
        try { $manifest = $reader.ReadToEnd() } finally { $reader.Dispose() }
        $verifiedExecutable = $false
        foreach ($line in ($manifest -split '\r?\n' | Where-Object { $_ })) {
            if ($line -cnotmatch '^(?<hash>[0-9a-f]{64}) \*(?<path>.+)$') {
                throw 'Desktop artifact payload manifest is malformed.'
            }
            $path = [IO.Path]::GetFullPath((Join-Path $payload $Matches.path))
            if (-not $path.StartsWith($payload + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
                throw 'Desktop artifact payload path leaves its extraction directory.'
            }
            if ((Get-FileHash -LiteralPath $path).Hash -ine $Matches.hash) {
                throw "Extracted desktop payload differs from the selected artifact: $($Matches.path)"
            }
            if ($path -ieq [IO.Path]::GetFullPath($Executable)) { $verifiedExecutable = $true }
        }
        if (-not $verifiedExecutable) { throw 'Selected executable is not in the artifact payload manifest.' }
        return Get-Content -LiteralPath "$payload/release-manifest.json" -Raw | ConvertFrom-Json
    }
    finally { $zip.Dispose() }
}
