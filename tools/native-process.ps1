if (-not ('Marvel.Tools.NativeProcessScope' -as [type])) { Add-Type -Path "$PSScriptRoot/NativeProcessScope.cs" }

function Invoke-NativeProcess {
    param([string]$Executable, [string[]]$Arguments, [hashtable]$Environment,
        [int]$TimeoutSeconds, [string]$LogPath, [string]$WorkingDirectory)

    $gate = $null
    $scope = $null
    if ($IsWindows) {
        $gateName = 'Local\marvel-native-' + [guid]::NewGuid().ToString('N')
        $gate = [Threading.EventWaitHandle]::new($false, [Threading.EventResetMode]::ManualReset, $gateName)
        $encoded = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes((ConvertTo-Json -InputObject @($Arguments) -Compress)))
        $processArguments = @('-NoProfile', '-File', "$PSScriptRoot/native-process-child.ps1", '-ReleaseEvent', $gateName,
            '-Executable', $Executable, '-ArgumentsBase64', $encoded)
        $start = [Diagnostics.ProcessStartInfo]::new((Get-Process -Id $PID).Path)
    }
    else {
        $start = [Diagnostics.ProcessStartInfo]::new('setsid')
        $processArguments = @('--wait', '--', $Executable) + $Arguments
    }
    $start.WorkingDirectory = $WorkingDirectory
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($argument in $processArguments) { $start.ArgumentList.Add($argument) }
    # Each case owns these inputs, including absent inputs. Ambient smoke state cannot alter membership.
    foreach ($key in @($start.Environment.Keys)) {
        if ($key -match '^MARVEL_(SMOKE_|B1_|TABLEAU_|LEGAL_TABLEAU_|RECIPIENT_|MATERIAL_|NATIVE_|ENGINE_ENDPOINT$|TELEMETRY_|UI_SCALE$)') {
            $start.Environment.Remove($key) | Out-Null
        }
    }
    foreach ($key in $Environment.Keys) { $start.Environment[$key] = [string]$Environment[$key] }
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $start
    $watch = [Diagnostics.Stopwatch]::StartNew()
    try {
        if (-not $process.Start()) { throw "Could not launch $Executable" }
        $scope = [Marvel.Tools.NativeProcessScope]::new($process, $IsWindows)
        if ($gate) { $gate.Set() | Out-Null }
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        $timedOut = -not $process.WaitForExit([Math]::Max(1, $TimeoutSeconds * 1000 - [int]$watch.ElapsedMilliseconds))
        $remaining = [Math]::Max(1, $TimeoutSeconds * 1000 - [int]$watch.ElapsedMilliseconds)
        $drained = [Threading.Tasks.Task]::WhenAll([Threading.Tasks.Task[]]@($stdout, $stderr))
        if (-not $timedOut) { $timedOut = -not $drained.Wait($remaining) }
        $scope.Dispose()
        $scope = $null
        if ($timedOut) { $process.WaitForExit(5000) | Out-Null }
        # Pipe holders belong to the terminated scope. Bound drain even if an OS pipe misbehaves.
        $drained.Wait(5000) | Out-Null
        $output = if ($stdout.IsCompletedSuccessfully) { $stdout.Result } else { '[stdout drain timed out]' }
        $output += if ($stderr.IsCompletedSuccessfully) { $stderr.Result } else { '[stderr drain timed out]' }
        [IO.File]::WriteAllText($LogPath, $output)
        Write-Host $output
        $exitCode = if ($process.HasExited) { $process.ExitCode } else { -1 }
        return @{ ExitCode = $exitCode; TimedOut = $timedOut; Output = $output;
            ElapsedSeconds = [Math]::Round($watch.Elapsed.TotalSeconds, 3) }
    }
    finally {
        if ($scope) { $scope.Dispose() }
        if ($gate) { $gate.Dispose() }
        $process.Dispose()
    }
}
