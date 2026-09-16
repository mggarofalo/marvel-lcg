param(
    [Parameter(Mandatory = $false)]
    [string]$GodotBin,
    [Parameter(Mandatory = $false)]
    [switch]$NoBuild,
    [Parameter(Mandatory = $false)]
    [switch]$Editor
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src/Marvel.Godot"
. "$PSScriptRoot/godot-local.ps1"

$resolvedGodot = Resolve-GodotDotNetExecutable -GodotBin $GodotBin
if (-not $NoBuild) {
    dotnet build "$project/Marvel.Godot.csproj" --nologo
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

$arguments = @("--path", $project)
if ($Editor) {
    $arguments += "--editor"
}

Write-Host "Starting Marvel Champions with $resolvedGodot"
& $resolvedGodot @arguments
exit $LASTEXITCODE
