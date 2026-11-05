[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$SetupPath,
    [Parameter(Mandatory)][string]$PayloadDirectory,
    [string]$DotnetPath = 'dotnet'
)
$ErrorActionPreference = 'Stop'
# This removes the installed application: never run on a developer/user machine.
if ($env:GITHUB_ACTIONS -ne 'true' -or $env:RUNNER_ENVIRONMENT -ne 'github-hosted') {
    throw 'This acceptance check runs only on disposable GitHub-hosted Windows runners.'
}
$installed = Join-Path $env:ProgramFiles 'EasyMount'
if ((Test-Path $installed) -or (Get-Service -Name 'WinFsp.Launcher' -ErrorAction SilentlyContinue)) {
    throw 'Runner is not a clean EasyMount/WinFsp installation environment.'
}
$setup = (Resolve-Path -LiteralPath $SetupPath).Path
$logs = Join-Path $env:RUNNER_TEMP 'easymount-installer-check'
New-Item -ItemType Directory -Force $logs | Out-Null
function Run-Setup([string]$operation) {
    $log = Join-Path $logs "$operation.log"
    $process = Start-Process -FilePath $setup -ArgumentList @("/$operation", '/quiet', '/norestart', '/log', "`"$log`"") -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -notin @(0, 3010)) { throw "$operation failed: $($process.ExitCode)" }
    if ($process.ExitCode -eq 3010) { throw "$operation requires a reboot; clean-runner mount acceptance is incomplete." }
}
Run-Setup 'install'
try {
    foreach ($file in @('EasyMount.exe', 'EasyMount.dll', 'tools/rclone.exe')) {
        if ((Get-FileHash (Join-Path $installed $file)).Hash -ne (Get-FileHash (Join-Path $PayloadDirectory $file)).Hash) {
            throw "Installed payload mismatch: $file"
        }
    }
    Write-Host 'PASS clean Setup installation matches the built app and engine'
    $output = @(& $DotnetPath run --project (Join-Path $PSScriptRoot 'EasyMount.Checks.csproj') -- (Join-Path $installed 'tools/rclone.exe') --mounts)
    $output | Write-Output
    if ($LASTEXITCODE -ne 0 -or -not ($output -match '^RESULT: .*Local WinFsp mount tests requested: True\.')) { throw 'Installed engine real WinFsp lifecycle checks failed or the wrong runner executed.' }
} finally {
    Run-Setup 'uninstall'
}
if (Test-Path (Join-Path $installed 'EasyMount.exe')) { throw 'App remains after Setup uninstall.' }
if (-not (Get-Service -Name 'WinFsp.Launcher' -ErrorAction SilentlyContinue)) { throw 'Shared WinFsp was removed.' }
Write-Host 'PASS clean Setup uninstall removes EasyMount and preserves shared WinFsp'
