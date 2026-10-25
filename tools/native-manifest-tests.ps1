$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/native-manifest.ps1"
. "$PSScriptRoot/native-evidence.ps1"
$project = Join-Path (Split-Path -Parent $PSScriptRoot) 'src/Marvel.Godot'
$source = Join-Path $PSScriptRoot 'native-cases.json'
$valid = Read-NativeManifest $source $project
if ($valid.cases.Count -eq 0) { throw 'The admitted native manifest has no cases.' }
$candidatePath = Join-Path ([IO.Path]::GetTempPath()) ('marvel-manifest-test-' + [guid]::NewGuid().ToString('N') + '.json')
$probes = [ordered]@{
    'script-uri' = { param($m) $m.cases[0].script = 'smoke/local_game_smoke.gd' }
    'script-escape' = { param($m) $m.cases[0].script = 'res://../../AGENTS.md' }
    'fixture-placeholder' = { param($m) $m.cases[0].Remove('fixture'); $m.cases[0].arguments = @('--input={fixture}') }
    'fixture-reference' = { param($m) $m.cases[0].fixture = 'unknown-provider' }
    'profile' = { param($m) $m.cases[0].profiles = @('unregistered-profile') }
    'scale' = { param($m) $m.cases[0].scale = 55 }
    'viewport' = { param($m) $m.cases[0].viewport = '800x600' }
    'players' = { param($m) $m.cases[0].players = 5 }
    'players-zero' = { param($m) $m.cases[0].players = 0 }
    'completion-marker' = { param($m) $m.cases[0].markers = @() }
    'fixture-parent' = { param($m) $m.fixtures.faces.file = '../escaped.json' }
    'fixture-windows-parent' = { param($m) $m.fixtures.faces.file = '..\escaped.json' }
    'fixture-root' = { param($m) $m.fixtures.faces.file = '/escaped.json' }
    'fixture-dot' = { param($m) $m.fixtures.faces.file = '..' }
}
$diagnostics = @{
    'script-uri' = 'Invalid native script URI:'
    'script-escape' = 'Native script escapes its project or does not exist:'
    'fixture-placeholder' = 'Native fixture placeholder has no provider:'
    'fixture-reference' = 'Unknown native fixture:'
    'profile' = 'Unknown native profile:'
    'scale' = 'Unsupported native scale:'
    'viewport' = 'Unsupported native viewport:'
    'players' = 'Invalid native input profile:'
    'players-zero' = 'Invalid native input profile:'
    'completion-marker' = 'Missing native completion marker:'
    'fixture-parent' = 'Native fixture must use a leaf filename.'
    'fixture-windows-parent' = 'Native fixture must use a leaf filename.'
    'fixture-root' = 'Native fixture must use a leaf filename.'
    'fixture-dot' = 'Native fixture must use a leaf filename.'
}
$evidenceDirectory = $candidatePath + '.evidence'
[IO.Directory]::CreateDirectory($evidenceDirectory) | Out-Null
$ambientResults = $env:MARVEL_NATIVE_RESULTS_DIRECTORY
try {
    Assert-NativeEvidenceFresh $evidenceDirectory
    if (@(Get-ChildItem -LiteralPath $evidenceDirectory).Count -ne 0) { throw 'Fresh evidence admission mutated the directory.' }
    $priorResults = Join-Path $evidenceDirectory 'results.jsonl'
    $priorLog = Join-Path $evidenceDirectory 'version.log'
    [IO.File]::WriteAllText($priorResults, 'prior run results')
    [IO.File]::WriteAllText($priorLog, 'prior version log')
    foreach ($environmentSelected in @($false, $true)) {
        $arguments = @{ GodotBin = 'native-evidence-must-reject-before-launch'; NoBuild = $true }
        if ($environmentSelected) { $env:MARVEL_NATIVE_RESULTS_DIRECTORY = $evidenceDirectory }
        else { $arguments.ResultsDirectory = $evidenceDirectory }
        $rejected = $false
        try { & "$PSScriptRoot/native-cases.ps1" @arguments }
        catch {
            if ($_.Exception.Message -ne 'Native evidence already contains results.jsonl; choose a fresh results directory.') {
                throw "Reused native evidence failed for an unrelated reason: $($_.Exception.Message)"
            }
            $rejected = $true
        }
        if (-not $rejected) { throw 'A second native run admitted existing evidence.' }
        if ([IO.File]::ReadAllText($priorResults) -ne 'prior run results' -or
            [IO.File]::ReadAllText($priorLog) -ne 'prior version log' -or
            @(Get-ChildItem -LiteralPath $evidenceDirectory).Count -ne 2) {
            throw 'Rejected evidence reuse changed an existing artifact or generated a new one.'
        }
    }
    Write-Host 'NATIVE_EVIDENCE_REUSE_OK fresh=true explicit-rejected=true environment-rejected=true artifacts-unchanged=true'
    [IO.File]::WriteAllText($candidatePath, 'stale fixture from an earlier run')
    Reset-NativeFixtureOutput $candidatePath
    # A successful no-match provider produces no new file and must not reuse stale evidence.
    if (Test-Path -LiteralPath $candidatePath) { throw 'An empty fixture provider could reuse stale output.' }
    [IO.File]::WriteAllText($candidatePath, 'fresh fixture')
    if ([IO.File]::ReadAllText($candidatePath) -ne 'fresh fixture') { throw 'Fresh fixture output was not retained.' }
    $plain = @(Resolve-NativeArguments @{ arguments = @('--marvel-b1-sample') } @{})
    if ($plain.Count -ne 1 -or $plain[0] -ne '--marvel-b1-sample') { throw 'A non-fixture argument was changed or dropped.' }
    $expanded = @(Resolve-NativeArguments @{ fixture = 'sample'; arguments = @('--input={fixture}', '--literal') } @{ sample = 'C:/evidence with spaces/sample.json' })
    if ($expanded.Count -ne 2 -or $expanded[0] -ne '--input=C:/evidence with spaces/sample.json' -or $expanded[1] -ne '--literal') { throw 'Fixture expansion changed argument identity or boundaries.' }
    if (-not (Test-NativeCompletion '  JOURNEY_OK decisions=3' 'JOURNEY_OK')) { throw 'A standalone completion record was not recognized.' }
    foreach ($misleading in @('NOT_JOURNEY_OK', 'JOURNEY_OK_BUT_FAILED', 'Expected JOURNEY_OK')) {
        if (Test-NativeCompletion $misleading 'JOURNEY_OK') { throw "An embedded marker falsely completed a case: $misleading" }
    }
    $accepted = '{"event_id":"server.request.completed","operation":"resolve","disposition":"accepted","save_committed":true}'
    if (-not (Test-NativeCompletion $accepted '"operation":"resolve"')) { throw 'Committed resolve evidence was not recognized.' }
    foreach ($uncommitted in @('{"operation":"resolve"}', $accepted.Replace('"accepted"', '"rejected"'), $accepted.Replace('true', 'false'))) {
        if (Test-NativeCompletion $uncommitted '"operation":"resolve"') { throw 'An uncommitted resolve falsely satisfied close-drain evidence.' }
    }
    foreach ($players in 1..4) {
        $candidate = Get-Content -LiteralPath $source -Raw | ConvertFrom-Json -AsHashtable
        $candidate.cases[0].players = $players
        [IO.File]::WriteAllText($candidatePath, ($candidate | ConvertTo-Json -Depth 20))
        $admitted = Read-NativeManifest $candidatePath $project
        if ($admitted.cases[0].players -ne $players) { throw 'Native player count changed during admission.' }
    }
    Write-Host 'NATIVE_MANIFEST_PLAYERS_OK supported=1..4'
    foreach ($name in $probes.Keys) {
        $candidate = Get-Content -LiteralPath $source -Raw | ConvertFrom-Json -AsHashtable
        & $probes[$name] $candidate
        [IO.File]::WriteAllText($candidatePath, ($candidate | ConvertTo-Json -Depth 20))
        $rejected = $false
        try { Read-NativeManifest $candidatePath $project | Out-Null }
        catch {
            if (-not $_.Exception.Message.StartsWith($diagnostics[$name], [StringComparison]::Ordinal)) {
                throw "Native manifest failed for an unrelated reason ($name): $($_.Exception.Message)"
            }
            $rejected = $true
        }
        if (-not $rejected) { throw "Malformed native manifest was admitted: $name" }
        Write-Host "NATIVE_MANIFEST_REJECTION_OK $name"
    }
    Write-Host "NATIVE_MANIFEST_TESTS_OK positive=true rejected=$($probes.Count)"
}
finally {
    $env:MARVEL_NATIVE_RESULTS_DIRECTORY = $ambientResults
    Remove-Item -LiteralPath $candidatePath -Force -ErrorAction SilentlyContinue
    foreach ($leaf in @('results.jsonl', 'version.log')) {
        Remove-Item -LiteralPath (Join-Path $evidenceDirectory $leaf) -Force -ErrorAction SilentlyContinue
    }
    Remove-Item -LiteralPath $evidenceDirectory -Force
}
