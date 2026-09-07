param(
    [Parameter(Mandatory = $false)]
    [string] $Configuration = 'Release',

    [Parameter(Mandatory = $false)]
    [string] $OutputDirectory = "",

    [Parameter(Mandatory = $false)]
    [string] $CollectionZipName = 'Ratopia-Mod-202609072051.zip',

    [Parameter(Mandatory = $false)]
    [switch] $SkipBuild
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repoRoot 'artifacts'
}
$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)

$releaseSlugs = @{
    'BroadcastStationGlobalCoverage' = 'broadcaststationglobalcoverage'
    'EquipmentReforgeDodge' = 'equipmentreforgedodge'
    'EquipmentReforgeSelector' = 'equipmentreforgeselector'
    'ExecutionPlatform' = 'executionplatform'
    'GodViewManagement' = 'godviewmanagement'
    'HeaterEnhancement' = 'heaterenhancement'
    'MultipurposeResearch' = 'multipurposeresearch'
    'PopulationCustomizer' = 'populationcustomizer'
    'RatopiaMod.YunQing.All' = 'yunqing'
    'ResearchAndTradeOptimization' = 'researchandtradeoptimization'
    'RestroomBathFun' = 'restroombathfun'
    'SharedWarehouse' = 'sharedwarehouse'
    'SleepAcceleration' = 'sleepacceleration'
    'SpecialRatizens' = 'specialratizens'
    'StrongerWorkDistance' = 'strongerworkdistance'
    'SuperBow' = 'superbow'
    'WireThroughWalls' = 'wirethroughwalls'
}

function New-ZipFromDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string] $SourceDirectory,

        [Parameter(Mandatory = $true)]
        [string] $DestinationZip
    )

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $destinationZip = [System.IO.Path]::GetFullPath($DestinationZip)
    New-Item -ItemType Directory -Force (Split-Path -Parent $destinationZip) | Out-Null
    if (Test-Path -LiteralPath $destinationZip) {
        Remove-Item -LiteralPath $destinationZip -Force
    }

    $archive = [System.IO.Compression.ZipFile]::Open($destinationZip, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in Get-ChildItem -LiteralPath $SourceDirectory -Recurse -File) {
            $entryName = $file.FullName.Substring($SourceDirectory.Length).TrimStart('\', '/').Replace('\', '/')
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $archive,
                $file.FullName,
                $entryName,
                [System.IO.Compression.CompressionLevel]::Optimal
            ) | Out-Null
        }
    }
    finally {
        $archive.Dispose()
    }
}

$projects = Get-ChildItem -Path $repoRoot -Recurse -Filter '*.csproj' |
    Where-Object { $_.FullName -notmatch '[\\\\/](artifacts|bin|obj|tests)[\\\\/]' } |
    Sort-Object FullName

if (-not $projects) {
    throw 'No plugin projects were found.'
}

if (-not $SkipBuild) {
    foreach ($project in $projects) {
        Write-Host "=== BUILD $($project.BaseName) ==="
        dotnet build $project.FullName -c $Configuration -p:InstallAfterBuild=false -p:DisableRatopiaModInstall=true --nologo
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed for $($project.FullName)"
        }
    }
}

$collectionRoot = Join-Path $OutputDirectory 'collection-package'
if (Test-Path -LiteralPath $collectionRoot) {
    Remove-Item -LiteralPath $collectionRoot -Recurse -Force
}
New-Item -ItemType Directory -Force $collectionRoot | Out-Null

$mods = @()
foreach ($project in $projects) {
    [xml] $projectXml = Get-Content -LiteralPath $project.FullName
    $assemblyNameNode = $projectXml.SelectSingleNode('//PropertyGroup/AssemblyName')
    $versionNode = $projectXml.SelectSingleNode('//PropertyGroup/Version')
    if (-not $assemblyNameNode) { throw "Cannot read <AssemblyName> from $($project.FullName)" }
    if (-not $versionNode) { throw "Cannot read <Version> from $($project.FullName)" }

    $assemblyName = $assemblyNameNode.InnerText.Trim()
    $version = $versionNode.InnerText.Trim()
    $pluginDll = Get-ChildItem -LiteralPath (Join-Path $project.DirectoryName 'bin') -Recurse -File -Filter "$assemblyName.dll" |
        Where-Object { $_.FullName -match "[\\\\/]$Configuration[\\\\/]" } |
        Sort-Object FullName -Descending |
        Select-Object -First 1
    if (-not $pluginDll) {
        throw "Cannot find $assemblyName.dll in $($project.DirectoryName)/bin/$Configuration"
    }

    $outputDir = $pluginDll.DirectoryName
    $filesToPackage = Get-ChildItem -LiteralPath $outputDir -File |
        Where-Object { $_.Extension -notin @('.pdb', '.xml') }

    $pluginDir = Join-Path $collectionRoot "BepInEx/plugins/$assemblyName"
    New-Item -ItemType Directory -Force $pluginDir | Out-Null
    foreach ($file in $filesToPackage) {
        Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $pluginDir $file.Name) -Force
    }

    $singlePackageRoot = Join-Path $OutputDirectory "single-package/$assemblyName"
    if (Test-Path -LiteralPath $singlePackageRoot) {
        Remove-Item -LiteralPath $singlePackageRoot -Recurse -Force
    }
    $singlePluginDir = Join-Path $singlePackageRoot "BepInEx/plugins/$assemblyName"
    New-Item -ItemType Directory -Force $singlePluginDir | Out-Null
    foreach ($file in $filesToPackage) {
        Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $singlePluginDir $file.Name) -Force
    }

    $assetName = "$assemblyName-v$version.zip"
    New-ZipFromDirectory -SourceDirectory $singlePackageRoot -DestinationZip (Join-Path $OutputDirectory $assetName)

    $slug = $releaseSlugs[$assemblyName]
    if (-not $slug) { $slug = $assemblyName.ToLowerInvariant() }

    $mods += [ordered]@{
        assemblyName = $assemblyName
        version = $version
        releaseSlug = $slug
        asset = $assetName
    }
}

$manifest = [ordered]@{
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    collectionZip = $CollectionZipName
    mods = $mods
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'artifacts.json') -Encoding UTF8

New-ZipFromDirectory -SourceDirectory $collectionRoot -DestinationZip (Join-Path $OutputDirectory $CollectionZipName)

Write-Host ''
Write-Host "Built $($mods.Count) mod package(s)."
Write-Host "Collection package: $(Join-Path $OutputDirectory $CollectionZipName)"
Write-Host "Manifest: $(Join-Path $OutputDirectory 'artifacts.json')"
