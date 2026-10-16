param([string]$ReleaseEvent, [string]$Executable, [string]$ArgumentsBase64)
$ErrorActionPreference = 'Stop'
# The parent assigns this waiting process to its kill-on-close job before allowing descendants.
$gate = [Threading.EventWaitHandle]::OpenExisting($ReleaseEvent)
try { if (-not $gate.WaitOne(15000)) { throw 'Native process ownership was not established.' } }
finally { $gate.Dispose() }
$arguments = @([Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($ArgumentsBase64)) | ConvertFrom-Json)
& $Executable @arguments
exit $LASTEXITCODE
