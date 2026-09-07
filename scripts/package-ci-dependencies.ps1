param(
    [Parameter(Mandatory = $false)]
    [string] $RatopiaDir = $env:RATOPIA_DIR,

    [Parameter(Mandatory = $false)]
    [string] $OutputZip = ""
)

$ErrorActionPreference = 'Stop'

if (-not $RatopiaDir) {
    throw "Set RATOPIA_DIR or pass -RatopiaDir. Example: C:\Program Files (x86)\Steam\steamapps\common\Ratopia"
}

$RatopiaDir = (Resolve-Path $RatopiaDir).Path
$repoRoot = Split-Path -Parent $PSScriptRoot
$projectRoot = Join-Path $repoRoot 'RatopiaMod.Nyaiko.YunQing'
if (-not $OutputZip) {
    $OutputZip = Join-Path $projectRoot 'artifacts\ratopia-ci-deps.zip'
}
$bundleDir = Join-Path $projectRoot 'artifacts\ratopia-ci-deps'
$msbuildVariables = @{
    RatopiaDir = $RatopiaDir
    BepInExCoreDir = Join-Path $RatopiaDir 'BepInEx\core'
    GameManagedDir = Join-Path $RatopiaDir 'Ratopia_Data\Managed'
}

$projectFiles = @(
    (Join-Path $projectRoot 'Directory.Build.props'),
    (Join-Path $projectRoot 'RatopiaMod.YunQing.All\RatopiaMod.YunQing.All.csproj')
)

if (Test-Path $bundleDir) {
    Remove-Item $bundleDir -Recurse -Force
}
New-Item -ItemType Directory -Force $bundleDir | Out-Null

$copied = 0
foreach ($projectFile in $projectFiles) {
    [xml] $projectXml = Get-Content $projectFile
    foreach ($reference in $projectXml.SelectNodes('//Reference')) {
        $hintPath = $reference.HintPath
        if (-not $hintPath) { continue }

        foreach ($name in $msbuildVariables.Keys) {
            $hintPath = $hintPath.Replace("`$($name)", $msbuildVariables[$name])
        }

        if (-not [System.IO.Path]::IsPathRooted($hintPath)) {
            $hintPath = Join-Path $projectRoot $hintPath
        }

        if (-not (Test-Path $hintPath)) {
            throw "Missing referenced assembly: $hintPath"
        }

        $sourcePath = (Resolve-Path $hintPath).Path
        $relativePath = $sourcePath.Substring($RatopiaDir.Length).TrimStart('\', '/')
        $destinationPath = Join-Path $bundleDir $relativePath
        New-Item -ItemType Directory -Force (Split-Path -Parent $destinationPath) | Out-Null
        Copy-Item $sourcePath $destinationPath -Force
        $copied++
    }
}

if ($copied -eq 0) {
    throw 'No assembly references were found. Check Directory.Build.props and the project file.'
}

$outputZip = [System.IO.Path]::GetFullPath($OutputZip)
New-Item -ItemType Directory -Force (Split-Path -Parent $outputZip) | Out-Null
if (Test-Path $outputZip) {
    Remove-Item $outputZip -Force
}

# Compress-Archive on Windows PowerShell 5.1 emits backslash entry names,
# which unzip on Linux rejects. Create a ZIP with forward-slash entry names.
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::Open(
    $outputZip,
    [System.IO.Compression.ZipArchiveMode]::Create
)
try {
    foreach ($file in Get-ChildItem -Path $bundleDir -Recurse -File) {
        $entryName = $file.FullName.Substring($bundleDir.Length).TrimStart('\', '/').Replace('\', '/')
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

Write-Host "Copied $copied reference DLL(s)."
Write-Host "Dependency bundle: $outputZip"
Write-Host 'Upload this zip to a PRIVATE repository release for GitHub Actions.'
