[CmdletBinding()]
param(
    [string]$GoPath = 'go',
    [string]$WorkDirectory = (Join-Path (Get-Location) 'work\easymount-engine'),
    [Parameter(Mandatory)][string]$OutputPath
)
$ErrorActionPreference = 'Stop'
$engineVersion = (Select-Xml -LiteralPath (Join-Path $PSScriptRoot '../Directory.Build.props') -XPath '/Project/PropertyGroup/EngineVersion').Node.InnerText
if ($engineVersion -notmatch '^v\d+\.\d+\.\d+-easymount\.\d+$') { throw 'Invalid engine version.' }
$workRoot = [IO.Path]::GetFullPath($WorkDirectory)
New-Item -ItemType Directory -Force $workRoot | Out-Null
$zip = Join-Path $workRoot 'rclone-v1.75.2.zip'
$expected = '09975735e8f26dec6ad43384f0c8a0cc894bdde383ccee042d486c41ab4031c2'
if (-not (Test-Path $zip)) { Invoke-WebRequest 'https://github.com/rclone/rclone/archive/refs/tags/v1.75.2.zip' -OutFile $zip }
if ((Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expected) { throw 'rclone source checksum mismatch.' }
$extract = Join-Path $workRoot ('source-' + [Guid]::NewGuid().ToString('N'))
Expand-Archive -LiteralPath $zip -DestinationPath $extract
$source = Join-Path $extract 'rclone-1.75.2'
& (Join-Path $PSScriptRoot 'apply-overlay.ps1') -SourceRoot $source
$output = [IO.Path]::GetFullPath($OutputPath)
$goExecutable = (Get-Command $GoPath -ErrorAction Stop).Source
New-Item -ItemType Directory -Force (Split-Path $output -Parent) | Out-Null
$env:CGO_ENABLED = '0'
Push-Location $source
try {
    & $goExecutable test ./backend/onedrive -run '^TestEasyMountDefaultDrive$' -count=1
    if ($LASTEXITCODE -ne 0) { throw 'OneDrive default selection regression check failed.' }
    & $goExecutable build -tags cmount -trimpath -ldflags "-s -w -X github.com/rclone/rclone/fs.Version=$engineVersion" -o $output .
    if ($LASTEXITCODE -ne 0) { throw 'EasyMount rclone engine build failed.' }
} finally { Pop-Location }
Write-Host "Engine ready: $output"
