# EasyMount 0.7.0-beta.1 — candidate notes

This is a candidate, not a published Stable release. Setup-only, Windows x64.

- Migrates the desktop app and bundled runtime to .NET 10.
- Keeps read/write as the default while preserving explicitly read-only drives.
- Includes user-created workspaces, EN/VI settings, light/dark themes, readable
  system typography, drive filtering/details and full-list refresh.
- Pooled drives keep whole new files on the first source that has room; files
  are not split across accounts. Existing duplicate paths retain documented
  union epall update/delete behavior.
- Safe exit/uninstall protects pending cached uploads and removes drive mappings
  after a verified disconnect; shared WinFsp and user configuration remain.
- Help and update links use https://easymount.pages.dev/.

Known release limitations: Google OAuth remains External/Testing and requires
approved testers; OneDrive/Dropbox real multi-account acceptance is pending;
the local Setup candidate has no release signature. The public stable manifest
therefore remains unpublished. A short Pages address does not complete Google
domain verification.

See RELEASE-STATUS.md for evidence and gaps. Do not interpret local/CI tests as
proof of every real-provider or data-recovery scenario.
