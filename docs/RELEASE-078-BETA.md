# EasyMount 0.7.8-beta.1

Public testing release for Windows 11 x64. This is not Stable and the EasyMount
installer is not yet digitally signed. Google OAuth review and SignPath
Foundation enrollment are separate, pending processes. Windows may warn about
the unsigned installer; a checksum does not establish a trusted publisher.

## Changes

- Cloud account manager supports adding and removing accounts. Removing an
  account safely disconnects affected drives and adjusts/removes dependent
  pools, while preserving cloud files and blocking unverified pending writes.
- Account lists use the compact themed scrollbar.
- Cloud configuration is encrypted with rclone's authenticated format when no
  rclone workers are running. Its random key is protected by Windows CurrentUser
  DPAPI and passed to child processes through their environment.
- Free SignPath signing workflow and policy are prepared, not yet activated.

## Verification and limits

128 local core checks passed, including encrypted fixture migration, missing
key rejection, real local WinFsp mounts, worker recovery, pending-write refusal,
safe-uninstall preflight, priority-pool writes and complete drive cleanup.
36 localization/theme checks passed. These checks do not constitute fresh cloud
provider sign-in, Dropbox acceptance, a full UAC/reboot matrix or a long beta
feedback period. Signed install/uninstall remains unavailable until a trusted
signing service approves the project.

Keep both `rclone.conf` and `rclone.conf.key` and the original Windows profile
for recovery after encryption. Existing backups are not removed. Preserve
cache containing unuploaded files; wait for pending uploads before updating.
Do not upload private cloud configuration, tokens or cache to GitHub/support.

The stable update API stays unpublished until a Stable release is accepted.
This Beta is an explicit download on the website and GitHub prerelease page.
