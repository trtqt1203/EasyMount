# Beta acceptance record

Do not start the 5–7 day beta period until an immutable downloadable candidate,
its SHA256 and the access limitations are available to the testers. A CI run or
developer test session is not the beta period.

## Candidate and environment

Record candidate version/tag, source commit, Setup SHA256, signature status,
Windows edition/build, display scale, app language/theme, provider and account
count. Use aliases such as Google account A; never put emails, tokens, passwords,
full rclone.conf, private file paths or complete unsanitized logs in public issues.

## Test matrix

| Area | Required observation |
| --- | --- |
| Setup | Clean install without WinFsp; install with same/newer WinFsp; standard-user/UAC outcome; reboot-required outcome |
| Upgrade | Previous supported version upgrades; profile/settings/workspace hashes and previous connection state are preserved |
| Cloud login | Two distinct accounts each for Google Drive, OneDrive and Dropbox; cancellation and expired/revoked authorization |
| File I/O | Generated Unicode/English files, small and large files; upload/download checksums, rename, edit and delete only disposable fixtures |
| Five-source pool | Whole-file placement in priority order, next-source fallback, every-source-full failure, reordered priorities, duplicate paths |
| Failure recovery | Controlled network/auth failure with disposable data; pending cached data remains recoverable and is verified on cloud after recovery |
| Exit/uninstall | Pending uploads block removal; drained queues allow clean disconnect and removal; app mappings disappear and shared WinFsp remains |
| Workspaces/UI | Initially no workspaces; Enter creates, deletion keeps drives/files, state survives restart; EN/VI, light/dark and multiple DPI settings |
| Updates | Website download matches the published SHA256; app uses the same release manifest; actual signature status is shown honestly |

For each result record date, tester alias, exact candidate, pass/fail/not-tested,
expected behavior, observed behavior, and a sanitized evidence reference. Record
failures and retries separately. Never replace a failed result with a pass merely
because a later retry worked.

## Daily record

| Date | Tester alias | Candidate/hash | Area/provider | Outcome | Sanitized evidence or issue |
| --- | --- | --- | --- | --- | --- |
| Not started | — | — | — | Not tested | — |

## Stable gate

At the end of 5–7 actual days, review all failures, provider authorization limits,
signing, clean-machine/upgrade evidence and the remaining RELEASE-PLAN.md gates.
Do not treat an empty daily record or absence of bug reports as a successful beta.
