[CmdletBinding()]
param(
    [string]$DotnetPath = 'dotnet',
    [string]$WixPath = 'wix',
    [string]$WorkDirectory = (Join-Path (Get-Location) 'work\easymount-installer'),
    [switch]$SkipAppBuild,
    [string]$GoPath = 'go',
    [string]$EnginePath = '',
    [string]$AppPayloadDirectory = (Join-Path (Get-Location) 'work\easymount-build\app')
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$projectRoot = $PSScriptRoot
$workRoot = [IO.Path]::GetFullPath($WorkDirectory)
New-Item -ItemType Directory -Force $workRoot | Out-Null
if (-not $SkipAppBuild) {
    & (Join-Path $projectRoot 'build.ps1') -DotnetPath $DotnetPath -GoPath $GoPath -EnginePath $EnginePath -WorkDirectory (Split-Path ([IO.Path]::GetFullPath($AppPayloadDirectory)) -Parent)
}
$appPayload = [IO.Path]::GetFullPath($AppPayloadDirectory)
if (-not (Test-Path (Join-Path $appPayload 'EasyMount.exe'))) { throw 'Build the internal app payload first.' }

$wixCommand = Get-Command $WixPath -ErrorAction SilentlyContinue
if ($wixCommand) { $wix = $wixCommand.Source }
else {
    $localWix = Join-Path $workRoot 'wix'
    $wix = Join-Path $localWix 'wix.exe'
    if (-not (Test-Path $wix)) {
        & $DotnetPath tool install wix --version 5.0.2 --tool-path $localWix
        if ($LASTEXITCODE -ne 0) { throw 'Cannot prepare WiX 5.0.2.' }
    }
}
$wixVersion = & $wix --version
if ($LASTEXITCODE -ne 0 -or $wixVersion -notmatch '^5\.0\.2[+]') { throw 'This build requires WiX 5.0.2.' }
foreach ($extension in @('WixToolset.BootstrapperApplications.wixext/5.0.2', 'WixToolset.Util.wixext/5.0.2')) {
    & $wix extension add -g $extension
    if ($LASTEXITCODE -ne 0) { throw "Cannot prepare $extension." }
}

$winFspUrl = 'https://github.com/winfsp/winfsp/releases/download/v2.1/winfsp-2.1.25156.msi'
$winFspHash = '073a70e00f77423e34bed98b86e600def93393ba5822204fac57a29324db9f7a'
$sourceUrl = 'https://github.com/winfsp/winfsp/archive/refs/tags/v2.1.zip'
$sourceHash = '7b51f3c64fc5596eab315c8812f8b13e96d6830deef66e30df2019dafd3e0dd4'
$winFspMsi = Join-Path $workRoot 'winfsp-2.1.25156.msi'
$sourceZip = Join-Path $workRoot 'winfsp-v2.1-source.zip'
foreach ($download in @(
    @{Url=$winFspUrl; Path=$winFspMsi; Hash=$winFspHash},
    @{Url=$sourceUrl; Path=$sourceZip; Hash=$sourceHash}
)) {
    if (-not (Test-Path $download.Path)) { Invoke-WebRequest $download.Url -OutFile $download.Path -UseBasicParsing }
    if ((Get-FileHash $download.Path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $download.Hash) {
        throw "Checksum mismatch: $($download.Path)"
    }
}
$signature = Get-AuthenticodeSignature $winFspMsi
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'O=NAVIMATICS LLC,') {
    throw 'The bundled WinFsp MSI does not have the expected valid official signature.'
}

$payload = Join-Path $workRoot ('payload-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $payload | Out-Null
Copy-Item (Join-Path $appPayload '*') $payload -Recurse -Force
$thirdParty = Join-Path $payload 'third-party'
New-Item -ItemType Directory -Force $thirdParty | Out-Null
Copy-Item $sourceZip $thirdParty -Force
$licensePath = Join-Path $thirdParty 'WinFsp-License.txt'
Invoke-WebRequest 'https://raw.githubusercontent.com/winfsp/winfsp/ddca7bd5481857a65ba552f643b8776fd070836f/License.txt' -OutFile $licensePath -UseBasicParsing
Copy-Item $licensePath (Join-Path $projectRoot 'docs\WinFsp-License.txt') -Force
Copy-Item (Join-Path $projectRoot 'docs\rclone-LICENSE.txt') $thirdParty -Force
Copy-Item (Join-Path $projectRoot 'docs\THIRD-PARTY-NOTICES.md') $thirdParty -Force
Copy-Item (Join-Path $projectRoot 'README.md') $payload -Force
Copy-Item (Join-Path $projectRoot 'LICENSE') $payload -Force

$licenseText = @'
EasyMount Setup

This installer includes EasyMount, rclone and the original signed WinFsp 2025 (2.1.25156) installer. WinFsp is installed or upgraded only when needed. Administrator access is required. Exit EasyMount using its Quit menu before installing an update. Windows may request a restart before the drive is available.

WinFsp - Windows File System Proxy, Copyright (C) Bill Zissimopoulos
https://github.com/winfsp/winfsp

WinFsp is distributed unmodified with its license and matching v2.1 source archive in the application's third-party folder. It is a shared system component and remains installed when EasyMount is removed. rclone is MIT-licensed; its license is included in the same folder.

The full WinFsp license follows:

'@
$licenseText += Get-Content $licensePath -Raw
$escaped = $licenseText.Replace('\', '\\').Replace('{', '\{').Replace('}', '\}').Replace("`r", '').Replace("`n", '\par ')
$licenseRtf = Join-Path $workRoot 'License.rtf'
('{\rtf1\ansi\deff0{\fonttbl{\f0 Segoe UI;}}\f0\fs18 ' + $escaped + '}') | Set-Content $licenseRtf -Encoding ascii

$appMsi = Join-Path $workRoot 'EasyMount-0.3.4-x64.msi'
$setup = Join-Path $projectRoot 'EasyMount-0.3.4-Setup.exe'
& $wix build (Join-Path $projectRoot 'installer\Package.wxs') -arch x64 -d "ProjectDir=$projectRoot" -d "PayloadDir=$payload" -pdbtype none -o $appMsi
if ($LASTEXITCODE -ne 0) { throw 'EasyMount MSI build failed.' }
& $wix build (Join-Path $projectRoot 'installer\Bundle.wxs') -arch x64 -ext WixToolset.BootstrapperApplications.wixext -ext WixToolset.Util.wixext -d "ProjectDir=$projectRoot" -d "WinFspMsi=$winFspMsi" -d "AppMsi=$appMsi" -d "LicenseRtf=$licenseRtf" -pdbtype none -o $setup
if ($LASTEXITCODE -ne 0) { throw 'EasyMount bootstrapper build failed.' }

$hash = (Get-FileHash $setup -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $([IO.Path]::GetFileName($setup))" | Set-Content ($setup + '.sha256') -Encoding ascii
@{
    version='0.3.4'; architecture='x64'; setup=[IO.Path]::GetFileName($setup); setupSha256=$hash
    winFspVersion='2.1.25156'; winFspSource=$winFspUrl; winFspMsiSha256=$winFspHash
    winFspSignature=$signature.Status.ToString(); signer=$signature.SignerCertificate.Subject
    winFspSourceArchive=$sourceUrl; winFspSourceSha256=$sourceHash
    wix='5.0.2'; installation='per-machine'; downloadsAtInstallTime=$false
    sharedDriverOnUninstall='preserved'; installedWinFspDetection='MSI UpgradeCode; skip same/newer versions'
} | ConvertTo-Json | Set-Content (Join-Path $projectRoot 'docs\installer-build.json') -Encoding utf8
Write-Host "Ready: $setup"
