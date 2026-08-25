[CmdletBinding()]
param(
    [string]$RatopiaDir = $env:RATOPIA_DIR
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($RatopiaDir)) { throw '请通过 -RatopiaDir 或 RATOPIA_DIR 指定 Ratopia 游戏目录。' }

$projectRoot = (Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path
$env:RATOPIA_DIR = $RatopiaDir
$solution = Join-Path $projectRoot 'MultipurposeResearch.sln'
& dotnet test $solution -c Release /p:InstallAfterBuild=false
if ($LASTEXITCODE -ne 0) { throw 'Release 测试失败。' }
& dotnet build (Join-Path $projectRoot 'src\MultipurposeResearch\MultipurposeResearch.csproj') -c Release /p:InstallAfterBuild=false --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Release 构建失败。' }

$stage = Join-Path $projectRoot 'artifacts\package'
$dist = Join-Path $projectRoot 'dist'
$zip = Join-Path $dist '多用途研究点-v0.1.11-BepInEx5.zip'
$stageFull = [System.IO.Path]::GetFullPath($stage)
$rootFull = [System.IO.Path]::GetFullPath($projectRoot).TrimEnd('\') + '\'
if (-not $stageFull.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) { throw "拒绝清理项目外目录：$stageFull" }
if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
New-Item -ItemType Directory -Path (Join-Path $stage 'BepInEx\plugins\多用途研究点') -Force | Out-Null
New-Item -ItemType Directory -Path $dist -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $projectRoot 'src\MultipurposeResearch\bin\Release\net472\MultipurposeResearch.dll') -Destination (Join-Path $stage 'BepInEx\plugins\多用途研究点\多用途研究点.dll')
Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination (Join-Path $stage 'README.md')
if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
Compress-Archive -LiteralPath (Join-Path $stage 'BepInEx'), (Join-Path $stage 'README.md') -DestinationPath $zip -CompressionLevel Optimal

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($zip)
try {
    $names = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\', '/') } | Where-Object { -not $_.EndsWith('/') })
    $expected = @('BepInEx/plugins/多用途研究点/多用途研究点.dll', 'README.md')
    $unexpected = @($names | Where-Object { $_ -notin $expected })
    $missing = @($expected | Where-Object { $_ -notin $names })
    if ($unexpected.Count -gt 0 -or $missing.Count -gt 0) { throw "发布包白名单失败；缺少=$($missing -join ',') 多余=$($unexpected -join ',')" }
}
finally { $archive.Dispose() }
Write-Host "发布包：$zip"
Write-Host "SHA-256：$((Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash)"
