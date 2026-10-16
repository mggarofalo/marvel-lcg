$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/native-process.ps1"
$directory = Join-Path ([IO.Path]::GetTempPath()) ('marvel-process-test-' + [guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($directory) | Out-Null
$shell = (Get-Process -Id $PID).Path
$ambient = $env:MARVEL_ENGINE_ENDPOINT
try {
    $single = Invoke-NativeProcess $shell @('--version') @{} 15 (Join-Path $directory 'single.log') $directory
    if ($single.TimedOut -or $single.ExitCode -ne 0 -or $single.Output -notmatch '^PowerShell 7\.') {
        throw 'One argument did not reach the owned executable intact.'
    }
    $env:MARVEL_ENGINE_ENDPOINT = 'ambient-endpoint-must-not-reach-child'
    $normalScript = Join-Path $directory 'normal.ps1'
    [IO.File]::WriteAllText($normalScript, @'
if ($env:MARVEL_ENGINE_ENDPOINT) { throw 'Inherited an ambient engine endpoint.' }
if ($env:MARVEL_NATIVE_CONTRACT -ne 'explicit-case') { throw 'Explicit case input was lost.' }
Write-Output 'NORMAL_PROCESS_OK'
'@)
    $normal = Invoke-NativeProcess $shell @('-NoProfile', '-File', $normalScript) @{
        MARVEL_NATIVE_CONTRACT = 'explicit-case'
    } 15 (Join-Path $directory 'normal.log') $directory
    if ($normal.TimedOut -or $normal.ExitCode -ne 0 -or -not $normal.Output.Contains('NORMAL_PROCESS_OK')) {
        throw 'Normal completion or isolated case environment failed.'
    }
    $childScript = Join-Path $directory 'pipe-holder.ps1'
    [IO.File]::WriteAllText($childScript, 'Start-Sleep -Seconds 120')
    $rootScript = Join-Path $directory 'root-exits.ps1'
    [IO.File]::WriteAllText($rootScript, @'
param([string]$Shell, [string]$Child)
$start = [Diagnostics.ProcessStartInfo]::new($Shell)
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
$start.ArgumentList.Add('-NoProfile')
$start.ArgumentList.Add('-File')
$start.ArgumentList.Add($Child)
$process = [Diagnostics.Process]::Start($start)
Write-Output "PIPE_HOLDER_PID=$($process.Id)"
Write-Output 'ROOT_EXITING_OK'
exit 0
'@)
    $held = Invoke-NativeProcess $shell @('-NoProfile', '-File', $rootScript, '-Shell', $shell, '-Child', $childScript) @{} 3 (Join-Path $directory 'held.log') $directory
    if (-not $held.TimedOut -or $held.ExitCode -ne 0 -or -not $held.Output.Contains('ROOT_EXITING_OK')) {
        throw 'A child retaining stdout after successful root exit did not hit the drain deadline.'
    }
    if ($held.ElapsedSeconds -gt 13) { throw 'Owned child cleanup exceeded the bounded deadline and cleanup budget.' }
    if ($held.Output -notmatch 'PIPE_HOLDER_PID=(\d+)') { throw 'The pipe-holding child was not observed.' }
    $childId = [int]$Matches[1]
    $child = Get-Process -Id $childId -ErrorAction SilentlyContinue
    if ($child -and -not $child.WaitForExit(5000)) { throw "Owned pipe-holding child survived cleanup: $childId" }
    Write-Host 'NATIVE_PROCESS_TESTS_OK normal-exit=true environment-isolated=true pipe-deadline=true child-terminated=true'
}
finally {
    $env:MARVEL_ENGINE_ENDPOINT = $ambient
    $resolved = [IO.Path]::GetFullPath($directory)
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFileName($resolved) -notlike 'marvel-process-test-*') { throw 'Refusing cleanup outside the owned temporary directory.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
