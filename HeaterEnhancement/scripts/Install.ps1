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

$expectedGuid = 'cn.ratopia.heaterenhancement'
$expectedVersion = '0.1.3'
$projectRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$ratopiaRoot = [IO.Path]::GetFullPath($RatopiaDir)
if ([string]::IsNullOrWhiteSpace($PluginPath)) {
    $PluginPath = Join-Path $projectRoot 'src\HeaterEnhancement\bin\Release\net472\HeaterEnhancement.dll'
}

$sourcePlugin = [IO.Path]::GetFullPath($PluginPath)
$targetPlugin = Join-Path $ratopiaRoot 'BepInEx\plugins\HeaterEnhancement\HeaterEnhancement.dll'
$targetDirectory = Split-Path -Parent $targetPlugin
$temporaryPlugin = "$targetPlugin.installing"
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
$backupDirectory = Join-Path $projectRoot "backups\pre-install-$timestamp"
$pluginBackup = Join-Path $backupDirectory 'HeaterEnhancement.dll'
$cecilPath = Join-Path $ratopiaRoot 'BepInEx\core\Mono.Cecil.dll'

function Assert-ChildPath([string]$Path, [string]$Parent) {
    $fullPath = [IO.Path]::GetFullPath($Path)
    $fullParent = [IO.Path]::GetFullPath($Parent).TrimEnd('\') + '\'
    if (-not $fullPath.StartsWith($fullParent, [StringComparison]::OrdinalIgnoreCase)) {
        throw "路径不在预期目录内：$fullPath"
    }
}

function Get-PluginMetadata([string]$Path) {
    $assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($Path)
    try {
        foreach ($type in $assembly.MainModule.Types) {
            foreach ($attribute in $type.CustomAttributes) {
                if ($attribute.AttributeType.FullName -ne 'BepInEx.BepInPlugin') {
                    continue
                }

                return [pscustomobject]@{
                    Guid = [string]$attribute.ConstructorArguments[0].Value
                    Name = [string]$attribute.ConstructorArguments[1].Value
                    Version = [string]$attribute.ConstructorArguments[2].Value
                }
            }
        }
    }
    finally {
        $assembly.Dispose()
    }

    return $null
}

$ratopiaProcesses = @(Get-Process -Name 'Ratopia' -ErrorAction SilentlyContinue)
if ($ratopiaProcesses.Count -gt 0) {
    throw "Ratopia 仍在运行（PID：$($ratopiaProcesses.Id -join ', ')）。请正常退出游戏后再安装；脚本不会关闭游戏。"
}

if (-not (Test-Path -LiteralPath $sourcePlugin -PathType Leaf)) {
    throw "找不到待安装插件：$sourcePlugin"
}
if (-not (Test-Path -LiteralPath $cecilPath -PathType Leaf)) {
    throw "找不到 Mono.Cecil：$cecilPath"
}

Assert-ChildPath -Path $targetPlugin -Parent $ratopiaRoot
Assert-ChildPath -Path $temporaryPlugin -Parent $ratopiaRoot
Assert-ChildPath -Path $backupDirectory -Parent $projectRoot
Add-Type -Path $cecilPath

$sourceMetadata = Get-PluginMetadata $sourcePlugin
if ($null -eq $sourceMetadata -or $sourceMetadata.Guid -ne $expectedGuid -or $sourceMetadata.Version -ne $expectedVersion) {
    throw "待安装 DLL 的 BepInPlugin 元数据不匹配。期望 GUID=$expectedGuid，版本=$expectedVersion。"
}

$duplicateGuidFiles = [Collections.Generic.List[string]]::new()
foreach ($directory in @(
             (Join-Path $ratopiaRoot 'BepInEx\plugins'),
             (Join-Path $ratopiaRoot 'BepInEx\patchers'))) {
    if (-not (Test-Path -LiteralPath $directory -PathType Container)) {
        continue
    }

    foreach ($candidate in Get-ChildItem -LiteralPath $directory -Filter '*.dll' -File -Recurse) {
        try {
            $metadata = Get-PluginMetadata $candidate.FullName
            if ($metadata -and $metadata.Guid -eq $expectedGuid -and
                -not $candidate.FullName.Equals($targetPlugin, [StringComparison]::OrdinalIgnoreCase)) {
                $duplicateGuidFiles.Add($candidate.FullName)
            }
        }
        catch {
            Write-Verbose "无法读取插件元数据，已跳过：$($candidate.FullName)"
        }
    }
}
if ($duplicateGuidFiles.Count -gt 0) {
    throw "发现重复 GUID $expectedGuid：$($duplicateGuidFiles -join ', ')"
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

$installedMetadata = Get-PluginMetadata $targetPlugin
if ($null -eq $installedMetadata -or $installedMetadata.Guid -ne $expectedGuid -or $installedMetadata.Version -ne $expectedVersion) {
    throw "安装后的 DLL 元数据不匹配。期望 GUID=$expectedGuid，版本=$expectedVersion。"
}

$sourceHash = (Get-FileHash -LiteralPath $sourcePlugin -Algorithm SHA256).Hash
$installedHash = (Get-FileHash -LiteralPath $targetPlugin -Algorithm SHA256).Hash
if (-not $sourceHash.Equals($installedHash, [StringComparison]::OrdinalIgnoreCase)) {
    throw "安装后 DLL 哈希校验失败。源：$sourceHash；目标：$installedHash。"
}

[pscustomobject]@{
    Installed = $targetPlugin
    Backup = if (Test-Path -LiteralPath $pluginBackup -PathType Leaf) { $pluginBackup } else { $null }
    GUID = $installedMetadata.Guid
    Version = $installedMetadata.Version
    SHA256 = $installedHash
}
