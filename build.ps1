[CmdletBinding()]
param(
    [string]$DotnetPath = 'dotnet',
    [string]$GoPath = 'go',
    [string]$EnginePath = '',
    [string]$OAuthClientsPath = (Join-Path $PSScriptRoot 'oauth-clients.json'),
    [string]$WorkDirectory = (Join-Path (Get-Location) 'work\easymount-build')
)
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$appVersion = (Select-Xml -LiteralPath (Join-Path $projectRoot "Directory.Build.props") -XPath "/Project/PropertyGroup/Version").Node.InnerText
if ($appVersion -notmatch "^\d+\.\d+\.\d+$") { throw "A numeric three-part installer version is required." }
$workRoot = [IO.Path]::GetFullPath($WorkDirectory)
$payload = Join-Path $workRoot 'app'
$oauthClients = (Resolve-Path -LiteralPath $OAuthClientsPath -ErrorAction Stop).Path
$identity = Get-Content -LiteralPath $oauthClients -Raw | ConvertFrom-Json
if (-not $identity.drive -or [string]::IsNullOrWhiteSpace($identity.drive.client_id) -or [string]::IsNullOrWhiteSpace($identity.drive.client_secret)) {
    throw 'Supply a configured Google Desktop OAuth application identity; the example is not a release configuration.'
}
$toolsDirectory = Join-Path $payload 'tools'
New-Item -ItemType Directory -Force $toolsDirectory | Out-Null
if (-not $EnginePath) {
    $EnginePath = Join-Path $workRoot 'engine\rclone.exe'
    & (Join-Path $projectRoot 'engine\build-engine.ps1') -GoPath $GoPath -WorkDirectory (Join-Path $workRoot 'engine-source') -OutputPath $EnginePath
}
$EnginePath = (Resolve-Path $EnginePath).Path
$version = & $EnginePath version
if ($LASTEXITCODE -ne 0 -or ($version -join " ") -notmatch 'v1.75.2-easymount.2') { throw 'The EasyMount priority engine is required.' }
& $DotnetPath publish (Join-Path $projectRoot 'src\EasyMount\EasyMount.csproj') -c Release -r win-x64 --self-contained true -o $payload -p:PublishSingleFile=false
if ($LASTEXITCODE -ne 0) { throw 'EasyMount publish failed.' }
Copy-Item -LiteralPath $EnginePath -Destination (Join-Path $toolsDirectory 'rclone.exe') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs\rclone-LICENSE.txt') -Destination $toolsDirectory -Force
Copy-Item -LiteralPath $oauthClients -Destination (Join-Path $payload 'oauth-clients.json') -Force
@{
    appVersion=$appVersion; engineVersion='v1.75.2-easymount.2'; engineSha256=(Get-FileHash $EnginePath -Algorithm SHA256).Hash.ToLowerInvariant()
    upstream='https://github.com/rclone/rclone/tree/v1.75.2'; overlay='engine/priority.go and engine/apply-overlay.ps1'; distribution='Setup only'
} | ConvertTo-Json | Set-Content (Join-Path $projectRoot 'docs\BUILD.json') -Encoding utf8
Write-Host "Internal installer payload: $payload"
Write-Host 'Run build-setup.ps1 to produce the release installer.'
