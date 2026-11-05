# Release execution status

Updated 2026-11-05. Candidate source: 0.7.1-beta.2 (numeric MSI version 0.7.1). Installed: 0.7.0-beta.1.
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
- The developer Windows installation was safely upgraded from 0.6.15 to 0.7.0-beta.1+733122fb37dd7ea7a159e22f8a0e2a1ffd096d2e. Safe shutdown and Setup returned 0; installed app/engine hashes matched the candidate. All six checked configuration files were unchanged. R:–V: reconnected automatically and W: stayed disconnected, preserving the previous state. See release-070-upgrade-results.json.
- The installed candidate bundles .NET runtime 10.0.14 and the engine rebuilt using pinned Go 1.27.3. All five live Google mounts passed mounted-write/read SHA256, independent cloud-download SHA256 and cloud-confirmed deletion checks using unique generated 1 MiB fixtures. This is not a large-file/network-interruption test.
- Native UI showed version 0.7.0, five connected drives, and successful startup. Its actual update-check UI consumed the short-host API and displayed that no public release is available; no download was offered.

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
- Release source is now on branch release/0.7.1-beta.2. Source-only Windows CI passed at commit b4ad58d98f3f111ae324e292aa5c5acc0d288a92: https://github.com/trtqt1203/EasyMount/actions/runs/41855549280 . This rebuilt the pinned engine from source and ran the desktop/local checks on GitHub.
- The expanded clean install/mount/uninstall run passed for c072f568439be01437e9d32703910dd2358ce685: https://github.com/trtqt1203/EasyMount/actions/runs/41855887074 . It installed Setup on a clean GitHub-hosted Windows runner, matched installed payload hashes, ran actual local WinFsp mount tests, removed EasyMount and preserved shared WinFsp. Its disposable installer uses nonfunctional fixture OAuth and must never be distributed. Upgrade from the previous user version and full desktop UI acceptance remain separate.
- The same expanded CI passed for installed candidate source 733122fb37dd7ea7a159e22f8a0e2a1ffd096d2e: https://github.com/trtqt1203/EasyMount/actions/runs/41858657532 . The local 0.6.15 upgrade is now evidenced separately; standard-user/UAC, reboot-required and full pending-queue installation matrix still need coverage.
- User completed Microsoft sign-in. One OneDrive account is configured and passed four real direct-engine upload/download SHA256, Unicode path, rename, edit and fixture deletion checks. A second independent OneDrive account, OneDrive VFS acceptance and Dropbox remain pending.
- OneDrive discovery exposed a real onboarding defect: /me/drives returned four personal entries, three of which failed root lookup with HTTP 400 invalidRequest; the actual /me/drive entry was third and worked. Selecting that primary drive completed setup. Candidate engine .3 now prioritizes the canonical default drive. The regression test failed on .2 and passes on .3; an authenticated real discovery check confirms that the usable default is first. This source fix is not yet installed.
- Candidate app source falls back to the user's own drive owner email when /me profile data is unavailable, localizes yes/no choices, and hides the internal drive ID when normal choices exist. Build has zero warnings/errors; five identity parser cases are covered. Full UI/fresh-login verification of the next installed candidate remains pending.
- A local check run exposed shared obj/bin paths between the two .NET test projects: the wrong apphost launched localization checks instead of core checks. That run is not counted as core evidence. Candidate projects now have separate outputs, with runner-result guards in CI and clean install checks. Running localization then core now proves 36 localization/theme checks and 113 core/actual local WinFsp checks, respectively.
- Windows Sandbox is disabled on the user's machine; it has not been enabled or the computer restarted. Clean install/mount/uninstall acceptance ran on a disposable GitHub-hosted runner.
- Google domain verification guidance requires a DNS-level Domain property, not URL-prefix-only ownership. Awaiting the user's controlled domain; no domain purchase has been made. Source: https://support.google.com/cloud/answer/13804266?hl=en .

## Short website address

