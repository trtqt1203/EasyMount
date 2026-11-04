# Release execution status

Updated 2026-11-04. Candidate: 0.7.0-beta.1 (numeric MSI version 0.7.0).
This is not a public stable release.

## Verified in this run

- SDK 10.0.403 downloaded from Microsoft and checked against its official SHA512; runtime 10.0.14.
- App and both test projects migrated to net10.0-windows; Release build passed with no warnings or errors.
- 36 localization/theme checks passed.
- 18 local engine integration checks passed with v1.75.2-easymount.2.
- 108 core checks passed, including isolated real Windows WinFsp mounts, priority/quota fallback, worker recovery, safe disconnect, and live public release API consumption.
- These tests use disposable local storage. They do not prove cloud-provider writes, multi-account OAuth, or a clean-machine installation.
- Installed personal app has not been upgraded by this run; existing cloud data and mounts are preserved.

## Build changes

- SDK pinned in global.json. Target framework and candidate version centralized in Directory.Build.props.
- UI version labels and installer versions derive from build metadata.
- Removed unused embedded Inter fonts; current UI uses the Windows system font.
- Packaging requires a configured Desktop OAuth identity; it no longer silently ships the empty example. Supply OAuthClientsPath when building outside the local checkout. Never commit a user's cloud tokens or rclone.conf.
- Live API tests default to the production release endpoint; EASYMOUNT_TEST_API_URL can explicitly select a local test server.

## Google Cloud observed directly

- Correct signed-in project account: travistrtqt@gmail.com. No additional login required at this checkpoint.
- Project easymount-513208; Audience External / Testing; 5 test users configured.
- Publish app is disabled pending Branding completion.
- App name and support/developer contact are configured; homepage, privacy policy, terms, logo and authorized domains are empty.
- Domain ownership, permission justification and verification remain release gates. No Production toggle or verification submission has been made.

## Still required

- Complete the remaining gates in RELEASE-PLAN.md: real multi-account tests for all three providers, five-cloud pool/data safety tests, clean Windows install/upgrade/uninstall evidence, signing, sanitized source publication, downloadable release/update verification, and beta feedback period.
- A passing local suite or installer build must not be reported as completion of those gates.
