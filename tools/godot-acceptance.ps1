param(
    [string]$Script = 'res://smoke/automation/driver_contract_smoke.gd',
    [string]$GodotBin,
    [string]$ResultsDirectory,
    [string]$Marker = 'UI_ACCEPTANCE_OK',
    [int]$TimeoutSeconds = 120,
    [switch]$Rendered,
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
. "$PSScriptRoot/godot-local.ps1"
. "$PSScriptRoot/native-process.ps1"
. "$PSScriptRoot/godot-smoke-diagnostics.ps1"
. "$PSScriptRoot/native-evidence.ps1"
if ($Script -notmatch '^res://smoke/[a-zA-Z0-9_/-]+\.gd$' -or
    -not (Test-Path -LiteralPath "$repoRoot/src/Marvel.Godot/$($Script.Substring(6))")) {
    throw 'Script must name an existing res://smoke/ GDScript.'
}
if ($TimeoutSeconds -lt 1 -or [string]::IsNullOrWhiteSpace($Marker)) {
    throw 'A positive timeout and nonempty completion marker are required.'
}
if ([string]::IsNullOrWhiteSpace($ResultsDirectory)) {
    $ResultsDirectory = Join-Path ([IO.Path]::GetTempPath()) ('marvel-acceptance-' + [guid]::NewGuid().ToString('N'))
}
$ResultsDirectory = [IO.Path]::GetFullPath($ResultsDirectory)
if (Test-Path -LiteralPath $ResultsDirectory) { throw 'Choose a fresh results directory.' }
if ($ResultsDirectory.StartsWith($repoRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or $ResultsDirectory -eq $repoRoot) {
    throw 'Acceptance evidence must be outside the repository.'
}
$resolvedGodot = Resolve-GodotDotNetExecutable -GodotBin $GodotBin
New-Item -ItemType Directory -Path $ResultsDirectory | Out-Null
if (-not $NoBuild) {
    # The editor executable loads Debug assemblies for project scripts.
    $build = Invoke-NativeProcess 'dotnet' @('build', "$repoRoot/src/Marvel.Godot/Marvel.Godot.csproj", '-c', 'Debug', '--nologo') @{} 180 "$ResultsDirectory/build.log" $repoRoot
    if ($build.ExitCode -ne 0 -or $build.TimedOut) { throw "Build failed: $ResultsDirectory/build.log" }
}
$arguments = @('--audio-driver', 'Dummy', '--path', "$repoRoot/src/Marvel.Godot", '--script', $Script)
if (-not $Rendered) { $arguments = @('--headless') + $arguments }
else { $arguments = @('--minimized') + $arguments }
$environment = @{ MARVEL_UI_EVIDENCE_DIRECTORY = "$ResultsDirectory/artifacts";
    MARVEL_UI_SCALE = 100; MARVEL_SMOKE_VIEWPORT = '1920x1080';
    MARVEL_SMOKE_MOTION = 'disabled'; MARVEL_SMOKE_TWO_PLAYER = 'false' }
$run = Invoke-NativeProcess $resolvedGodot $arguments $environment $TimeoutSeconds "$ResultsDirectory/run.log" $repoRoot
$passed = $run.ExitCode -eq 0 -and -not $run.TimedOut -and
    (Test-NativeCompletion $run.Output $Marker) -and -not (Test-GodotSmokeDiagnostics @($run.Output))
@{ script = $Script; rendered = [bool]$Rendered; passed = $passed; exitCode = $run.ExitCode;
    timedOut = $run.TimedOut; elapsedSeconds = $run.ElapsedSeconds; marker = $Marker } |
    ConvertTo-Json | Set-Content -LiteralPath "$ResultsDirectory/result.json"
if (-not $passed) { throw "Acceptance failed. Evidence: $ResultsDirectory" }
Write-Host "ACCEPTANCE_OK evidence=$ResultsDirectory"
