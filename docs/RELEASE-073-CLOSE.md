# 0.7.3 close/unmount verification

Verified 2026-11-06 on the developer Windows installation. Source f8a823d9846d91ba27e136ea9f0b9bd21e4a33d1; Setup SHA256 D161102F5F369EE8C2276E3D54774354928963B852A91C8500724ADEA0D51959.

Closing the window now invokes safe shutdown by default. Keeping mounts active after closing requires the new explicit KeepMountedOnClose setting; legacy Background=true does not opt users in. A close requested during an operation is deferred until that operation ends. Shutdown recovers known worker leases and checks pending writes before unmounting.

- Local checks: 116 core/actual WinFsp checks, 36 localization/theme checks and 37 compiled installer checks passed.
- Installed upgrade exited 0; existing drive profiles were unchanged.
- Native GUI: a disposable local-backed Y: mount auto-connected with legacy Background=true. Closing immediately after a write was refused while the write was pending. The exact file content reached the backend; a second close removed Y: and all app/worker processes.
- Native GUI: opened the installed app with personal profiles, then requested close during startup auto-connect. After the ongoing operation finished, safe shutdown removed every mount and app/worker process. Explorer refreshed to show only C: under This PC.
- Prior surviving workers were recovered and gracefully stopped. A stale Z: shell entry was refreshed only after confirming no real mapping existed. No personal cache or cloud data was deleted.

Limitations: this does not prove safe behavior after forced process termination, power loss or reboot. Uploads/errors can intentionally prevent shutdown; the user must wait or resolve the upload. The reused safety dialog currently retains uninstall-oriented Vietnamese wording. GitHub CI was not verified because DNS resolution failed; no new release was published.

See release-073-close-results.json for the final installed state.
