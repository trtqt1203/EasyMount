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

- Final packaging CI 41945678142 succeeded for source 57ded36e2cbdb667f65cb5cddff0717c7223e44f: build, localization, gateway, rebuilt engine, core checks, priority checks, fixture Setup, 37 installer checks and clean Windows install/mount/uninstall. The CI fixture has nonfunctional OAuth and is not the draft release artifact.

- One existing OneDrive account also passed seven real temporary-VFS acceptance checks with engine .3 and the unchanged candidate DriveService: unique disposable root, service connect, mounted 1 MiB Unicode write/read SHA256, independent cloud download/hash, mounted deletion confirmed on cloud, empty-root removal and safe disconnect. Both the test drive and Windows mapping disappeared and no test session lease remained. Personal drive profiles were not changed; private credential/cache/recovery files are retained outside the source. See release-071-onedrive-vfs-results.json and tools/cloud-mount-check. This does not cover a second account, fresh OAuth, large files or interrupted uploads.

## Owner follow-up and installed identity repair

- Owner declined further interactive Dropbox/second-OneDrive sign-in; no browser/native UI automation was resumed. Those tests are deferred by the owner, not marked passed.
- Owner confirmed there is no controlled custom domain. Google DNS ownership/verification therefore remains unresolved; no domain was purchased.
- Owner requested the OneDrive line below the drive name be fixed. Installed account cache lacked the OneDrive email, while the candidate's owner fallback resolved it successfully. The verified same-drive email was added atomically to the installed account-emails cache after a private backup. Existing cached emails, drive profiles/names, account labels and OAuth configuration were verified unchanged. Native UI was not controlled or observed; its Refresh action reloads accounts. See release-071-onedrive-email-repair.json.

## Five-source real VFS acceptance

- The five Google remotes selected from the existing user pool passed 24 acceptance checks through a NEW isolated VFS mount, not the existing personal pool. Each 1 MiB Unicode file reached the next expected cloud source under controlled 1.5 MiB folder limits. Direct provider downloads matched hashes; direct listings confirmed exactly one assigned whole file per source. Mounted deletions were confirmed on all five clouds. Empty generated roots were removed nonrecursively; safe disconnect removed the temporary logical drive/network mapping and all session leases. No personal mount worker/profile was changed. See release-071-google-pool-vfs-results.json.
- This does not establish physical quota exhaustion, all-full cached upload recovery, large-file or network interruption behavior. Original direct union-engine all-full/duplicate tests remain separate evidence. No new OAuth sign-in was performed.

- OneDrive also passed seven temporary-VFS lifecycle checks with a generated 128 MiB Unicode fixture after verifying local/cloud headroom. Mounted readback and an independent cloud download matched SHA256, mounted deletion was confirmed on cloud, the empty UUID root was removed, and safe disconnect left no drive/network mapping/session lease. This covers that recorded file size, not every size boundary or network/auth recovery. See release-071-onedrive-128mib-vfs-results.json.
- RELEASE-READINESS-AUDIT.md consolidates the original plan gates and separates proven, deferred and external/pending requirements. Full Stable completion remains unproven. The owner has been asked whether to retain the concrete Draft or publish a limited unsigned Beta; no response or publication is assumed.

- Final read-back confirmed GitHub release 434644422 is still Draft/prerelease with the original verified two asset digests. All three isolated cloud acceptance runs report passed and have zero session leases. Live short-host page/API checks passed (16); /api/releases returned 200/unpublished/null download with the installed app's EasyMount/0.4 User-Agent. Python-urllib/3.13 received 403, reproducible by changing only User-Agent; this is recorded separately rather than treated as an app-update failure. No hosting security setting was changed.
- Independent source/build/cloud acceptance preparation is complete at this checkpoint. Completing the original Stable goal still requires owner/external actions listed in RELEASE-READINESS-AUDIT.md: publication choice, DNS-controlled Google domain/review, signing route, authorized manual acceptance and actual beta feedback. No public Beta, Stable, OAuth approval, signature or elapsed tester period is assumed.

## Domain blocker resolved — 2026-11-05

- easymount.name.vn is active at Tino and DNS now uses Cloudflare Free. Apex and www Pages domains both show Active / SSL enabled and return HTTPS 200. Privacy and release API return 200; all 16 live API/page checks pass on the apex.
- Google Search Console confirmed DNS Domain-property ownership with the project-owner account. Google OAuth Branding saved the new homepage, privacy URL and authorized domain. This supersedes earlier missing-domain statements; OAuth Production/restricted-scope approval and the other release gates remain separate. See DOMAIN-SETUP.md.

## Google branding published - 2026-11-05

- With explicit owner approval, OAuth changed to Production. Canonical logo was uploaded; Google's branding verification succeeded and verified branding was published. Console confirms it is shown to users.
- Restricted drive / drive.readonly data access remains unverified. The unverified cap remains 100 users (5 used at this checkpoint). Verification preparation is accessible; intended use and scope justification were entered, but cannot be saved without the required real YouTube demo URL. No data-access review submission or approval is claimed. See GOOGLE-VERIFICATION-DOSSIER.md.

## Installed 0.7.1 and real desktop acceptance checkpoint

- Existing-owner safe shutdown/preparation returned 0. Original personal metadata/configuration was backed up privately; caches were not deleted. Exact draft Setup b82d6286... installed successfully (exit 0), without reboot. Installed ProductVersion is 0.7.1-beta.2+57ded36e2cbdb667f65cb5cddff0717c7223e44f; app and engine SHA256 match the original packaged payload.
- Seven profiles, labels, workspace and UI settings survived byte-for-byte. rclone.conf changed only token lines during legitimate refresh. The app was observed restoring the same six online mounts and OneDrive's email line. This does not prove arbitrary file integrity, a deliberately pending cloud queue, interactive UAC or reboot behavior. See release-071-installed-upgrade.json.
- Native Settings changed English/dark to Vietnamese/light, then Vietnamese/dark and saved successfully. Restored/maximized layout was observed. Actual Windows Display scale on the 1920x1080 second display was changed 100% -> 125% -> 150% -> 100%; first display's 150% scale was not changed. At higher dynamic DPI the candidate was visibly bitmap-scaled/blurry. Long account text uses ellipsis and the list scrolls vertically; no claim of full matrix acceptance.
- Compiled original PE manifest has no per-monitor declaration. Added a WPF app.manifest with PerMonitorV2 and legacy true/pm, retaining asInvoker/uiAccess=false. WFO0003 is suppressed only for this WPF startup path because the manifest must take effect before the first HWND; no WinForms startup API is used. Self-contained QA publish succeeded without warnings/errors, 36 localization/theme checks passed, and the compiled QA PE manifest was independently inspected for all three settings.
- The DPI source change is NOT in the installed draft artifact and has NOT passed native retest. The isolated no-credential/no-autoconnect QA launch was not completed: native control coordinates became invalid across displays, including an Explorer Run error dialog. The user was asked to close those two dialogs. No personal cloud worker was force-killed and no cache was removed. Original English/dark desktop settings were restored after another successful safe close, and the installed app was launched again; post-launch UI was not re-observed.
- No signed binary, Google data-access approval, reboot acceptance or complete Stable result is claimed.
