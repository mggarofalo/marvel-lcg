[CmdletBinding()]
param([string]$Revision = $env:MARVEL_ARCHITECTURE_BASE)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# PRs compare the target commit; main pushes compare their previous head.
# Manual/release certification compares the certified commit's parent.
if ([string]::IsNullOrWhiteSpace($Revision) -or $Revision -match '^0+$') {
    $Revision = & git rev-parse 'HEAD^'
    if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve the architecture comparison parent.' }
}
if ($Revision -notmatch '^[0-9a-fA-F]{40}$') {
    throw 'The architecture comparison must be a full commit hash.'
}
& git fetch --no-tags --depth=1 origin $Revision
if ($LASTEXITCODE -ne 0) { throw 'Cannot fetch the architecture comparison commit.' }
& git cat-file -e "${Revision}:tests/Marvel.Architecture.Tests/source-layout-baseline.txt"
if ($LASTEXITCODE -ne 0) { throw 'The architecture comparison has no reviewed source baseline.' }
if (-not $env:GITHUB_ENV) { throw 'This CI setup script requires GITHUB_ENV; local tests use the branch merge base.' }
"MARVEL_ARCHITECTURE_BASE=$Revision" | Out-File -FilePath $env:GITHUB_ENV -Encoding utf8 -Append
Write-Output "Architecture debt is compared against $Revision"
