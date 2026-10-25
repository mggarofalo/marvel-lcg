[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('inspect', 'continue', 'restart')][string]$Journey,
    [Parameter(Mandatory)][string]$Root,
    [Parameter(Mandatory)][string]$GodotBin,
    [string]$ServerContainer,
    [string]$ReplayContract = 'engine-replay-v22'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = Split-Path $PSScriptRoot -Parent
. "$PSScriptRoot/native-process.ps1"
. "$PSScriptRoot/campaign-upgrade-verification.ps1"
$Root = [IO.Path]::GetFullPath($Root)
$fixture = Get-Content -LiteralPath "$Root/fixture.json" -Raw | ConvertFrom-Json -AsHashtable
if ((Test-Path "$Root/$Journey.log") -or (Test-Path "$Root/$Journey-engine.log")) {
    throw 'Preserve existing evidence; prepare a fresh fixture for a retry.'
}
$environment = @{
    MARVEL_ENGINE_ENDPOINT = $fixture.Endpoint
    MARVEL_SAVE_DIRECTORY = $fixture.Authority
    MARVEL_CAMPAIGN_ACCESS_DIRECTORY = $fixture.Access
    MARVEL_UI_SCALE = '100'
    MARVEL_SMOKE_VIEWPORT = '1920x1080'
    MARVEL_SMOKE_MOTION = 'disabled'
    MARVEL_UPGRADE_JOURNEY = $Journey
    MARVEL_UPGRADE_CAPTURE_DIRECTORY = $Root
}
$result = Invoke-NativeProcess -Executable $GodotBin -Arguments @(
    '--path', "$repo/src/Marvel.Godot", '--rendering-method', 'gl_compatibility',
    '--audio-driver', 'Dummy', '--log-file', "$Root/$Journey-engine.log",
    '--script', "$PSScriptRoot/campaign_upgrade_native.gd"
) -Environment $environment -TimeoutSeconds 240 -LogPath "$Root/$Journey.log" -WorkingDirectory $repo
$output = $result.Output
if (Test-Path "$Root/$Journey-engine.log") {
    $output += Get-Content -LiteralPath "$Root/$Journey-engine.log" -Raw
}
if ($result.ExitCode -ne 0 -or $result.TimedOut -or
    $output -match '(?m)^(SCRIPT ERROR:|ERROR:|WARNING:.*leaked)' -or
    $output -notmatch "CAMPAIGN_UPGRADE_NATIVE_OK journey=$Journey revision=") {
    throw "Development native journey failed: $Root/$Journey.log"
}
if ($ServerContainer) {
    if (-not $fixture.Endpoint) { throw 'A container snapshot requires a remote fixture.' }
    docker stop $ServerContainer | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Could not stop the authority before snapshotting.' }
    docker logs $ServerContainer *> "$Root/$Journey-server.log"
    $container = docker inspect $ServerContainer | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $container[0].State.ExitCode -ne 0) {
        throw 'Remote authority did not stop cleanly.'
    }
    $container | ConvertTo-Json -Depth 30 | Set-Content "$Root/$Journey-container.json"
    $snapshot = "$Root/$Journey-authority"
    docker cp "${ServerContainer}:/var/lib/marvel/sessions" $snapshot
    if ($LASTEXITCODE -ne 0) { throw 'Could not retain the stopped authority snapshot.' }
    $fixture.Campaigns = "$snapshot/.campaigns"
}
$verification = @{ Fixture = $fixture; ReplayContract = $ReplayContract }
if ($Journey -ne 'inspect') {
    $verification.ContinuedArchive = "$repo/tests/Marvel.Server.Tests/Fixtures/played-campaign-91098022-continued.archive.gz"
}
Assert-PlayedCampaignUpgrade @verification
@{
    Journey = $Journey; ClientKind = 'Source-built development runtime; not a desktop release'
    SourceCommit = (git -C $repo rev-parse HEAD)
    ProbeSha256 = (Get-FileHash "$PSScriptRoot/campaign_upgrade_native.gd").Hash
    Executable = [IO.Path]::GetFullPath($GodotBin)
    ExecutableSha256 = (Get-FileHash $GodotBin).Hash
    Endpoint = $fixture.Endpoint; ReplayContract = $ReplayContract
    FixtureSha256 = $fixture.FixtureSha256
    OS = [Runtime.InteropServices.RuntimeInformation]::OSDescription
} | ConvertTo-Json | Set-Content "$Root/$Journey-evidence.json"
