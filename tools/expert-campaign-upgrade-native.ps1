[CmdletBinding()]
param([Parameter(Mandatory)][string]$Root, [string]$GodotBin, [switch]$NoBuild)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = Split-Path $PSScriptRoot -Parent
. "$PSScriptRoot/godot-local.ps1"
. "$PSScriptRoot/native-process.ps1"
. "$PSScriptRoot/campaign-upgrade-verification.ps1"
. "$PSScriptRoot/expert-campaign-upgrade-verification.ps1"
$Root = [IO.Path]::GetFullPath($Root)
if (Test-Path -LiteralPath $Root) { throw 'Evidence root must be new.' }
New-Item -ItemType Directory -Path $Root | Out-Null
$godot = Resolve-GodotDotNetExecutable $GodotBin
$archive = "$repo/tests/Marvel.Server.Tests/Fixtures/expert-campaign-91098022.zip"
$hash = (Get-FileHash $archive).Hash
if ($hash -cne '1A7B953709EE3E390863BFD28A6C920EE00B53F7F24853B197013AA72903538B') { throw 'Expert predecessor hash mismatch.' }
Expand-Archive -LiteralPath $archive -DestinationPath "$Root/predecessor"
if (-not $NoBuild) {
    $build = Invoke-NativeProcess dotnet @('build', "$repo/src/Marvel.Godot/Marvel.Godot.csproj", '-c', 'Debug', '--nologo') @{} 180 "$Root/build.log" $repo
    if ($build.ExitCode -ne 0 -or $build.TimedOut) { throw 'Godot Debug build failed.' }
}
$id = '5721469c84d13a3550fd6d2f3950f301'
$published = "$Root/predecessor/published"
$access = Get-Content "$published/access.json" -Raw | ConvertFrom-Json
foreach ($choice in @('decline')) {
    $scope = "$Root/$choice"
    New-Item -ItemType Directory -Path "$scope/authority/Campaigns", "$scope/access" -Force | Out-Null
    Copy-Item "$published/campaigns/$id" "$scope/authority/Campaigns" -Recurse
    $local = [guid]::NewGuid().ToString('N'); $generation = [guid]::NewGuid().ToString('N')
    $connection = @{ schema = 1; local_id = $local; endpoint = ''; label = 'Expert predecessor'; campaign_id = $access.id; capability = $access.owner; game_id = $access.game; game_capability = $access.capability; pending = $null; snapshot = $null }
    $bytes = [Text.Encoding]::UTF8.GetBytes(($connection | ConvertTo-Json -Depth 10 -Compress))
    New-Item -ItemType Directory -Path "$scope/access/$local" | Out-Null
    [IO.File]::WriteAllBytes("$scope/access/$local/$generation.archive", $bytes)
    @{ generation = $generation; length = $bytes.Length; sha256 = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)); previous = $null } | ConvertTo-Json -Compress | Set-Content "$scope/access/$local/current"
    foreach ($journey in @('inspect', $choice, 'restart')) {
        $environment = @{ MARVEL_ENGINE_ENDPOINT = ''; MARVEL_SAVE_DIRECTORY = "$scope/authority"; MARVEL_CAMPAIGN_ACCESS_DIRECTORY = "$scope/access"; MARVEL_DECK_LIBRARY_DIRECTORY = "$scope/decks"; MARVEL_UI_SCALE = '100'; MARVEL_SMOKE_VIEWPORT = '1920x1080'; MARVEL_SMOKE_MOTION = 'disabled'; MARVEL_EXPERT_JOURNEY = $journey; MARVEL_UI_EVIDENCE_DIRECTORY = "$scope/$journey-artifacts" }
        $run = Invoke-NativeProcess $godot @('--headless', '--audio-driver', 'Dummy', '--path', "$repo/src/Marvel.Godot", '--script', "$PSScriptRoot/expert_campaign_upgrade_native.gd") $environment 90 "$scope/$journey.log" $repo
        if ($run.ExitCode -ne 0 -or $run.TimedOut -or $run.Output -match '(?m)^(SCRIPT ERROR:|ERROR:|WARNING:.*leaked)' -or $run.Output -notmatch "(?m)^EXPERT_CAMPAIGN_NATIVE_OK journey=$journey\s*$") { throw "Expert native $journey failed: $scope/$journey.log" }
        Assert-ExpertCampaignUpgrade $published "$scope/authority/Campaigns/$id" $(if ($journey -eq 'inspect') { '' } else { $choice })
        $current = Read-CampaignGeneration "$scope/authority/Campaigns/$id"
        if ($journey -eq $choice) { $continued = $current.ToJsonString() }
        if ($journey -eq 'restart' -and $continued -cne $current.ToJsonString()) { throw 'Restart changed persisted campaign.' }
    }
}
@{ sourceCommit = (git -C $repo rev-parse HEAD); fixtureSha256 = $hash; executable = $godot; executableSha256 = (Get-FileHash $godot).Hash; rendered = $false; viewport = '1920x1080'; scale = 100; isolation = 'Separate embedded authority, campaign access and deck library per recovery branch'; journeys = @('inspect', 'decline', 'restart') } | ConvertTo-Json | Set-Content "$Root/evidence.json"
Write-Host "EXPERT_CAMPAIGN_UPGRADE_OK evidence=$Root"