- User requested Cloudflare Pages. Dedicated project easymount was created in the existing Cloudflare account without modifying WakeStop.
- https://easymount.pages.dev/ is deployed and rendered in the browser; home, guide, privacy, logo and /api/releases each returned HTTP 200.
- The Pages advanced-mode gateway serves the existing public Sites origin. This is a short address, not a full hosting migration. Cookies/Authorization are not forwarded; upstream cookies are removed. GET/HEAD are accepted; the existing /api/pool-preview POST contract is preserved with a bounded 32 KiB body, while other POST paths remain disallowed. Fixed-origin, credential isolation, request-size, redirect and error handling checks passed. All 16 live API/page contract checks pass through the short host.
- Release API correctly remains unpublished/null download. No stable release or OAuth approval is implied by the new address. pages.dev does not provide DNS ownership of its parent, so Google verification remains unresolved.
- The installed candidate now uses the short website address for help/update links. Stable release metadata is unchanged and remains unpublished.
- Sites version 13 deployed successfully (source d7204a77084c14bbef4c43c225d6a8711265aaa3): preview version 0.7.0, copyright 2026, system typography, English default, persistent VI/EN and light/dark controls in Settings, no pre-created workspace. Desktop and 390px mobile Settings were checked in a browser. The old simulator UI and red warning remain removed.
- User stopped Computer Use with physical Escape. No further UI automation is being performed without renewed user authorization. Non-UI source, packaging and isolated tests continue; Dropbox interactive sign-in remains pending.

## 0.7.1-beta.2 packaging checkpoint

- Source commit bd796ad6c5e0939cb752d346fbd80e676f0c7c4c was pushed to release/0.7.1-beta.2.
- Setup built successfully and passed 33 compiled installer checks without installing the app or driver. SHA256: 78997d74b3de5ba21af582665eadbabf9d8612baa86d8d0f7b08d34d24856b47. Authenticode: NotSigned.
- Installed Windows app remains 0.7.0-beta.1. Native UI and fresh-login acceptance for 0.7.1 remain pending.
- Updated Windows CI run: https://github.com/trtqt1203/EasyMount/actions/runs/41944881628 (pending at this checkpoint).

- Engine .3 also passed the four real OneDrive direct-engine checks with an existing OAuth session; see release-071-onedrive-cloud-results.json. Native VFS/fresh-login acceptance is still pending.
- The 139 tracked website source files from deployed Sites commit d7204a77084c14bbef4c43c225d6a8711265aaa3 are now exported under web/ on the release branch; no production website change was made. All 16 short-host live page/API checks passed again. See WEBSITE-SOURCE.md.

## Runtime notice completion

- Windows CI 41944881628 succeeded for bd796ad6c5e0939cb752d346fbd80e676f0c7c4c, including clean Setup installation, actual local WinFsp mounts and uninstall.
- Packaging review found missing Microsoft runtime license/notices. Commit 57ded36e2cbdb667f65cb5cddff0717c7223e44f now includes the exact files from the officially verified SDK distribution and validates their embedded MSI hashes.
- Rebuilt candidate Setup SHA256 is b82d6286b3cee72d1be09c6cdc15404b7b036ee42f9d36a5dcb9b3c172de088d; 37 installer checks passed. Still NotSigned and not installed. The earlier unpublished candidate is preserved privately, not distributed.
- New packaging CI: https://github.com/trtqt1203/EasyMount/actions/runs/41945678142 (pending at this checkpoint).
- A Google verification dossier with scope justification/data flow/video requirements is prepared; it is not a submission or approval. See GOOGLE-VERIFICATION-DOSSIER.md.

- GitHub draft release 434644422 was created for source 57ded36, tagged v0.7.1-beta.2, with prerelease=true and draft=true. Setup and SHA256 assets were uploaded. Both were downloaded independently from authenticated GitHub asset endpoints and their byte counts/SHA256 matched. No public release/tag/download manifest was enabled. See release-071-draft-assets.json.
