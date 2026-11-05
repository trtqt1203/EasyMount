# Release readiness audit — 2026-11-05

Scope remains the complete RELEASE-PLAN.md, not merely a green build.
Current internal candidate: 0.7.2-beta.1, source f66747fc85fbdcda6444ee8bc943d7c57e0cdbba.
Installed desktop is now 0.7.2-beta.1+f66747f; upgrade and six restored connections were observed directly. Original 0.7.1-beta.2 remains a separate GitHub Draft.
This audit does not authorize publication or claim Stable acceptance.

| Requirement | Evidence and current result | Remaining gate |
| --- | --- | --- |
| .NET 10 migration and reproducible build | Pinned SDK/runtime; clean Windows CI 41945678142 succeeded; BUILD.json | 0.7.2 installed UI and per-monitor process mode observed; exact packaged source f66747f CI 42031122476 succeeded, including clean install/mount/uninstall |
| Google public multi-account OAuth | Five existing identities passed real file tests; five-source VFS passed 24 checks | DNS domain ownership verified; Production and verified/published branding completed with owner approval. Restricted-scope review awaits a real YouTube OAuth/use demo; no submission or data-access approval |
| OneDrive | Existing account passed direct provider operations, 1 MiB and 128 MiB temporary VFS write/read SHA256, independent cloud verification/deletion and safe unmount; installed email cache repaired | Exact candidate fresh-login/UI and second independent account unverified; further sign-in declined by owner; not every file-size boundary is covered |
| Dropbox | Implemented provider flow; no real-provider acceptance in this run | Further interactive sign-in declined by owner; do not describe it as verified |
| Five-source whole-file priority | Direct engine tests and 24 real pooled-VFS checks; each generated file independently found on its expected source, no split/duplicate, deletion and unmount verified | Controlled folder limits are not physical quota exhaustion; network/auth interruption recovery and broader cloud failure cases remain unverified |
| Pending upload/recovery protection | 113 local core/WinFsp checks include pending uploads, orphan sessions, recovery and refusal of unsafe removal | These local checks cannot prove every real-cloud interrupted upload scenario |
| Workspace, language, theme, typography/DPI | Localization/theme checks and earlier native/browser observations; website version 13 rendered on desktop/mobile | DPI fix validated in a separate app: runtime per-monitor awareness confirmed; owner confirmed clear physical display after transition. Installed 0.7.2 Settings exercised EN/VI and light/dark; main window observed VI/light and EN/dark. Broader DPI/GPU matrix remains limited |
| Clean install/upgrade/uninstall | Clean Windows CI install/mount/uninstall passed; 0.6.15 to installed 0.7.0 upgrade preserved checked config and connections | Existing-owner 0.7.0 to exact 0.7.1 upgrade passed, safe preparation returned 0, 7 profiles preserved and 6 connections restored. Standard-user/UAC/reboot-required and deliberate pending-queue upgrade matrix remains incomplete |
| Distribution licenses | WinFsp signed original/source archive, rclone MIT, Microsoft license/notices included; 37 installer checks passed | Keep third-party notices/signatures intact in future builds |
| Trusted release signing | Setup Authenticode is NotSigned; signing preparation records possible routes | Owner-selected/approved signing route and valid final signatures/timestamps are absent; no purchase or provider agreement made |
| Public source and exact artifact | Public release branch includes desktop/website source; Draft 434644422 contains Setup and SHA256, downloaded independently and matched | Draft publication choice pending; draft is not an immutable public release/tag |
| Website/download/update chain | Public short website/API works; 16 live contract checks; installed earlier candidate consumed real update API | Stable manifest deliberately remains unpublished; no public download/install/update full-chain acceptance for 0.7.1 |
| Real beta period | BETA-TESTING.md and sanitized issue template prepared | No 5–7 actual days of tester feedback; cannot replace with automated tests or elapsed tool runtime |

Current Setup SHA256:
`e312e298859a4f7dcf1b1e68ecbc22ab993e8971f775d28ff8e9d6aea56ed7dc`.
Current internal artifact/install evidence: release-072-installed-upgrade.json. Original 0.7.1 draft assets and independent downloads: release-071-draft-assets.json. The current 0.7.2 artifact is not uploaded/published.
Windows CI: https://github.com/trtqt1203/EasyMount/actions/runs/41945678142 .
Detailed cloud/upgrade evidence and limitations: RELEASE-STATUS.md and the
release-070/release-071 JSON reports. Failed runs/retries are retained separately.

## Actions requiring owner input or external completion

- Decide whether to keep the exact unsigned, provider-limited candidate Draft
  or explicitly publish a limited Beta with the recorded limitations. This does
  not waive the original Stable gates. A concrete Draft and checksum exist.
- Complete the real English OAuth/use demonstration video, then save and submit
  restricted-scope verification. Domain ownership and branding are already done.
- SignPath free-program form is prepared. Required personal contact and legal/data
  consent approval is still pending; no application has been submitted. Preparation
  is not provider acceptance or a valid signature.
- Resume any required manual acceptance only if the owner authorizes it; further
  Dropbox/second-OneDrive sign-in was explicitly declined and is not retried.
- After an accepted downloadable Beta exists, collect actual tester feedback
  for the planned period. No invitations or messages were sent to other people.
