param(
    [Parameter(Mandatory = $false)]
    [string]$GodotBin = $env:GODOT_BIN,
    [Parameter(Mandatory = $false)]
    [int]$Port = 41924
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($GodotBin)) {
    throw "Set GODOT_BIN or pass -GodotBin with the Godot 4.7 .NET executable."
}

$version = & $GodotBin --version
if ($LASTEXITCODE -ne 0 -or -not $version.StartsWith("4.7.")) {
    throw "Godot 4.7 .NET is required; found: $version"
}

dotnet build "$repoRoot/src/Marvel.Godot/Marvel.Godot.csproj" --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$serverOut = [System.IO.Path]::GetTempFileName()
$serverError = [System.IO.Path]::GetTempFileName()
$server = $null
try {
    $arguments = @(
        "run", "--no-build",
        "--project", "$repoRoot/src/Marvel.Server/Marvel.Server.csproj",
        "--", "--listen", "127.0.0.1", "--port", $Port,
        "--data-root", $repoRoot,
        "--visibility", "restricted", "--seat", "0"
    )
    $server = Start-Process -FilePath "dotnet" -ArgumentList $arguments `
        -RedirectStandardOutput $serverOut -RedirectStandardError $serverError `
        -PassThru -NoNewWindow

    $ready = $false
    for ($attempt = 0; $attempt -lt 300; $attempt++) {
        if ($server.HasExited) {
            throw "Restricted hosted smoke server exited: $(Get-Content $serverError -Raw)"
        }
        if ((Get-Content $serverError -Raw) -match "listening on") {
            $ready = $true
            break
        }
        Start-Sleep -Milliseconds 100
    }
    if (-not $ready) {
        throw "Restricted hosted smoke server did not become ready: $(Get-Content $serverError -Raw)"
    }

    $env:MARVEL_ENGINE_ENDPOINT = "tcp://127.0.0.1:$Port"
    $env:MARVEL_UI_SCALE = "standard"
    $smokeOutput = & $GodotBin --path "$repoRoot/src/Marvel.Godot" `
        --script res://smoke/hosted_multiplayer_smoke.gd 2>&1
    $smokeOutput | Write-Output
    if ($LASTEXITCODE -ne 0 -or `
        -not ($smokeOutput -match "HOSTED_MULTIPLAYER_SMOKE_OK")) {
        exit 1
    }
}
finally {
    if ($null -ne $server -and -not $server.HasExited) {
        $server.Kill($true)
        $server.WaitForExit()
    }
    Remove-Item $serverOut, $serverError -Force -ErrorAction SilentlyContinue
}
exit 0
