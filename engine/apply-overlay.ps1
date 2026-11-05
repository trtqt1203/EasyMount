param([Parameter(Mandatory)][string]$SourceRoot)
$ErrorActionPreference = 'Stop'
$union = Join-Path $SourceRoot 'backend\union\union.go'
$options = Join-Path $SourceRoot 'backend\union\common\options.go'
$s = Get-Content -LiteralPath $union -Raw
if ($s.Contains('EasyMountPriority')) { throw 'Apply overlay only to clean rclone v1.75.2 source.' }
$anchor = 'func (f *Fs) put(ctx context.Context, in io.Reader, src fs.ObjectInfo, stream bool, options ...fs.OpenOption) (fs.Object, error) {'
if (-not $s.Contains($anchor)) { throw 'rclone put signature changed.' }
$s = $s.Replace($anchor, $anchor + "`n`tif f.opt.EasyMountPriority { return f.priorityPut(ctx, in, src, options...) }")
$s = $s.Replace('// Backward compatible to old config', "if err := applyEasyMountPlan(opt); err != nil { return nil, err }`n`t// Backward compatible to old config")
$s = $s.Replace('type Fs struct {', "type Fs struct {`n`tpriorityMu sync.Mutex // EasyMount ordered placement")
$fileRootFix = @'
// EasyMount keeps every account when a source-file lookup returns ErrorIsFile.
// Otherwise rclone may reuse the source's parent Fs for a new destination and
// accidentally restrict the destination to the source account.
if opt.EasyMountPriority && fserr == fs.ErrorIsFile {
    f.upstreams = make([]*upstream.Fs, len(opt.Upstreams))
    for i, remote := range opt.Upstreams {
        u, e := upstream.New(ctx, remote, f.root, opt)
        if e != nil { return nil, e }
        f.upstreams[i] = u
    }
}
err = upstream.Prepare(f.upstreams)
'@
$s = $s.Replace('err = upstream.Prepare(f.upstreams)', $fileRootFix)
$s = $s.Replace('f.features = features', "if opt.EasyMountPriority { features.Copy = nil; features.Move = nil; features.ListR = nil } // Apply placement on copies; updates retain their source; listings retain priority.`n`tf.features = features")
$s = $s.Replace('Options: []fs.Option{{', @'
Options: []fs.Option{{Name: "easymount_plan", Default: "", Advanced: true, Help: "EasyMount: JSON pool plan path; avoids environment/command-line length limits."},
{Name: "easymount_priority", Default: false, Advanced: true, Help: "EasyMount: place new files by configured priority and file-size quota."},
{Name: "easymount_limits", Default: "", Advanced: true, Help: "EasyMount: per-source folder limits in bytes, comma separated; zero uses cloud quota."}, {
'@)
[IO.File]::WriteAllText($union, $s)
$s = Get-Content -LiteralPath $options -Raw
$s = $s.Replace('type Options struct {', "type Options struct {`n`tEasyMountPlan string ``config:`"easymount_plan`"```n`tEasyMountPriority bool ``config:`"easymount_priority`"```n`tEasyMountLimits string ``config:`"easymount_limits`"``")
[IO.File]::WriteAllText($options, $s)
Copy-Item (Join-Path $PSScriptRoot 'priority.go') (Join-Path $SourceRoot 'backend\union\priority_easymount.go') -Force

# Network prefixes are UNC paths, not 32-byte disk labels. Byte truncation also
# splits Vietnamese UTF-8 characters. WinFsp accepts the complete network path.
$utils = Join-Path $SourceRoot 'cmd\mountlib\utils.go'
$s = Get-Content -LiteralPath $utils -Raw
$anchor = 'if runtime.GOOS == "windows" && len(vol) > 32 {'
if (-not $s.Contains($anchor)) { throw 'rclone volume name handling changed.' }
$s = $s.Replace($anchor, 'if runtime.GOOS == "windows" && !o.NetworkMode && !strings.HasPrefix(vol, `\\`) && len(vol) > 32 {')
[IO.File]::WriteAllText($utils, $s)

# Prefer the canonical /me/drive over Graph's internal personal-drive containers.
$oneDrive = Join-Path $SourceRoot 'backend\onedrive\onedrive.go'
$s = Get-Content -LiteralPath $oneDrive -Raw
$start = $s.IndexOf('if meDriveErr == nil {')
if ($start -lt 0) { throw 'OneDrive default-drive discovery changed.' }
$end = $s.IndexOf('} else if drivesErr != nil {', $start)
if ($start -lt 0 -or $end -lt $start) { throw 'OneDrive default-drive discovery changed.' }
$block = $s.Substring($start, $end - $start)
if (-not $block.Contains('for _, drive := range drives.Drives {') -or -not $block.Contains('drives.Drives = append(drives.Drives, meDrive)')) { throw 'OneDrive choice ordering changed.' }
$fixed = $block.Replace('for _, drive := range drives.Drives {', 'for i, drive := range drives.Drives {')
$fixed = $fixed.Replace('found = true', "copy(drives.Drives[1:i+1], drives.Drives[:i])`n`t`t`t`t`t`tdrives.Drives[0] = meDrive`n`t`t`t`t`t`tfound = true")
$fixed = $fixed.Replace('drives.Drives = append(drives.Drives, meDrive)', 'drives.Drives = append([]api.DriveResource{meDrive}, drives.Drives...)')
$s = $s.Remove($start, $end - $start).Insert($start, $fixed)
[IO.File]::WriteAllText($oneDrive, $s)
Copy-Item (Join-Path $PSScriptRoot 'onedrive_default_test.go') (Join-Path $SourceRoot 'backend\onedrive\easymount_default_test.go') -Force
