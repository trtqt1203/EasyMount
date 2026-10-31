# Checks

- `dotnet run --project tests/EasyMount.Checks.csproj -- <absolute rclone.exe path>` checks configuration, ordered pool plans, rename safety, 1000 sources, loopback WebDAV and missing-driver handling.
- `python tests/check-pool.py <absolute rclone.exe path>` exercises the exact compiled engine with disposable account folders and constrained quotas, without mounting or contacting real cloud accounts.
- `tests/check-installer.ps1` extracts/decompiles the generated WiX bundle and checks payload hashes using ordinary WiX tools; it does not install the app or driver.

Use only disposable fixtures for checks. Real mounted-drive I/O and OAuth are separate manual acceptance steps.

Native setup checks include real WebDAV configuration, password obscuring, random RC authentication, cancellation, duplicate-name protection, and local OAuth callback lifecycle with fixture client credentials. They do not authenticate a real Google/Microsoft user.

Append `--mounts` after the rclone path to run disposable local WinFsp lifecycle checks on a free letter from Z: through T:. No personal cloud remote or existing drive is used. The test verifies an actual cached local write and safe unmount.

Uninstall integration was exercised with a disposable local WebDAV server and a spare Windows drive: an active-app pending upload and an orphan pending upload both blocked MSI `/x /qn` before app removal; after draining and checking the local write, Setup uninstall removed the app and drive while preserving WinFsp/account configuration hashes. Reinstall follows the test. See `docs/SAFE-UNINSTALL.md` and `docs/uninstall-integration-results.json`. Do not run this destructive app-removal check against personal mounted drives; use disposable local data and preserve account configurations.
