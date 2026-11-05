# EasyMount 0.7.1-beta.2 — candidate notes

Setup-only Windows x64 candidate; not a published Stable release.

- OneDrive setup now puts the canonical personal drive first instead of
  defaulting to an internal Microsoft container that can reject file access.
- OneDrive account identity can use the drive owner's email when profile
  lookup is unavailable, without requesting additional profile permissions.
- Setup displays localized confirmation choices and hides internal drive IDs
  when the user can select a normal named drive.
- The two desktop check runners now use separate build outputs. CI requires
  each core runner to report its own result, preventing a localization-only
  invocation from being counted as core coverage.
- The short website gateway preserves the existing bounded pool-preview API.

Verification: 113 core/local WinFsp checks, 36 localization/theme checks,
18 engine checks, gateway checks and a warning-free desktop build passed.
The OneDrive ordering regression fails on engine .2 and passes on .3;
authenticated discovery also confirms the canonical drive is first.
One real OneDrive account passed direct upload/download integrity, rename,
edit and disposable-file deletion checks on the installed previous candidate.
The new candidate still needs fresh-login and native UI acceptance.

Google OAuth remains Testing. Second-account OneDrive, Dropbox, broader
failure recovery, release signing and the real beta feedback period remain
release gates. See RELEASE-STATUS.md and RELEASE-PLAN.md.
