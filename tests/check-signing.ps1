[CmdletBinding()]
param([Parameter(Mandatory)][string]$UnsignedAppPath)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
foreach ($relative in @('build-setup.ps1', 'tools/verify-release-signature.ps1')) {
    $errors = $null
    [Management.Automation.Language.Parser]::ParseFile((Join-Path $root $relative), [ref]$null, [ref]$errors) | Out-Null
    if ($errors) { throw "PowerShell syntax errors: $relative" }
}
foreach ($name in @('payload', 'msi', 'engine', 'setup')) {
    [xml]$configuration = Get-Content (Join-Path $root "tools/signpath/$name.xml") -Raw
    $ns = New-Object Xml.XmlNamespaceManager($configuration.NameTable)
    $ns.AddNamespace('s', 'http://signpath.io/artifact-configuration/v1')
    if (-not $configuration.SelectSingleNode('/s:artifact-configuration/s:zip-file', $ns)) { throw "Missing GitHub artifact ZIP root: $name" }
    if (-not $configuration.SelectSingleNode('/s:artifact-configuration/s:parameters/s:parameter[@name="version" and @required="true"]', $ns)) { throw "Missing version restriction: $name" }
    if ($configuration.OuterXml -match 'rclone.exe|System.\*|Microsoft.\*') { throw 'Upstream binaries must not be re-signed.' }
}
$signature = Get-AuthenticodeSignature -LiteralPath $UnsignedAppPath
if ($signature.Status -ne 'NotSigned') { throw 'Supply a known unsigned disposable build for the rejection check.' }
$rejected = $false
try {
    & (Join-Path $root 'tools/verify-release-signature.ps1') -Path $UnsignedAppPath -CertificateThumbprint ('0' * 40)
} catch {
    if ($_.Exception.Message -notmatch 'missing, invalid or from an unexpected certificate') { throw }
    $rejected = $true
}
if (-not $rejected) { throw 'Unsigned application was incorrectly accepted for release.' }
Write-Output 'PASS signing scripts parse; four artifact configurations restrict versions; unsigned release is rejected.'
