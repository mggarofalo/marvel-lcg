[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GodotBin,
    [Parameter(Mandatory = $true)][string]$Version,
    [Parameter(Mandatory = $true)][string]$Commit,
    [Parameter(Mandatory = $true)][string]$Output,
    [string]$Publisher
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Invoke-Checked {
    param([Parameter(Mandatory = $true)][scriptblock]$Command)
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "external command failed with exit code $LASTEXITCODE"
    }
}

function Copy-SourceTree {
    param([string]$Source, [string]$Destination)
    New-Item -ItemType Directory -Path $Destination | Out-Null
    & robocopy $Source $Destination /E /NFL /NDL /NJH /NJS /NP /XD bin obj .godot | Out-Null
    if ($LASTEXITCODE -gt 7) {
        throw "source staging failed with robocopy exit code $LASTEXITCODE"
    }
}

function Find-WindowsSdkTool {
    param([string]$Name)
    $root = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'
    $tool = Get-ChildItem -LiteralPath $root -Recurse -Filter $Name -File |
        Where-Object { $_.DirectoryName -match '[\\/]x64$' } |
        Sort-Object FullName -Descending |
        Select-Object -First 1
    if ($null -eq $tool) {
        throw "$Name was not found in the Windows SDK"
    }
    return $tool.FullName
}

function New-Logo {
    param([int]$Size, [string]$Path)
    Add-Type -AssemblyName System.Drawing
    $bitmap = [System.Drawing.Bitmap]::new($Size, $Size)
    try {
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.Clear([System.Drawing.Color]::FromArgb(9, 14, 21))
            $brush = [System.Drawing.SolidBrush]::new(
                [System.Drawing.Color]::FromArgb(224, 174, 82))
            try {
                $margin = [Math]::Max(2, [int]($Size / 7))
                $graphics.FillRectangle(
                    $brush,
                    $margin,
                    $margin,
                    $Size - (2 * $margin),
                    $Size - (2 * $margin))
            }
            finally {
                $brush.Dispose()
            }
        }
        finally {
            $graphics.Dispose()
        }
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $bitmap.Dispose()
    }
}

if (-not (Test-Path -LiteralPath $GodotBin -PathType Leaf)) {
    throw 'Godot editor executable was not found'
}
if ($Commit -cnotmatch '^[0-9a-f]{40}$') {
    throw 'commit must be 40 lowercase hexadecimal characters'
}

$repository = Split-Path -Parent $PSScriptRoot
$head = (& git -C $repository rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $head -cne $Commit) {
    throw 'commit does not identify the checked-out source'
}
$dirty = & git -C $repository status --porcelain --untracked-files=all
if ($LASTEXITCODE -ne 0 -or $dirty) {
    throw 'desktop artifacts require a clean checkout'
}

$outputParent = Split-Path -Parent ([IO.Path]::GetFullPath($Output))
if (-not (Test-Path -LiteralPath $outputParent -PathType Container)) {
    throw 'output parent directory does not exist'
}
$output = Join-Path $outputParent (Split-Path -Leaf $Output)
if (Test-Path -LiteralPath $output) {
    throw 'output already exists'
}

$versionMatch = [regex]::Match(
    $Version,
    '^(?<major>0|[1-9][0-9]*)\.(?<minor>0|[1-9][0-9]*)\.(?<patch>0|[1-9][0-9]*)(?:-(?<channel>preview|dev)\.(?<sequence>0|[1-9][0-9]*))?$')
