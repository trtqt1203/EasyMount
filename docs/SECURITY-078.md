# Security changes 0.7.8

Cloud configuration migration uses rclone's authenticated encrypted format. A random password is stored using Windows CurrentUser DPAPI in rclone.conf.key. Both files and the original Windows profile are needed for recovery. Every app-owned rclone subprocess receives the decrypted password through its process environment, not command arguments or logs. This protects copied configuration files at rest; it does not protect against malware executing as the same Windows user or an administrator.

Migration is deferred while any rclone worker is running. A temporary config is encrypted and tested with listremotes before replacing the original. Failures retain the original configuration. Existing user-created plaintext backups are not automatically deleted. Session recovery and safe-uninstall preflight were exercised with encrypted local fixtures. Elevated real-user uninstall and the complete UAC/reboot matrix still require acceptance before Stable.

The Pages gateway now sets HSTS (one day, no includeSubDomains/preload), nosniff, DENY framing, strict-origin referrers, disabled camera/microphone/geolocation, and a CSP limiting base URLs, objects and embedding. This is intentionally not a full script-src policy. Source tests do not establish that headers are deployed.

tools/sign-release.ps1 signs and timestamps a Setup with an existing trusted code-signing certificate, verifies the result and updates SHA256. No certificate with a private key was present during implementation. Unsigned builds remain unsigned; no self-signed publisher identity is substituted.

Validation: desktop build passed; 128 core checks including encryption migration/restart/missing-key and actual local WinFsp mount/recovery/disconnect/pool cases passed; gateway checks passed. Real-user configuration migration and live deployment are not yet verified.
