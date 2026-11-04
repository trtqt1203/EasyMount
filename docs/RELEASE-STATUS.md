# Release execution status

Updated 2026-11-04. Candidate: 0.7.0-beta.1 (numeric MSI version 0.7.0).
This is not a public stable release.

## Verified in this run

- SDK 10.0.403 downloaded from Microsoft and checked against its official SHA512; runtime 10.0.14.
- App and both test projects migrated to net10.0-windows; Release build passed with no warnings or errors.
- 36 localization/theme checks passed.
- 18 local engine integration checks passed with v1.75.2-easymount.2.
- 108 core checks passed, including isolated real Windows WinFsp mounts, priority/quota fallback, worker recovery, safe disconnect, and live public release API consumption.
- 33 compiled installer-structure checks passed; no personal account profiles were bundled. This did not install the candidate.
- Five existing Google Drive accounts passed direct real upload/download SHA256, Unicode naming, rename, edit and disposable-file deletion checks. Unique generated 4 MiB fixtures were used per account. Account 1 had one cleanup failure; a targeted cleanup and a fresh complete account-1 run succeeded. The original transient cause is undetermined.
- Five real Google Drive sources passed 22 union-engine checks: whole-file priority placement through all five sources, reads, rejection when all configured limits were exhausted, duplicate read priority, epall update and deletion across all sources. Test limits were 8 bytes per isolated source; physical cloud quotas were not filled. This does not prove VFS caching, real physical-quota exhaustion or loss-of-network behavior.
- Configured remotes resolve to five distinct cached email identities. Existing sessions were used; this run does not prove fresh interactive multi-account OAuth.
- Clean export builds with no warnings/errors, and passes 36 UI/localization plus 84 local core checks without personal configuration.
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
- Release source is on branch release/0.7.0-beta.1. Source-only Windows CI passed at commit b4ad58d98f3f111ae324e292aa5c5acc0d288a92: https://github.com/trtqt1203/EasyMount/actions/runs/41855549280 . This rebuilt the pinned engine from source and ran the desktop/local checks on GitHub.
- The expanded clean install/mount/uninstall run passed for c072f568439be01437e9d32703910dd2358ce685: https://github.com/trtqt1203/EasyMount/actions/runs/41855887074 . It installed Setup on a clean GitHub-hosted Windows runner, matched installed payload hashes, ran actual local WinFsp mount tests, removed EasyMount and preserved shared WinFsp. Its disposable installer uses nonfunctional fixture OAuth and must never be distributed. Upgrade from the previous user version and full desktop UI acceptance remain separate.
- OneDrive/Dropbox accounts are not configured locally. User has been asked to add test accounts through the app; no password or token is requested in chat.
- Windows Sandbox is disabled on the user's machine; it has not been enabled or the computer restarted. Clean install/mount/uninstall acceptance ran on a disposable GitHub-hosted runner.
- Google domain verification guidance requires a DNS-level Domain property, not URL-prefix-only ownership. Awaiting the user's controlled domain; no domain purchase has been made. Source: https://support.google.com/cloud/answer/13804266?hl=en .

## Short website address

- User requested Cloudflare Pages. Dedicated project easymount was created in the existing Cloudflare account without modifying WakeStop.
- https://easymount.pages.dev/ is deployed and rendered in the browser; home, guide, privacy, logo and /api/releases each returned HTTP 200.
- The Pages advanced-mode gateway serves the existing public Sites origin. This is a short address, not a full hosting migration. Cookies/Authorization are not forwarded; upstream cookies are removed; only GET/HEAD are accepted. Fixed-origin, redirect and error handling checks passed.
- Release API correctly remains unpublished/null download. No stable release or OAuth approval is implied by the new address. pages.dev does not provide DNS ownership of its parent, so Google verification remains unresolved.
- App source now uses the short website address for help/update links; installed desktop is still unchanged until a safe candidate upgrade is performed.