if (-not $versionMatch.Success) {
    throw 'version is not a supported product version'
}
$baseVersion = '{0}.{1}.{2}' -f `
    $versionMatch.Groups['major'].Value, `
    $versionMatch.Groups['minor'].Value, `
    $versionMatch.Groups['patch'].Value
$channel = switch ($versionMatch.Groups['channel'].Value) {
    'preview' { 'preview' }
    'dev' { 'developer' }
    default { 'stable' }
}

if ($channel -ne 'developer' -and -not $Publisher) {
    throw 'preview and stable package inputs require Publisher'
}

$scratch = Join-Path ([IO.Path]::GetTempPath()) `
    ('marvel-windows-desktop-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $scratch | Out-Null
try {
    $sourceRoot = Join-Path $scratch 'source'
    New-Item -ItemType Directory -Path $sourceRoot | Out-Null
    foreach ($file in @(
        'Directory.Build.props',
        'Directory.Build.targets',
        'Directory.Packages.props',
        'global.json')) {
        Copy-Item -LiteralPath (Join-Path $repository $file) -Destination $sourceRoot
    }
    Copy-SourceTree (Join-Path $repository 'src') (Join-Path $sourceRoot 'src')
    New-Item -ItemType Directory -Path (Join-Path $sourceRoot 'tools') | Out-Null
    Copy-SourceTree `
        (Join-Path $repository 'tools\Marvel.Release') `
        (Join-Path $sourceRoot 'tools\Marvel.Release')

    $projectSettings = Join-Path $sourceRoot 'src\Marvel.Godot\project.godot'
    $projectText = [IO.File]::ReadAllText($projectSettings)
    $projectText = [regex]::Replace(
        $projectText,
        'config/version="[^"]*"',
        "config/version=`"$baseVersion`"")
    [IO.File]::WriteAllText($projectSettings, $projectText)

    $env:MarvelProductVersion = $Version
    $env:MarvelCommit = $Commit
    $env:MarvelVersionMajor = $versionMatch.Groups['major'].Value
    $env:MarvelVersionMinor = $versionMatch.Groups['minor'].Value
    $env:MarvelVersionPatch = $versionMatch.Groups['patch'].Value
    $env:MarvelSourceRoot = $sourceRoot

    $godotProject = Join-Path $sourceRoot 'src\Marvel.Godot'
    Invoke-Checked { dotnet new sln --name Marvel.Godot --format sln --output $godotProject }
    Invoke-Checked {
        dotnet sln (Join-Path $godotProject 'Marvel.Godot.sln') add `
            (Join-Path $godotProject 'Marvel.Godot.csproj')
    }
    Invoke-Checked { dotnet restore (Join-Path $godotProject 'Marvel.Godot.csproj') }
    $payload = Join-Path $scratch 'payload'
    New-Item -ItemType Directory -Path $payload | Out-Null
    $executable = Join-Path $payload 'MarvelChampions.exe'
    Invoke-Checked {
        & $GodotBin --headless --path $godotProject `
            --export-release 'Windows Desktop' $executable
    }
    if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
        throw 'Godot did not produce a Windows executable'
    }
    $managedApplication = Get-ChildItem -LiteralPath $payload -Recurse `
        -Filter 'Marvel.Godot.dll' -File | Select-Object -First 1
    if ($null -eq $managedApplication) {
        throw 'Godot export omitted the managed application'
    }

    foreach ($dataset in @('cards', 'setup', 'abilities')) {
        $destination = Join-Path $payload "datasets\$dataset"
        New-Item -ItemType Directory -Path $destination | Out-Null
        Copy-Item `
            -LiteralPath (Join-Path $repository "datasets\$dataset\$dataset.json") `
            -Destination (Join-Path $destination "$dataset.json")
    }

    $releaseProject = Join-Path $sourceRoot 'tools\Marvel.Release\Marvel.Release.csproj'
    Invoke-Checked { dotnet restore $releaseProject }
    Invoke-Checked {
        dotnet build $releaseProject --configuration Release --no-restore `
            -p:MarvelProductVersion=$Version `
            -p:MarvelCommit=$Commit `
            -p:MarvelVersionMajor=$env:MarvelVersionMajor `
            -p:MarvelVersionMinor=$env:MarvelVersionMinor `
            -p:MarvelVersionPatch=$env:MarvelVersionPatch
    }
    $releaseTool = Join-Path $sourceRoot 'tools\Marvel.Release\bin\Release\net8.0\Marvel.Release.dll'
    Invoke-Checked {
        dotnet $releaseTool manifest `
            --version $Version `
            --commit $Commit `
            --data-root $repository `
            --output (Join-Path $payload 'release-manifest.json')
    }

    if ($channel -eq 'developer') {
        $msixVersion = '1.0.0.0'
        $packageName = 'mggarofalo.MarvelChampions.Developer.' + $Commit.Substring(0, 12)
        $manifestPublisher = `
            'CN=Marvel Champions Developer, OID.2.25.311729368913984317654407730594956997722=1'
    }
    else {
        $msixVersion = (& dotnet $releaseTool msix-version $Version).Trim()
        if ($LASTEXITCODE -ne 0) { throw 'MSIX version mapping failed' }
        $packageName = 'mggarofalo.MarvelChampions'
        $manifestPublisher = $Publisher
    }

    $assets = Join-Path $payload 'Assets'
    New-Item -ItemType Directory -Path $assets | Out-Null
    New-Logo 44 (Join-Path $assets 'Square44x44Logo.png')
    New-Logo 150 (Join-Path $assets 'Square150x150Logo.png')
    New-Logo 50 (Join-Path $assets 'StoreLogo.png')

    $escapedPublisher = [Security.SecurityElement]::Escape($manifestPublisher)
    $appxManifest = @"
<?xml version="1.0" encoding="utf-8"?>
<Package
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  xmlns:uap10="http://schemas.microsoft.com/appx/manifest/uap/windows10/10"
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
  IgnorableNamespaces="uap uap10 rescap">
  <Identity Name="$packageName" Publisher="$escapedPublisher" Version="$msixVersion" ProcessorArchitecture="x64" />
  <Properties>
    <DisplayName>Marvel Champions</DisplayName>
    <PublisherDisplayName>mggarofalo</PublisherDisplayName>
    <Description>Marvel Champions desktop client</Description>
    <Logo>Assets\StoreLogo.png</Logo>
  </Properties>
  <Resources>
    <Resource Language="en-us" />
  </Resources>
  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.19041.0" MaxVersionTested="10.0.26100.0" />
  </Dependencies>
  <Applications>
    <Application Id="MarvelChampions" Executable="MarvelChampions.exe" uap10:RuntimeBehavior="packagedClassicApp" uap10:TrustLevel="mediumIL">
      <uap:VisualElements DisplayName="Marvel Champions" Description="Marvel Champions desktop client" BackgroundColor="#090E15" Square44x44Logo="Assets\Square44x44Logo.png" Square150x150Logo="Assets\Square150x150Logo.png" />
    </Application>
  </Applications>
  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>
</Package>
"@
    [IO.File]::WriteAllText(
        (Join-Path $payload 'AppxManifest.xml'),
        $appxManifest,
        [Text.UTF8Encoding]::new($false))

    $payloadHashFile = Join-Path $payload 'payload-sha256.txt'
    $hashLines = Get-ChildItem -LiteralPath $payload -Recurse -File |
        Where-Object { $_.FullName -cne $payloadHashFile } |
        Sort-Object FullName |
        ForEach-Object {
            $relative = [IO.Path]::GetRelativePath($payload, $_.FullName).Replace('\', '/')
            '{0} *{1}' -f (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $relative
        }
    [IO.File]::WriteAllLines($payloadHashFile, $hashLines, [Text.UTF8Encoding]::new($false))

    $commitEpoch = [long](& git -C $repository show -s --format=%ct $Commit)
    if ($LASTEXITCODE -ne 0) { throw 'source commit timestamp was not available' }
    $commitTime = [DateTimeOffset]::FromUnixTimeSeconds($commitEpoch).UtcDateTime
    Get-ChildItem -LiteralPath $payload -Recurse -Force | ForEach-Object {
        $_.LastWriteTimeUtc = $commitTime
    }

    $makeAppx = Find-WindowsSdkTool 'makeappx.exe'
    New-Item -ItemType Directory -Path $output | Out-Null
    $package = Join-Path $output "MarvelChampions-$Version-windows-x64-unsigned.msix"
    Invoke-Checked { & $makeAppx pack /d $payload /p $package /o /h SHA256 }

    $unpacked = Join-Path $scratch 'unpacked'
    Invoke-Checked { & $makeAppx unpack /p $package /d $unpacked /o }
    foreach ($line in Get-Content -LiteralPath (Join-Path $unpacked 'payload-sha256.txt')) {
        if ($line -cnotmatch '^(?<hash>[0-9a-f]{64}) \*(?<path>.+)$') {
            throw 'packaged payload hash manifest is malformed'
        }
        $unpackedFile = Join-Path $unpacked $Matches['path'].Replace('/', '\')
        $actual = (Get-FileHash $unpackedFile -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actual -cne $Matches['hash']) {
            throw "packaged payload differs at $($Matches['path'])"
        }
    }
    $signature = Join-Path $unpacked 'AppxSignature.p7x'
    if (Test-Path -LiteralPath $signature -PathType Leaf) {
        throw 'unsigned package input unexpectedly contains a signature'
    }

    $packageHash = (Get-FileHash $package -Algorithm SHA256).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText(
        "$package.sha256",
        "$packageHash *$(Split-Path -Leaf $package)`n",
        [Text.UTF8Encoding]::new($false))
    Write-Output $package
}
finally {
    Remove-Item -LiteralPath $scratch -Recurse -Force -ErrorAction SilentlyContinue
}
