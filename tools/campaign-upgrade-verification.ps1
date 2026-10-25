function Read-CampaignGeneration {
    param([string]$Directory)
    $manifest = Get-Content -LiteralPath "$Directory/current" -Raw | ConvertFrom-Json
    if ($manifest.generation -cnotmatch '^[a-f0-9]{32}$') { throw 'Invalid campaign generation.' }
    $path = Join-Path $Directory ($manifest.generation + '.archive')
    $bytes = [IO.File]::ReadAllBytes($path)
    if ($bytes.Length -ne $manifest.length -or
        [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)) -cne $manifest.sha256) {
        throw 'Campaign publication checksum failed.'
    }
    return ,([Text.Json.Nodes.JsonNode]::Parse([Text.Encoding]::UTF8.GetString($bytes)))
}

function Assert-PlayedCampaignUpgrade {
    param([hashtable]$Fixture, [string]$ReplayContract, [string]$ContinuedArchive)
    $id = 'f7c9ce17062996483ea973fd77a6257b'
    $original = Read-CampaignGeneration "$($Fixture.Source)/published/campaigns/$id"
    $current = Read-CampaignGeneration "$($Fixture.Campaigns)/$id"
    $expected = $original
    if ($ContinuedArchive) {
        $hash = (Get-FileHash -LiteralPath $ContinuedArchive).Hash
        if ($hash -cne '970501B99649ED1B2FBC5EB18DD337C9F7CA3A355D7FD01D137EEF02F5970DC6') {
            throw 'Predecessor continuation archive checksum failed.'
        }
        $file = [IO.File]::OpenRead($ContinuedArchive)
        try {
            $gzip = [IO.Compression.GZipStream]::new($file, [IO.Compression.CompressionMode]::Decompress, $true)
            try {
                $reader = [IO.StreamReader]::new($gzip)
                try { $expected = [Text.Json.Nodes.JsonNode]::Parse($reader.ReadToEnd()) }
                finally { $reader.Dispose() }
            }
            finally { $gzip.Dispose() }
        }
        finally { $file.Dispose() }
    }
    $save = $current['save']
    $before = $expected['save']
    if ($save['compatibility']['replay_contract'].ToString() -cne $ReplayContract) {
        throw 'Campaign was not published under the selected artifact replay contract.'
    }
    if ($save['attempts'].Count -ne 13 -or $save['attempts'].Count -ne $before['attempts'].Count -or
        $save['journal'].Count -ne $before['journal'].Count) { throw 'Campaign history length changed.' }
    $target = $save['compatibility'].DeepClone()
    # Only explicitly named provenance locations may differ after verified migration.
    $save['compatibility'] = $before['compatibility'].DeepClone()
    for ($i = 0; $i -lt $save['attempts'].Count; $i++) {
        if (-not [Text.Json.Nodes.JsonNode]::DeepEquals($target, $save['attempts'][$i]['compatibility'])) {
            throw 'Campaign attempt was not migrated to the same target identity.'
        }
        $save['attempts'][$i]['compatibility'] = $before['attempts'][$i]['compatibility'].DeepClone()
    }
    for ($i = 0; $i -lt $save['journal'].Count; $i++) {
        if ($before['journal'][$i]['proof']) {
            if (-not $save['journal'][$i]['proof']) { throw 'Campaign proof disappeared.' }
            if (-not [Text.Json.Nodes.JsonNode]::DeepEquals($target, $save['journal'][$i]['proof']['compatibility'])) {
                throw 'Campaign proof was not migrated to the same target identity.'
            }
            $save['journal'][$i]['proof']['compatibility'] =
                $before['journal'][$i]['proof']['compatibility'].DeepClone()
        }
    }
    if (-not [Text.Json.Nodes.JsonNode]::DeepEquals($expected, $current)) {
        throw 'Upgrade changed original campaign gameplay, proof, authority, receipt, or record.'
    }
    $backups = @(Get-ChildItem -LiteralPath "$($Fixture.Campaigns)/$id" -Force -Filter '.upgrade-*.archive')
    if ($backups.Count -ne 1) { throw 'Expected one retained original migration backup.' }
    $originalManifest = Get-Content -LiteralPath "$($Fixture.Source)/published/campaigns/$id/current" -Raw | ConvertFrom-Json
    if ((Get-FileHash -LiteralPath $backups[0].FullName).Hash -cne $originalManifest.sha256) {
        throw 'Retained migration backup does not preserve the original archive bytes.'
    }
    $backup = [Text.Json.Nodes.JsonNode]::Parse([IO.File]::ReadAllText($backups[0].FullName))
    if (-not [Text.Json.Nodes.JsonNode]::DeepEquals($original, $backup)) {
        throw 'Retained migration backup differs from the original campaign.'
    }
}
