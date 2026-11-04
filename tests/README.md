# Checks

- `dotnet run --project tests/EasyMount.Checks.csproj -- <absolute rclone.exe path>` checks configuration, ordered pool plans, rename safety, 1000 sources, loopback WebDAV and missing-driver handling.
- `python tests/check-pool.py <absolute rclone.exe path>` exercises the exact compiled engine with disposable account folders and constrained quotas, without mounting or contacting real cloud accounts.
- `tests/check-installer.ps1` extracts/decompiles the generated WiX bundle and checks payload hashes using ordinary WiX tools; it does not install the app or driver.

Use only disposable fixtures for checks. Real mounted-drive I/O and OAuth are separate manual acceptance steps.

`check-cloud.py --engine <exe> --config <config> --remote <account:> --report <json>` runs real-provider upload/download (SHA256), Unicode path, rename, edit and disposable-file deletion checks. Repeat `--remote` for each account. It creates a unique test directory, never uses existing cloud folders, and leaves failed fixtures intact for diagnosis. This is direct engine/API I/O, not evidence for interactive OAuth or mounted VFS behavior.

`--api-test` on the .NET core checks calls the public release metadata endpoint. Override `EASYMOUNT_TEST_API_URL` for a local server. An unpublished response can pass the API contract check; it does not prove an installer can be downloaded or updated.

The Windows CI workflow builds with pinned SDK/action versions and uses disposable local tests without user credentials. Real cloud, driver installation, signing and clean-machine upgrade acceptance remain separate gates.

`check-cloud-pool.py` takes the same engine/config/report options and exactly five `--remote` arguments. It uses separate UUID folders on real cloud accounts with an 8-byte test limit on each source to exercise priority placement, exhausted limits and documented duplicate-path `epall` behavior. The provider's physical storage is not filled. This direct union-engine test does not replace mounted VFS/cache safety or real network-disruption testing.

Native setup checks include real WebDAV configuration, password obscuring, random RC authentication, cancellation, duplicate-name protection, and local OAuth callback lifecycle with fixture client credentials. They do not authenticate a real Google/Microsoft user.

Append `--mounts` after the rclone path to run disposable local WinFsp lifecycle checks on a free letter from Z: through T:. No personal cloud remote or existing drive is used. The test verifies an actual cached local write and safe unmount.

Uninstall integration was exercised with a disposable local WebDAV server and a spare Windows drive: an active-app pending upload and an orphan pending upload both blocked MSI `/x /qn` before app removal; after draining and checking the local write, Setup uninstall removed the app and drive while preserving WinFsp/account configuration hashes. Reinstall follows the test. See `docs/SAFE-UNINSTALL.md` and `docs/uninstall-integration-results.json`. Do not run this destructive app-removal check against personal mounted drives; use disposable local data and preserve account configurations.
