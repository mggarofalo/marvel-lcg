[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Root,
    [Parameter(Mandatory)][string]$Image,
    [Parameter(Mandatory)][string]$GodotBin,
    [Parameter(Mandatory)][string]$ArtifactCommit,
    [string]$ReplayContract = 'engine-replay-v28'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$Root = [IO.Path]::GetFullPath($Root)
$identity = docker image inspect $Image | ConvertFrom-Json
if ($LASTEXITCODE -ne 0 -or $identity[0].Config.Labels.'org.opencontainers.image.revision' -cne $ArtifactCommit) {
    throw 'Server image does not identify the selected source commit.'
}
$Image = $identity[0].Id
$imageUser = $identity[0].Config.User
if ($imageUser -cnotmatch '^[1-9][0-9]*$') {
    throw 'The packaged authority must identify its non-root numeric user.'
}
& "$PSScriptRoot/campaign-upgrade-native.ps1" -Journey Prepare -Root $Root -Endpoint tcp://127.0.0.1:41953
$identity | ConvertTo-Json -Depth 30 | Set-Content "$Root/server-image.json"
$banner = docker run --rm --entrypoint dotnet $Image Marvel.Server.Host.dll --version
if ($LASTEXITCODE -ne 0 -or $banner -notmatch [regex]::Escape($ReplayContract)) {
    throw 'Server banner does not match the selected replay contract.'
}
$banner | Set-Content "$Root/server-version.txt"
$containerName = 'marvel-upgrade-' + [guid]::NewGuid().ToString('N')
$mount = "type=bind,source=$Root/authority,target=/var/lib/marvel/sessions"
try {
    # Only the new isolated fixture root changes ownership; the image keeps its configured user.
    docker run --rm --user 0 --mount $mount --entrypoint chown $Image -R "${imageUser}:${imageUser}" /var/lib/marvel/sessions
    if ($LASTEXITCODE -ne 0) { throw 'Could not grant the image user access to its isolated fixture.' }
    docker create --name $containerName --publish 127.0.0.1:41953:41923 --mount $mount $Image | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Could not create the selected server image.' }
    foreach ($journey in @('inspect', 'continue', 'restart')) {
        docker start $containerName | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Could not restart the saved campaign authority.' }
        $healthy = $false
        for ($attempt = 0; $attempt -lt 60; $attempt++) {
            $status = docker inspect $containerName | ConvertFrom-Json
            if ($status[0].State.Health.Status -ceq 'healthy') { $healthy = $true; break }
            if (-not $status[0].State.Running) { break }
            Start-Sleep -Seconds 1
        }
        if (-not $healthy) { throw 'Packaged authority did not become healthy.' }
        & "$PSScriptRoot/campaign-upgrade-development.ps1" -Journey $journey -Root $Root `
            -GodotBin $GodotBin -ReplayContract $ReplayContract -ServerContainer $containerName
    }
}
finally {
    docker logs $containerName *> "$Root/server-final.log"
    docker rm --force $containerName | Out-Null
}
