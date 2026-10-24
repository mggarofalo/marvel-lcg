# Fixture preparation never rewrites an authoritative predecessor archive.
function New-PlayedCampaignUpgradeFixture {
    param([string]$Archive, [string]$Root, [string]$Endpoint)
    $expected = '1199392f3439fe89b6a21aa03d195df18bef1cc70f20dd65a998a2e32b29ffe8'
    if ((Get-FileHash -LiteralPath $Archive -Algorithm SHA256).Hash.ToLowerInvariant() -cne $expected) {
        throw 'Played predecessor campaign fixture does not match its pinned SHA256.'
    }
    if (Test-Path -LiteralPath $Root) { throw 'Upgrade fixture root must be new.' }
    New-Item -ItemType Directory -Path $Root | Out-Null
    $source = Join-Path $Root 'predecessor'
    Expand-Archive -LiteralPath $Archive -DestinationPath $source
    $authority = Join-Path $Root 'authority'
    $campaigns = Join-Path $authority $(if ($Endpoint) { '.campaigns' } else { 'Campaigns' })
    New-Item -ItemType Directory -Path $campaigns -Force | Out-Null
    Copy-Item -LiteralPath "$source/published/campaigns/f7c9ce17062996483ea973fd77a6257b" -Destination $campaigns -Recurse
    $access = Get-Content -LiteralPath "$source/published/access.json" -Raw | ConvertFrom-Json
    $localId = [guid]::NewGuid().ToString('N')
    $generation = [guid]::NewGuid().ToString('N')
    $connection = [ordered]@{
        schema = 1; local_id = $localId; endpoint = $Endpoint; label = 'Played predecessor campaign'
        campaign_id = $access.id; capability = $access.owner
        game_id = $access.game; game_capability = $access.capability
        pending = $null; snapshot = $null
    }
    $bytes = [Text.Encoding]::UTF8.GetBytes(($connection | ConvertTo-Json -Depth 10 -Compress))
    $directory = Join-Path "$Root/access" $localId
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    [IO.File]::WriteAllBytes("$directory/$generation.archive", $bytes)
    $manifest = @{ generation = $generation; length = $bytes.Length
        sha256 = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)); previous = $null }
    [IO.File]::WriteAllText("$directory/current", ($manifest | ConvertTo-Json -Compress))
    return @{ Root = $Root; Source = $source; Authority = $authority; Campaigns = $campaigns
        Access = "$Root/access"; FixtureSha256 = $expected }
}
