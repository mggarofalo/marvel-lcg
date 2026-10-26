[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('Prepare', 'inspect', 'continue', 'restart')][string]$Journey,
    [Parameter(Mandatory)][string]$Root,
    [string]$Endpoint = '',
    [string]$ClientExecutable,
    [string]$ClientArtifact,
    [string]$ClientArtifactSha256,
    [string]$ArtifactCommit,
    [string]$ReplayContract = 'engine-replay-v25',
    [switch]$ExternalInteraction
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = Split-Path $PSScriptRoot -Parent
. "$PSScriptRoot/campaign-upgrade-fixture.ps1"
. "$PSScriptRoot/campaign-upgrade-verification.ps1"
. "$PSScriptRoot/campaign-upgrade-artifact.ps1"
. "$PSScriptRoot/native-process.ps1"
$Root = [IO.Path]::GetFullPath($Root)
if ($Journey -eq 'Prepare') {
    $fixture = New-PlayedCampaignUpgradeFixture `
        -Archive "$repo/tests/Marvel.Server.Tests/Fixtures/played-campaign-91098022.zip" -Root $Root -Endpoint $Endpoint
    $fixture.Endpoint = $Endpoint
    $fixture | ConvertTo-Json | Set-Content -LiteralPath "$Root/fixture.json"
    Write-Host "Original predecessor bytes prepared at $Root. Start the selected remote artifact before inspect, if applicable."
    return
}
$fixture = Get-Content -LiteralPath "$Root/fixture.json" -Raw | ConvertFrom-Json -AsHashtable
if (-not $ExternalInteraction) {
    throw 'Release templates require external UI interaction. Pass -ExternalInteraction and perform the documented journey.'
}
if ((Test-Path -LiteralPath "$Root/$Journey.log") -or (Test-Path -LiteralPath "$Root/$Journey-engine.log")) {
    throw 'Each journey needs fresh evidence files; preserve the failed run and prepare a new fixture to retry.'
}
if (-not $ClientArtifact -or -not $ClientExecutable -or $ClientArtifactSha256 -cnotmatch '^[a-fA-F0-9]{64}$') {
    throw 'Native package evidence requires an exact artifact path, executable, and expected SHA256.'
}
$actualHash = (Get-FileHash -LiteralPath $ClientArtifact -Algorithm SHA256).Hash
$release = Assert-CampaignDesktopArtifact -Archive $ClientArtifact -ExpectedHash $ClientArtifactSha256 -Executable $ClientExecutable
if ($ArtifactCommit -cnotmatch '^[a-f0-9]{40}$' -or $release.commit -cne $ArtifactCommit -or
    $release.engine.replay_contract -cne $ReplayContract) {
    throw 'Desktop release manifest does not match the selected commit and replay contract.'
}
$environment = @{
    MARVEL_ENGINE_ENDPOINT = $fixture.Endpoint
    MARVEL_SAVE_DIRECTORY = $fixture.Authority
    MARVEL_CAMPAIGN_ACCESS_DIRECTORY = $fixture.Access
    MARVEL_SMOKE_VIEWPORT = '1920x1080'
    MARVEL_SMOKE_MOTION = 'disabled'
    MARVEL_UPGRADE_JOURNEY = $Journey
    MARVEL_UPGRADE_CAPTURE_DIRECTORY = $Root
}
$result = Invoke-NativeProcess -Executable $ClientExecutable `
    -Arguments @('--rendering-method', 'gl_compatibility', '--log-file', "$Root/$Journey-engine.log",
        '--accessibility', 'always') `
    -Environment $environment -TimeoutSeconds 600 -LogPath "$Root/$Journey.log" `
    -WorkingDirectory (Split-Path ([IO.Path]::GetFullPath($ClientExecutable)) -Parent)
$output = $result.Output
if (Test-Path -LiteralPath "$Root/$Journey-engine.log") {
    $output += Get-Content -LiteralPath "$Root/$Journey-engine.log" -Raw
}
if ($result.ExitCode -ne 0 -or $result.TimedOut -or
    $output -match '(?m)^(SCRIPT ERROR:|ERROR:|WARNING:.*leaked)') {
    throw "Packaged campaign $Journey failed; see $Root/$Journey.log."
}
$verification = @{ Fixture = $fixture; ReplayContract = $ReplayContract }
if ($Journey -ne 'inspect') {
    $verification.ContinuedArchive = "$repo/tests/Marvel.Server.Tests/Fixtures/played-campaign-91098022-continued.archive.gz"
}
Assert-PlayedCampaignUpgrade @verification
@{ Journey = $Journey; Artifact = [IO.Path]::GetFullPath($ClientArtifact); ArtifactSha256 = $actualHash
    Executable = [IO.Path]::GetFullPath($ClientExecutable)
    ExecutableSha256 = (Get-FileHash -LiteralPath $ClientExecutable -Algorithm SHA256).Hash
    ProbeRevision = (git -C $repo rev-parse HEAD); ProbeSha256 = (Get-FileHash "$PSCommandPath").Hash
    Interaction = 'External UI; requires separately retained observations'
    Release = $release; Endpoint = $fixture.Endpoint; ReplayContract = $ReplayContract; FixtureSha256 = $fixture.FixtureSha256
    OS = [Runtime.InteropServices.RuntimeInformation]::OSDescription } |
    ConvertTo-Json | Set-Content -LiteralPath "$Root/$Journey-evidence.json"
Write-Host "Artifact process and persistence checks passed. Review the external UI observations and authority restarts separately."
