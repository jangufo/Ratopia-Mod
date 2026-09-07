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
if (-not $OutputZip) {
    $OutputZip = Join-Path $repoRoot 'artifacts/ratopia-ci-deps.zip'
}

$bundleDir = Join-Path $repoRoot 'artifacts/ratopia-ci-deps'
if (Test-Path $bundleDir) {
    Remove-Item -LiteralPath $bundleDir -Recurse -Force
}
New-Item -ItemType Directory -Force $bundleDir | Out-Null

$variables = @{
    'RatopiaDir' = ($RatopiaDir -replace '\\\\','/')
    'BepInExCoreDir' = (($RatopiaDir -replace '\\\\','/') + '/BepInEx/core')
    'GameManagedDir' = (($RatopiaDir -replace '\\\\','/') + '/Ratopia_Data/Managed')
}

$projectFiles = @()
$projectFiles += Get-ChildItem -Path $repoRoot -Recurse -Filter '*.csproj' |
    Where-Object { $_.FullName -notmatch '[\\\\/](artifacts|bin|obj)[\\\\/]' } |
    Select-Object -ExpandProperty FullName
$projectFiles += Get-ChildItem -Path $repoRoot -Recurse -Filter 'Directory.Build.props' |
    Where-Object { $_.FullName -notmatch '[\\\\/](artifacts|bin|obj)[\\\\/]' } |
    Select-Object -ExpandProperty FullName
$projectFiles = $projectFiles | Sort-Object -Unique

$copiedFiles = @{}
foreach ($projectFile in $projectFiles) {
    [xml] $projectXml = Get-Content -LiteralPath $projectFile
    foreach ($reference in $projectXml.SelectNodes('//Reference')) {
        $hintPath = $reference.HintPath
        if (-not $hintPath) { continue }

        foreach ($name in $variables.Keys) {
            $hintPath = $hintPath.Replace("`$($name)", $variables[$name])
        }

        $hintPath = $hintPath -replace '\\\\','/'
        if (-not [System.IO.Path]::IsPathRooted($hintPath)) {
            $hintPath = Join-Path (Split-Path -Parent $projectFile) $hintPath
        }

        if (-not (Test-Path -LiteralPath $hintPath)) {
            throw "Missing referenced assembly: $hintPath"
        }

        $sourcePath = (Resolve-Path -LiteralPath $hintPath).Path
        $relativePath = $sourcePath.Substring($RatopiaDir.Length).TrimStart('\', '/')
        if ($copiedFiles.ContainsKey($relativePath)) { continue }

        $destinationPath = Join-Path $bundleDir $relativePath
        New-Item -ItemType Directory -Force (Split-Path -Parent $destinationPath) | Out-Null
        Copy-Item -LiteralPath $sourcePath -Destination $destinationPath -Force
        $copiedFiles[$relativePath] = $sourcePath
    }
}

if ($copiedFiles.Count -eq 0) {
    throw 'No assembly references were found.'
}

$manifest = [ordered]@{
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    assemblyCSharpSha256 = (Get-FileHash -LiteralPath (Join-Path $RatopiaDir 'Ratopia_Data/Managed/Assembly-CSharp.dll') -Algorithm SHA256).Hash
    references = @($copiedFiles.Keys | Sort-Object)
}
$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $bundleDir 'manifest.json') -Encoding UTF8

$outputZip = [System.IO.Path]::GetFullPath($OutputZip)
New-Item -ItemType Directory -Force (Split-Path -Parent $outputZip) | Out-Null
if (Test-Path -LiteralPath $outputZip) {
    Remove-Item -LiteralPath $outputZip -Force
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::Open($outputZip, [System.IO.Compression.ZipArchiveMode]::Create)
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

Write-Host "Copied $($copiedFiles.Count) reference DLL(s)."
Write-Host "Dependency bundle: $outputZip"
Write-Host 'Upload this zip to a PRIVATE repository release for GitHub Actions.'
