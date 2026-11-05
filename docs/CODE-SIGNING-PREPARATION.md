# Code signing preparation

Checked 2026-11-05. Candidate 0.7.1 remains NotSigned; no signing service
has been configured, no application submitted and no certificate purchased.

SignPath Foundation is a possible free option for qualifying open-source
projects, not an entitlement or an approved integration. Its current terms
require an actively maintained, documented, already-released OSS project,
verifiable builds, MFA, defined contributor/signing roles and manual approval.

The custom rclone engine needs particular review: signing modified upstream
binaries has additional conditions, including visible fork/source provenance.
Do not assume EasyMount's current overlay build satisfies those conditions.
Preserve the original signed WinFsp MSI. Do not sign Microsoft or upstream
binaries as if they were authored by EasyMount.

Before applying, the owner must choose the signing route, confirm eligibility
and roles, and approve any provider terms. Then build from the approved source,
sign the owned executable components, build/sign MSI, build/sign Setup, verify
the resulting signature chain/timestamp and only then calculate release hashes.
Never overwrite an already-published artifact or set `signed: true` based on a
checksum, CI success, self-signed certificate or an application to a service.

The present CI installer uses fixture OAuth and is not a distributable release.
A production signing workflow must build the real authorized desktop identity
and keep user tokens/configuration out of both artifacts and logs.

Official references:

- https://signpath.org/terms
- https://signpath.org/
- https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options
