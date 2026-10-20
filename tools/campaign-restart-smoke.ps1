param([string]$GodotBin = $env:GODOT_BIN, [switch]$NoBuild, [string]$ResultsDirectory)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
. "$PSScriptRoot/native-process.ps1"
if (-not $GodotBin) { throw 'Set GODOT_BIN or pass -GodotBin.' }
if (-not $ResultsDirectory) {
    $ResultsDirectory = Join-Path ([IO.Path]::GetTempPath()) ('marvel-campaign-journey-' + [guid]::NewGuid().ToString('N'))
}
$ResultsDirectory = [IO.Path]::GetFullPath($ResultsDirectory)
New-Item -ItemType Directory -Force $ResultsDirectory | Out-Null
# Every run owns new stores, even when retaining logs in an existing results directory.
$fixture = Join-Path $ResultsDirectory ([guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $fixture | Out-Null
$revision = git -C $repo rev-parse HEAD
@{ Revision = $revision; CampaignDifficulty = 'Standard'; ScenarioDifficulty = 'Standard';
    Hero = 'spider_man'; Scenario = 'crossbones'; Viewport = '1920x1080';
    Journey = 'Create, keep opening hand, restart authority process, resume current attempt' } |
    ConvertTo-Json | Set-Content "$fixture/evidence.json"
if (-not $NoBuild) {
    dotnet build "$repo/src/Marvel.Server.Host/Marvel.Server.Host.csproj" -c Release
    if ($LASTEXITCODE) { throw 'Server build failed.' }
    dotnet build "$repo/src/Marvel.Godot/Marvel.Godot.csproj" -c Debug
    if ($LASTEXITCODE) { throw 'Client build failed.' }
}
$listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
$listener.Start()
$port = $listener.LocalEndpoint.Port
$listener.Stop()
$environment = @{
    MARVEL_ENGINE_ENDPOINT = "tcp://127.0.0.1:$port"
    MARVEL_SAVE_DIRECTORY = "$fixture/embedded"
    MARVEL_CAMPAIGN_ACCESS_DIRECTORY = "$fixture/access"
    MARVEL_SMOKE_VIEWPORT = '1920x1080'
}
foreach ($journey in @('create', 'resume')) {
    $start = [Diagnostics.ProcessStartInfo]::new('dotnet')
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($argument in @("$repo/src/Marvel.Server.Host/bin/Release/net8.0/Marvel.Server.Host.dll",
        '--listen', '127.0.0.1', '--port', "$port", '--data-root', $repo, '--save-root', "$fixture/authority")) {
        $start.ArgumentList.Add($argument)
    }
    $server = [Diagnostics.Process]::Start($start)
    $stdout = $server.StandardOutput.ReadToEndAsync()
    $stderr = $server.StandardError.ReadToEndAsync()
    try {
        $environment.MARVEL_CAMPAIGN_JOURNEY = $journey
        $environment.MARVEL_CAMPAIGN_JOURNEY_CAPTURE = "$fixture/$journey.png"
        $result = Invoke-NativeProcess -Executable $GodotBin -Arguments @('--path', "$repo/src/Marvel.Godot",
            '--rendering-method', 'gl_compatibility', '--script', 'res://smoke/campaign_entry_smoke.gd') `
            -Environment $environment -TimeoutSeconds 120 -LogPath "$fixture/$journey.log" -WorkingDirectory $repo
        if ($result.ExitCode -ne 0 -or $result.TimedOut -or $result.Output -notmatch 'RORS_CAMPAIGN_ENTRY_OK') {
            throw "Campaign $journey journey failed; see $fixture."
        }
    }
    finally {
        if (-not $server.HasExited) { $server.Kill($true) }
        $server.WaitForExit()
        [IO.File]::WriteAllText("$fixture/$journey-server.log", $stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult())
        $server.Dispose()
    }
}
if ((Get-Content "$fixture/resume-server.log" -Raw) -notmatch 'session.restore.completed') {
    throw "The restarted server did not restore its durable session; see $fixture."
}
Write-Host "Campaign create, process restart and resume passed at $revision (Standard campaign / Standard scenario)."
Write-Host "Evidence manifest, logs and create.png/resume.png: $fixture"
