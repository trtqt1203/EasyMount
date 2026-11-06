[CmdletBinding()]
param(
    [Parameter(Mandatory)][string[]]$Path,
    [Parameter(Mandatory)][string]$CertificateThumbprint
)
$ErrorActionPreference = 'Stop'
$expected = $CertificateThumbprint.Replace(' ', '').ToUpperInvariant()
if ($expected -notmatch '^[0-9A-F]{40}$') { throw 'Configure the approved signing certificate thumbprint first.' }
foreach ($file in $Path) {
    $signature = Get-AuthenticodeSignature -LiteralPath (Resolve-Path -LiteralPath $file).Path
    if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Thumbprint -ne $expected) {
        throw "Release signature is missing, invalid or from an unexpected certificate: $file"
    }
    if (-not $signature.TimeStamperCertificate) { throw "Release signature has no timestamp: $file" }
    Write-Output "Verified release signature and timestamp: $([IO.Path]::GetFileName($file))"
}
