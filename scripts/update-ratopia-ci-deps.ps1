param(
    [Parameter(Mandatory = $false)]
    [string] $RatopiaDir = $env:RATOPIA_DIR,

    [Parameter(Mandatory = $false)]
    [string] $DependencyRepo = 'jangufo/ratopia-ci-deps',

    [Parameter(Mandatory = $false)]
    [string] $ReleaseTag = 'deps-v1',

    [Parameter(Mandatory = $false)]
    [string] $AssetName = 'ratopia-ci-deps.zip',

    [Parameter(Mandatory = $false)]
    [string] $Token = $env:GITHUB_TOKEN
)

$ErrorActionPreference = 'Stop'

function Find-RatopiaDirectory {
    param([string] $ConfiguredDir)

    if ($ConfiguredDir) {
        return $ConfiguredDir
    }

    $steamRoots = @()
    $programFilesX86 = ${env:ProgramFiles(x86)}
    if ($programFilesX86) {
        $steamRoots += (Join-Path $programFilesX86 'Steam')
    }
    if ($env:ProgramFiles) {
        $steamRoots += (Join-Path $env:ProgramFiles 'Steam')
    }

    $libraryRoots = @()
    foreach ($steamRoot in $steamRoots) {
        if (Test-Path $steamRoot) {
            $libraryRoots += $steamRoot
            $libraryConfig = Join-Path $steamRoot 'steamapps\libraryfolders.vdf'
            if (Test-Path $libraryConfig) {
                foreach ($line in Get-Content $libraryConfig) {
                    if ($line -match '"path"\s+"([^"]+)"') {
                        $libraryRoots += $Matches[1].Replace('\\', '\')
                    }
                }
            }
        }
    }

    $candidates = @()
    foreach ($libraryRoot in ($libraryRoots | Select-Object -Unique)) {
        $candidate = Join-Path $libraryRoot 'steamapps\common\Ratopia'
        if (Test-Path $candidate) {
            $candidates += (Resolve-Path $candidate).Path
        }
    }

    if ($candidates.Count -eq 1) {
        return $candidates[0]
    }
    if ($candidates.Count -gt 1) {
        throw "Multiple Ratopia installations were found. Set RATOPIA_DIR to one of:`n$($candidates -join "`n")"
    }

    throw 'Ratopia installation was not found. Set RATOPIA_DIR or install Ratopia through Steam.'
}

function Invoke-GitHubJson {
    param(
        [string] $Method,
        [string] $Uri,
        [string] $Body,
        [string[]] $CurlConfig
    )

    $arguments = @(
        '--config', $CurlConfig,
        '-sS', '--retry', '5', '--retry-all-errors',
        '--connect-timeout', '20', '--max-time', '180',
        '-X', $Method
    )
    if ($null -ne $Body) {
        $arguments += @('-H', 'Content-Type: application/json', '--data', $Body)
    }
    $arguments += $Uri

    $response = & curl.exe @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "curl failed for $Method $Uri with exit code $LASTEXITCODE."
    }

    $jsonText = $response -join "`n"
    $json = $jsonText | ConvertFrom-Json
    if ($json.message) {
        throw "GitHub API error for $Method ${Uri}: $($json.message)"
    }
    return $json
}

$RatopiaDir = Find-RatopiaDirectory -ConfiguredDir $RatopiaDir
$RatopiaDir = (Resolve-Path $RatopiaDir).Path
$assemblyPath = Join-Path $RatopiaDir 'Ratopia_Data\Managed\Assembly-CSharp.dll'
if (-not (Test-Path $assemblyPath)) {
    throw "Assembly-CSharp.dll was not found: $assemblyPath"
}

$hashFile = Join-Path $PSScriptRoot 'Assembly-CSharp.sha256'
if (-not (Test-Path $hashFile)) {
    throw "Known Assembly-CSharp.dll hash file was not found: $hashFile"
}

$knownHash = (Get-Content $hashFile -Raw).Trim().ToUpperInvariant()
$sha256 = [System.Security.Cryptography.SHA256]::Create()
$assemblyStream = [System.IO.File]::OpenRead($assemblyPath)
try {
    $currentHash = ([BitConverter]::ToString($sha256.ComputeHash($assemblyStream))).Replace('-', '').ToUpperInvariant()
}
finally {
    $assemblyStream.Dispose()
    $sha256.Dispose()
}

if ($knownHash -eq $currentHash) {
    Write-Host "Assembly-CSharp.dll is unchanged."
    Write-Host "SHA256: $currentHash"
    Write-Host 'No CI dependency update is required.'
    exit 0
}

Write-Host 'Assembly-CSharp.dll has changed.'
Write-Host "Known SHA256:   $knownHash"
Write-Host "Current SHA256: $currentHash"

if (-not $Token) {
    throw 'GITHUB_TOKEN is not set. It is required to upload the updated dependency bundle.'
}

$packageScript = Join-Path $PSScriptRoot 'package-ci-dependencies.ps1'
$outputZip = Join-Path $env:TEMP 'ratopia-ci-deps-update.zip'
& $packageScript -RatopiaDir $RatopiaDir -OutputZip $outputZip

$curlConfig = Join-Path $env:TEMP ("github-api-" + [guid]::NewGuid().ToString() + ".curlrc")
try {
    "header = `"Authorization: Bearer $Token`"" | Set-Content -Encoding ASCII $curlConfig
    "header = `"Accept: application/vnd.github+json`"" | Add-Content $curlConfig
    "header = `"X-GitHub-Api-Version: 2022-11-28`"" | Add-Content $curlConfig
    "header = `"User-Agent: ratopia-mod-ci-update`"" | Add-Content $curlConfig

    $release = Invoke-GitHubJson `
        -Method 'GET' `
        -Uri "https://api.github.com/repos/$DependencyRepo/releases/tags/$ReleaseTag" `
        -CurlConfig $curlConfig

    foreach ($asset in @($release.assets | Where-Object Name -eq $AssetName)) {
        $null = & curl.exe `
            --config $curlConfig `
            -sS --retry 5 --retry-all-errors `
            -X DELETE `
            "https://api.github.com/repos/$DependencyRepo/releases/assets/$($asset.id)"
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to delete old release asset: $AssetName"
        }
    }

    $uploadResponse = & curl.exe `
        --config $curlConfig `
        -sS --retry 5 --retry-all-errors `
        --connect-timeout 20 --max-time 300 `
        -X POST `
        -H 'Content-Type: application/zip' `
        --data-binary "@$outputZip" `
        "https://uploads.github.com/repos/$DependencyRepo/releases/$($release.id)/assets?name=$AssetName"
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to upload $AssetName with exit code $LASTEXITCODE."
    }

    $asset = ($uploadResponse -join "`n") | ConvertFrom-Json
    if ($asset.message) {
        throw "GitHub upload error: $($asset.message)"
    }

    Set-Content -NoNewline -Encoding ASCII $hashFile $currentHash

    Write-Host "Updated dependency asset: $AssetName"
    Write-Host "Release: $ReleaseTag"
    Write-Host "New SHA256 recorded: $currentHash"
    Write-Host 'Remember to commit scripts/Assembly-CSharp.sha256 after this update.'
}
finally {
    Remove-Item -LiteralPath $curlConfig -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $outputZip -Force -ErrorAction SilentlyContinue
}
