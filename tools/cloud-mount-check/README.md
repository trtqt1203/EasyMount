# Opt-in real cloud mount acceptance

This check writes generated disposable data to one explicitly selected existing
Google Drive, OneDrive or Dropbox account. It does not open browser login or
modify the installed app's profiles. It is not an unattended CI test.

Build this project in Release, then run its DLL with the .NET host and five
arguments: engine path, existing private rclone.conf, configured remote name
(without colon), a NEW private work directory outside this source checkout,
and the Python executable. Run from the normal non-elevated desktop session.
Use the default project build output; the checker locates the repository's
existing tests/check-mounted-cloud.py relative to that output.

The work directory contains copied OAuth credentials, cache, logs and recovery
leases. Never commit, upload or share it. The source configuration is not
rewritten by this checker; the isolated copy can receive token refreshes.

The checker chooses a free drive letter only when neither a logical drive nor
a Windows network mapping is present. It creates a UUID cloud directory,
mounts only that directory with DriveService, and reuses the existing 1 MiB
Unicode fixture/hash/cloud-deletion check. Cloud directory removal is always
nonrecursive. It then reuses the service's authenticated queue/error checks
and safe disconnect, verifying the drive and network mapping are absent.

On failure, private fixtures/cache are retained. A worker whose upload state
cannot be verified may remain running with its recovery lease. Do not kill it
or delete cache: recover that private data directory with DriveService and
verify uploads first. No personal mount worker is selected or stopped.

Only summary.json is intended for publication after inspecting it for private
data. A pass covers one existing OAuth session and one small fixture, not fresh
login, large-file transfer, physical quota exhaustion or network/auth recovery.
