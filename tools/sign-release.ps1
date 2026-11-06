param(
    [Parameter(Mandatory=$true)][string]$SetupPath,
    [Parameter(Mandatory=$true)][string]$CertificateThumbprint,
    [string]$SignToolPath = 'signtool.exe'
)
$ErrorActionPreference = 'Stop'
$certificate = Get-Item -LiteralPath "Cert:\CurrentUser\My\$CertificateThumbprint"
if (!$certificate.HasPrivateKey -or $certificate.NotAfter -lt (Get-Date)) { throw 'A valid code-signing certificate with its private key is required.' }
if ($certificate.Subject -eq $certificate.Issuer) { throw 'Self-signed certificates are not accepted for public releases.' }
$setup = (Resolve-Path -LiteralPath $SetupPath).Path
& $SignToolPath sign /sha1 $CertificateThumbprint /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 $setup
if ($LASTEXITCODE -ne 0) { throw 'Signing failed.' }
$signature = Get-AuthenticodeSignature -LiteralPath $setup
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Thumbprint -ne $CertificateThumbprint) { throw 'Signature verification failed.' }
$hash = (Get-FileHash -LiteralPath $setup -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $([IO.Path]::GetFileName($setup))" | Set-Content -LiteralPath ($setup + '.sha256') -Encoding ascii
Write-Output 'Release signature verified and SHA256 refreshed.'
