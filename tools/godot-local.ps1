function Resolve-GodotDotNetExecutable {
    param(
        [Parameter(Mandatory = $false)]
        [string]$GodotBin
    )

    $candidates = [System.Collections.Generic.List[string]]::new()
    if (-not [string]::IsNullOrWhiteSpace($GodotBin)) {
        [void]$candidates.Add($GodotBin)
    }
    if (-not [string]::IsNullOrWhiteSpace($env:GODOT_BIN)) {
        [void]$candidates.Add($env:GODOT_BIN)
    }

    foreach ($commandName in @("godot4-mono", "godot-mono", "godot4", "godot")) {
        $command = Get-Command $commandName -CommandType Application -ErrorAction SilentlyContinue
        if ($null -ne $command) {
            [void]$candidates.Add($command.Source)
        }
    }

    $knownWindowsPaths = @(
        "C:\Tools\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe",
        "C:\Tools\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64.exe",
        (Join-Path $env:USERPROFILE "scoop\apps\godot-mono\current\godot_console.exe"),
        (Join-Path $env:USERPROFILE "scoop\apps\godot-mono\current\godot.exe")
    )
    foreach ($path in $knownWindowsPaths) {
        [void]$candidates.Add($path)
    }

    if (Test-Path -LiteralPath "C:\Tools" -PathType Container) {
        Get-ChildItem -LiteralPath "C:\Tools" -Directory -Filter "Godot_v4.7*-mono_win64" `
            -ErrorAction SilentlyContinue |
            ForEach-Object {
                Get-ChildItem -LiteralPath $_.FullName -File -Filter "Godot*_mono_win64*.exe" `
                    -ErrorAction SilentlyContinue |
                    Sort-Object { $_.Name -notlike "*_console.exe" } |
                    ForEach-Object { [void]$candidates.Add($_.FullName) }
            }
    }

    foreach ($candidate in $candidates | Select-Object -Unique) {
        if ([string]::IsNullOrWhiteSpace($candidate) -or
            -not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
            continue
        }

        $resolved = (Resolve-Path -LiteralPath $candidate).Path
        $probe = $resolved
        if ($resolved -notlike "*_console.exe") {
            $consoleProbe = Join-Path (Split-Path -Parent $resolved) `
                (([IO.Path]::GetFileNameWithoutExtension($resolved)) + "_console.exe")
            if (Test-Path -LiteralPath $consoleProbe -PathType Leaf) {
                $probe = $consoleProbe
            }
        }

        $versionOutput = & $probe --version 2>$null
        $probeExitCode = $LASTEXITCODE
        $version = $versionOutput | Select-Object -First 1
        if ($probeExitCode -eq 0 -and $version -like "4.7.*" -and $version -match "mono") {
            return $probe
        }
    }

    throw @"
Godot 4.7 .NET was not found. Install the .NET/Mono build under C:\Tools,
put it on PATH, or pass its executable once with -GodotBin. GODOT_BIN remains
supported for CI and custom installations but is not required for local play.
"@
}
