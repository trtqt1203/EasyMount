[CmdletBinding()]
param(
    [string]$SetupPath = '',
    [string]$WixPath = 'wix',
    [string]$AppPayloadDirectory = (Join-Path (Get-Location) 'work\easymount-build\app'),
    [string]$WorkDirectory = (Join-Path (Get-Location) 'work\installer-check')
)
$ErrorActionPreference = 'Stop'
$appVersion = (Select-Xml -LiteralPath (Join-Path $PSScriptRoot '../Directory.Build.props') -XPath '/Project/PropertyGroup/Version').Node.InnerText
if (-not $SetupPath) { $SetupPath = Join-Path $PSScriptRoot "../EasyMount-$appVersion-Setup.exe" }
Set-StrictMode -Version Latest
$setup = (Resolve-Path $SetupPath).Path
$checkRoot = Join-Path ([IO.Path]::GetFullPath($WorkDirectory)) ([Guid]::NewGuid().ToString('N'))
$ba = Join-Path $checkRoot 'ba'
$content = Join-Path $checkRoot 'content'
$checks = 0
function Check([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL $message" }
    $script:checks++
    Write-Host "PASS $message"
}
& $WixPath burn extract $setup -o $content -oba $ba
if ($LASTEXITCODE -ne 0) { throw 'Cannot extract installer.' }
[xml]$manifest = Get-Content (Join-Path $ba 'manifest.xml') -Raw
$ns = New-Object Xml.XmlNamespaceManager($manifest.NameTable)
$ns.AddNamespace('b', 'http://wixtoolset.org/schemas/v4/2008/Burn')
$chain = @($manifest.SelectNodes('/b:BurnManifest/b:Chain/b:MsiPackage', $ns))
Check ($chain.Count -eq 2 -and $chain[0].Id -eq 'WinFsp' -and $chain[1].Id -eq 'EasyMountApp') 'driver precedes app in the actual compiled bundle'
Check ($chain[0].Permanent -eq 'yes') 'shared WinFsp is preserved on app removal'
Check ($chain[0].InstallCondition -eq 'NOT WinFspVersion OR WinFspVersion < v2.1.25156') 'same/newer WinFsp skips installation; older version upgrades'
$search = $manifest.SelectSingleNode('/b:BurnManifest/b:MsiProductSearch', $ns)
Check ($search.Variable -eq 'WinFspVersion' -and $search.UpgradeCode -eq '{5466A3D8-3AA1-4240-B6A0-3A051940A3EC}' -and $search.Type -eq 'version') 'compiled detection uses the official WinFsp MSI UpgradeCode'
$payloads = @($manifest.SelectNodes('/b:BurnManifest/b:Payload', $ns))
Check ($payloads.Count -eq 2 -and @($payloads | Where-Object { $_.Packaging -ne 'embedded' -or $_.HasAttribute('DownloadUrl') }).Count -eq 0) 'all installation payloads are embedded for offline use'
$driver = @(Get-ChildItem $content -Recurse -File -Filter 'winfsp-2.1.25156.msi')
Check ($driver.Count -eq 1) 'original WinFsp MSI is embedded'
Check ((Get-FileHash $driver[0].FullName -Algorithm SHA256).Hash -eq '073A70E00F77423E34BED98B86E600DEF93393BA5822204FAC57A29324DB9F7A') 'embedded WinFsp bytes match the official published SHA256'
$sig = Get-AuthenticodeSignature $driver[0].FullName
Check ($sig.Status -eq 'Valid' -and $sig.SignerCertificate.Subject -match 'O=NAVIMATICS LLC,') 'embedded WinFsp retains a valid official signature'
$app = @(Get-ChildItem $content -Recurse -File -Filter "EasyMount-$appVersion-x64.msi")
Check ($app.Count -eq 1) 'EasyMount MSI is embedded'
$extracted = Join-Path $checkRoot 'msi'
$wxs = Join-Path $checkRoot 'app.wxs'
& $WixPath msi decompile $app[0].FullName -x $extracted -o $wxs
if ($LASTEXITCODE -ne 0) { throw 'Cannot decompile app MSI.' }
[xml]$application = Get-Content $wxs -Raw
$appNs = New-Object Xml.XmlNamespaceManager($application.NameTable)
$appNs.AddNamespace('w', 'http://wixtoolset.org/schemas/v4/wxs')
$files = @($application.SelectNodes('//w:File', $appNs))
$projectRoot = Split-Path $PSScriptRoot -Parent
foreach ($pair in @(
    @{Name='EasyMount.exe'; Path='app\EasyMount.exe'},
    @{Name='EasyMount.dll'; Path='app\EasyMount.dll'},
    @{Name='rclone.exe'; Path='app\tools\rclone.exe'},
    @{Name='coreclr.dll'; Path='app\coreclr.dll'},
    @{Name='oauth-clients.json'; Path='app\oauth-clients.json'},
    @{Name='README.md'; Path='README.md'},
    @{Name='WinFsp-License.txt'; Path='docs\WinFsp-License.txt'},
    @{Name='THIRD-PARTY-NOTICES.md'; Path='docs\THIRD-PARTY-NOTICES.md'},
    @{Name='dotnet-LICENSE.txt'; Path='docs\dotnet-LICENSE.txt'},
    @{Name='dotnet-THIRD-PARTY-NOTICES.txt'; Path='docs\dotnet-THIRD-PARTY-NOTICES.txt'}
)) {
    $file = @($files | Where-Object { $_.Name -eq $pair.Name })
    Check ($file.Count -eq 1) "MSI contains $($pair.Name)"
    $embeddedFile = Join-Path $extracted ('File\' + $file[0].Id)
    $expectedPath = if ($pair.Path.StartsWith('app\')) { Join-Path $AppPayloadDirectory $pair.Path.Substring(4) } else { Join-Path $projectRoot $pair.Path }
    Check ((Get-FileHash $embeddedFile -Algorithm SHA256).Hash -eq (Get-FileHash $expectedPath -Algorithm SHA256).Hash) "MSI payload matches current $($pair.Name)"
}
$source = @($files | Where-Object { $_.Name -eq 'winfsp-v2.1-source.zip' })
Check ($source.Count -eq 1) 'WinFsp corresponding source archive is included'
Check ((Get-FileHash (Join-Path $extracted ('File\' + $source[0].Id)) -Algorithm SHA256).Hash -eq '7B51F3C64FC5596EAB315C8812F8B13E96D6830DEEF66E30DF2019DAFD3E0DD4') 'WinFsp source archive matches the pinned v2.1 source'
Check ($application.SelectSingleNode('//w:Directory[@Id="INSTALLFOLDER"]', $appNs).ParentNode.Id -eq 'ProgramFiles64Folder') 'app targets 64-bit Program Files'
Check ($application.SelectSingleNode('//w:Shortcut[@Id="StartMenuShortcut"]', $appNs).Target -eq '[INSTALLFOLDER]EasyMount.exe') 'Start Menu shortcut points to the installed app'
Check (@($files | Where-Object { $_.Name -in @('rclone.conf','drives.json') }).Count -eq 0) 'no personal accounts or drive profiles are bundled'
$uninstall = $application.SelectSingleNode('//w:CustomAction[@Id="PrepareSafeUninstall"]', $appNs)
Check ($null -ne $uninstall -and $uninstall.Directory -eq 'INSTALLFOLDER' -and $uninstall.ExeCommand -match '--prepare-uninstall') 'compiled MSI invokes installed safe-unmount helper'
Check ($uninstall.GetAttribute('Return') -ne 'ignore' -and $uninstall.GetAttribute('Return') -ne 'asyncNoWait') 'uninstall requires a successful synchronous helper result'
$uninstallSequence = $application.SelectSingleNode('//w:InstallExecuteSequence/w:Custom[@Action="PrepareSafeUninstall"]', $appNs)
Check ($null -ne $uninstallSequence -and $uninstallSequence.GetAttribute('Condition') -match 'Installed' -and $uninstallSequence.GetAttribute('Condition') -match 'REMOVE') 'safe-unmount action is conditioned on actual app removal'
Write-Host "RESULT: $checks installer checks passed. No app or driver was installed."
