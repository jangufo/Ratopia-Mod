[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$RatopiaDir = $env:RATOPIA_DIR,

    [Parameter(Mandatory = $false)]
    [string]$PluginPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($RatopiaDir)) {
    throw '请通过 -RatopiaDir 或 RATOPIA_DIR 指定 Ratopia 游戏目录。'
}

$projectRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$ratopiaRoot = [IO.Path]::GetFullPath($RatopiaDir)
if ([string]::IsNullOrWhiteSpace($PluginPath)) {
    $PluginPath = Join-Path $projectRoot 'src\MultipurposeResearch\bin\Release\net472\MultipurposeResearch.dll'
}

$sourcePlugin = [IO.Path]::GetFullPath($PluginPath)
$targetPlugin = Join-Path $ratopiaRoot 'BepInEx\plugins\多用途研究点\多用途研究点.dll'
$targetDirectory = Split-Path -Parent $targetPlugin
$temporaryPlugin = "$targetPlugin.installing"
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupDirectory = Join-Path $projectRoot "backups\install-$timestamp"
$pluginBackup = Join-Path $backupDirectory '多用途研究点.dll'

$ratopiaProcesses = @(Get-Process -Name 'Ratopia' -ErrorAction SilentlyContinue)
if ($ratopiaProcesses.Count -gt 0) {
    throw "Ratopia 仍在运行（PID：$($ratopiaProcesses.Id -join ', ')）。请退出游戏后再安装；脚本不会关闭游戏。"
}

if (-not (Test-Path -LiteralPath $sourcePlugin -PathType Leaf)) {
    throw "找不到待安装插件：$sourcePlugin"
}

New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
if (Test-Path -LiteralPath $targetPlugin -PathType Leaf) {
    New-Item -ItemType Directory -Path $backupDirectory -Force | Out-Null
    Copy-Item -LiteralPath $targetPlugin -Destination $pluginBackup
}

if (Test-Path -LiteralPath $temporaryPlugin -PathType Leaf) {
    Remove-Item -LiteralPath $temporaryPlugin -Force
}

Copy-Item -LiteralPath $sourcePlugin -Destination $temporaryPlugin
Move-Item -LiteralPath $temporaryPlugin -Destination $targetPlugin -Force

$sourceHash = (Get-FileHash -LiteralPath $sourcePlugin -Algorithm SHA256).Hash
$installedHash = (Get-FileHash -LiteralPath $targetPlugin -Algorithm SHA256).Hash
if (-not $sourceHash.Equals($installedHash, [StringComparison]::OrdinalIgnoreCase)) {
    throw "安装后 DLL 哈希校验失败。源：$sourceHash；目标：$installedHash。"
}

[pscustomobject]@{
    Installed = $targetPlugin
    Backup = if (Test-Path -LiteralPath $pluginBackup -PathType Leaf) { $pluginBackup } else { $null }
    SHA256 = $installedHash
}
