param(
    [string]$GodotBin = $env:GODOT_BIN,
    [ValidateSet('representative', 'exhaustive', 'inspection-all-scales')]
    [string]$Profile = 'exhaustive',
    [string[]]$Case = @('*'),
    [switch]$List,
    [switch]$NoBuild,
    [string]$ResultsDirectory,
    [int]$ShardIndex = 0,
    [int]$ShardCount = 1
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
. "$PSScriptRoot/native-process.ps1"
. "$PSScriptRoot/godot-smoke-diagnostics.ps1"
. "$PSScriptRoot/native-manifest.ps1"
. "$PSScriptRoot/native-evidence.ps1"
$manifest = Read-NativeManifest "$PSScriptRoot/native-cases.json" "$repoRoot/src/Marvel.Godot"
$selected = @($manifest.cases | Where-Object {
    $entry = $_
    $inProfile = $entry.profiles -contains $Profile
    if ($Profile -eq 'inspection-all-scales') { $inProfile = $entry.script -eq 'res://smoke/card_inspection_smoke.gd' }
    $inProfile -and @($Case | Where-Object { $entry.id -like $_ }).Count -gt 0
})
if ($selected.Count -eq 0) { throw 'No native cases match this profile and selector.' }
foreach ($selector in $Case) {
    if (@($selected | Where-Object { $_.id -like $selector }).Count -eq 0) { throw "Unknown native case selector: $selector" }
}
$selected = @(Select-NativeShard $selected $ShardIndex $ShardCount)
if ($List) { ConvertTo-Json -InputObject @($selected) -Depth 12; return }
if ($selected.Count -eq 0) { throw 'No native cases in the selected shard.' }
if ([string]::IsNullOrWhiteSpace($GodotBin)) { throw 'Set GODOT_BIN or pass -GodotBin.' }
if ([string]::IsNullOrWhiteSpace($ResultsDirectory)) {
    $ResultsDirectory = $env:MARVEL_NATIVE_RESULTS_DIRECTORY
}
if ([string]::IsNullOrWhiteSpace($ResultsDirectory)) {
    $ResultsDirectory = Join-Path ([IO.Path]::GetTempPath()) ('marvel-native-' + [guid]::NewGuid().ToString('N'))
}
$ResultsDirectory = [IO.Path]::GetFullPath($ResultsDirectory)
$repositoryPrefix = [IO.Path]::GetFullPath($repoRoot) + [IO.Path]::DirectorySeparatorChar
if ($ResultsDirectory.Equals([IO.Path]::GetFullPath($repoRoot), [StringComparison]::OrdinalIgnoreCase) -or
    $ResultsDirectory.StartsWith($repositoryPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Native evidence must be written outside the repository.'
}
Assert-NativeEvidenceFresh $ResultsDirectory
[IO.Directory]::CreateDirectory($ResultsDirectory) | Out-Null
Write-Host "Native profile ${Profile}, shard ${ShardIndex}/${ShardCount}: $($selected.Count) cases. Evidence: $ResultsDirectory"
$version = Invoke-NativeProcess $GodotBin @('--version') @{} 15 "$ResultsDirectory/version.log" $repoRoot
if ($version.ExitCode -ne 0 -or $version.TimedOut -or -not $version.Output.StartsWith('4.7.')) { throw 'Godot 4.7 .NET is required.' }
if (-not $NoBuild) {
    foreach ($project in @('src/Marvel.Godot/Marvel.Godot.csproj', 'tests/Marvel.Godot.Tests/Marvel.Godot.Tests.csproj')) {
        $arguments = @('build', "$repoRoot/$project", '--nologo')
        if ($project.StartsWith('tests/')) { $arguments += @('-c', 'Release') }
        $built = Invoke-NativeProcess 'dotnet' $arguments @{} 180 "$ResultsDirectory/build-$([IO.Path]::GetFileNameWithoutExtension($project)).log" $repoRoot
        if ($built.ExitCode -ne 0 -or $built.TimedOut) { throw "Native dependency build failed: $project" }
    }
}
$fixturePaths = @{}
foreach ($fixtureId in @($selected.fixture | Where-Object { $_ } | Select-Object -Unique)) {
    $fixture = $manifest.fixtures[$fixtureId]
    $path = [IO.Path]::GetFullPath((Join-Path $ResultsDirectory $fixture.file))
    if (-not $path.StartsWith($ResultsDirectory + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Native fixture escapes the evidence directory: $fixtureId"
    }
    Reset-NativeFixtureOutput $path
    $environment = @{ $fixture.environment = $path }
    $arguments = @('test', "$repoRoot/tests/Marvel.Godot.Tests/Marvel.Godot.Tests.csproj", '-c', 'Release', '--no-build', '--nologo', '--filter', $fixture.testFilter)
    $generated = Invoke-NativeProcess 'dotnet' $arguments $environment $fixture.timeoutSeconds "$ResultsDirectory/fixture-$fixtureId.log" $repoRoot
    if ($generated.ExitCode -ne 0 -or $generated.TimedOut -or -not (Test-Path -LiteralPath $path)) { throw "Fixture generation failed: $fixtureId" }
    $fixturePaths[$fixtureId] = $path
}
$results = Join-Path $ResultsDirectory 'results.jsonl'
foreach ($entry in $selected) {
    Write-Host "NATIVE_CASE_BEGIN $($entry.id)"
    $environment = @{ MARVEL_UI_EVIDENCE_DIRECTORY = "$ResultsDirectory/artifacts/$($entry.id)";
        MARVEL_UI_SCALE = $entry.scale; MARVEL_SMOKE_MOTION = $entry.motion;
        MARVEL_SMOKE_VIEWPORT = $entry.viewport; MARVEL_SMOKE_TWO_PLAYER = ($entry.players -eq 2).ToString().ToLowerInvariant() }
    if ($entry.environment) { foreach ($key in $entry.environment.Keys) { $environment[$key] = $entry.environment[$key] } }
    $arguments = @('--headless', '--audio-driver', 'Dummy', '--path', "$repoRoot/src/Marvel.Godot", '--script', $entry.script)
    if ($entry.arguments) {
        $arguments += '--'
        $arguments += @(Resolve-NativeArguments $entry $fixturePaths)
    }
    $log = Join-Path $ResultsDirectory ($entry.id + '.log')
    $executable = $GodotBin
    if ($entry.contract) {
        $executable = 'dotnet'
        $environment['GODOT_BIN'] = $GodotBin
        $environment['MARVEL_NATIVE_CONTRACT'] = $entry.contract
        $arguments = @('test', "$repoRoot/tests/Marvel.Godot.Tests/Marvel.Godot.Tests.csproj", '-c', 'Release', '--no-build',
            '--filter', 'FullyQualifiedName~NativeContractJourneyTests', '--logger', 'console;verbosity=detailed')
    }
    $run = Invoke-NativeProcess $executable $arguments $environment $entry.timeoutSeconds $log $repoRoot
    $missing = @($entry.markers | Where-Object { -not (Test-NativeCompletion $run.Output $_) })
    $passed = $run.ExitCode -eq 0 -and -not $run.TimedOut -and $missing.Count -eq 0 -and -not (Test-GodotSmokeDiagnostics @($run.Output))
    $record = @{ case = $entry; passed = $passed; elapsedSeconds = $run.ElapsedSeconds;
        exitCode = $run.ExitCode; timedOut = $run.TimedOut; missingMarkers = $missing; log = $log }
    Add-Content -LiteralPath $results -Value ($record | ConvertTo-Json -Depth 15 -Compress)
    if (-not $passed) { throw "Native case failed: $($entry.id). Evidence: $log; missing markers: $($missing -join ', ')" }
    Write-Host "NATIVE_CASE_OK $($entry.id) seconds=$($run.ElapsedSeconds)"
}
Write-Host "NATIVE_PROFILE_OK profile=$Profile shard=$ShardIndex/$ShardCount cases=$($selected.Count) evidence=$results"
