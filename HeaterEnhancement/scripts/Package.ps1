[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$RatopiaDir = $env:RATOPIA_DIR
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($RatopiaDir)) {
    throw '请通过 -RatopiaDir 或 RATOPIA_DIR 指定 Ratopia 游戏目录。'
}

$projectRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$ratopiaRoot = [IO.Path]::GetFullPath($RatopiaDir)
$solutionPath = Join-Path $projectRoot 'HeaterEnhancement.sln'
$projectPath = Join-Path $projectRoot 'src\HeaterEnhancement\HeaterEnhancement.csproj'
$pluginDll = Join-Path $projectRoot 'src\HeaterEnhancement\bin\Release\net472\HeaterEnhancement.dll'
$stageDir = Join-Path $projectRoot 'artifacts\package'
$pluginStageDir = Join-Path $stageDir 'BepInEx\plugins\HeaterEnhancement'
$distDir = Join-Path $projectRoot 'dist'
$zipPath = Join-Path $distDir '加热器加强优化-v0.1.4-BepInEx5.zip'
$gameAssembly = Join-Path $ratopiaRoot 'Ratopia_Data\Managed\Assembly-CSharp.dll'
$expectedGameHash = 'C94847D858EED368D2082E9715D0C79DD79309631BEF861C6BEBD467306D6E1D'
$packageValidator = 'C:\Users\ASUS\.codex\skills\developing-ratopia-mods\scripts\Test-RatopiaPackage.ps1'

function Assert-ChildPath([string]$Path, [string]$Parent) {
    $fullPath = [IO.Path]::GetFullPath($Path)
    $fullParent = [IO.Path]::GetFullPath($Parent).TrimEnd('\') + '\'
    if (-not $fullPath.StartsWith($fullParent, [StringComparison]::OrdinalIgnoreCase)) {
        throw "路径不在预期项目目录内：$fullPath"
    }
}

Assert-ChildPath -Path $stageDir -Parent $projectRoot
Assert-ChildPath -Path $distDir -Parent $projectRoot
Assert-ChildPath -Path $zipPath -Parent $projectRoot

if (-not (Test-Path -LiteralPath $gameAssembly -PathType Leaf)) {
    throw "找不到游戏程序集：$gameAssembly"
}

$actualGameHash = (Get-FileHash -LiteralPath $gameAssembly -Algorithm SHA256).Hash
if ($actualGameHash -ne $expectedGameHash) {
    throw "游戏程序集版本不匹配。期望 $expectedGameHash，实际 $actualGameHash。"
}

& dotnet test $solutionPath -c Release -v minimal "/p:RatopiaDir=$ratopiaRoot" '/p:InstallAfterBuild=false'
if ($LASTEXITCODE -ne 0) {
    throw "Release 测试失败，退出码：$LASTEXITCODE"
}

& dotnet build $projectPath -c Release -v minimal --no-restore "/p:RatopiaDir=$ratopiaRoot" '/p:InstallAfterBuild=false'
if ($LASTEXITCODE -ne 0) {
    throw "Release 构建失败，退出码：$LASTEXITCODE"
}

if (-not (Test-Path -LiteralPath $pluginDll -PathType Leaf)) {
    throw "找不到 Release 插件：$pluginDll"
}

if (Test-Path -LiteralPath $stageDir) {
    Remove-Item -LiteralPath $stageDir -Recurse -Force
}
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

New-Item -ItemType Directory -Path $pluginStageDir -Force | Out-Null
New-Item -ItemType Directory -Path $distDir -Force | Out-Null
Copy-Item -LiteralPath $pluginDll -Destination (Join-Path $pluginStageDir 'HeaterEnhancement.dll')
Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination (Join-Path $stageDir 'README.md')

$forbiddenNames = @(
    'Assembly-CSharp.dll',
    'Assembly-CSharp-firstpass.dll',
    '0Harmony.dll',
    'BepInEx.dll',
    'UnityEngine.dll',
    'UnityEngine.CoreModule.dll'
)
$forbiddenFiles = Get-ChildItem -LiteralPath $stageDir -Recurse -File | Where-Object {
    $_.Name -in $forbiddenNames -or $_.Name -like 'UnityEngine*.dll' -or $_.Name -like '*.pdb'
}
if ($forbiddenFiles) {
    throw "发布目录包含禁止文件：$($forbiddenFiles.FullName -join ', ')"
}

$actualEntries = Get-ChildItem -LiteralPath $stageDir -Recurse -File | ForEach-Object {
    $_.FullName.Substring($stageDir.Length + 1).Replace('\', '/')
}
$expectedEntries = @(
    'BepInEx/plugins/HeaterEnhancement/HeaterEnhancement.dll',
    'README.md'
)
$actualSignature = (($actualEntries | Sort-Object) -join "`n")
$expectedSignature = (($expectedEntries | Sort-Object) -join "`n")
if ($actualSignature -ne $expectedSignature) {
    throw "发布目录结构不匹配。期望：`n$expectedSignature`n实际：`n$actualSignature"
}

Compress-Archive -Path (Join-Path $stageDir '*') -DestinationPath $zipPath -CompressionLevel Optimal

if (-not (Test-Path -LiteralPath $packageValidator -PathType Leaf)) {
    throw "找不到 Test-RatopiaPackage.ps1：$packageValidator"
}
& $packageValidator -Path $zipPath -ExpectedPluginName 'HeaterEnhancement'
if ($LASTEXITCODE -ne 0) {
    throw "Ratopia 包验证失败，退出码：$LASTEXITCODE"
}

$zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
Write-Host "发布包已生成：$zipPath"
Write-Host "SHA-256：$zipHash"
