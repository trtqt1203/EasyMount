# Code signing policy

## Current status

SignPath Foundation application preparation is in progress. Approval has not
been granted and no EasyMount release currently carries a SignPath signature.
Existing EasyMount installers are unsigned. SHA256 files are integrity checks,
not publisher signatures. Do not install a self-signed root certificate or
disable Windows security protections to use EasyMount.

The intended free program is [SignPath Foundation](https://signpath.org/).
After acceptance, the required attribution will be:
“Free code signing provided by [SignPath.io](https://about.signpath.io/),
certificate by [SignPath Foundation](https://signpath.org/).”
This describes the requested arrangement, not an approval already received.

## Maintainer responsibilities

- Author/committer: [trtqt1203 (TravisTRTQT)](https://github.com/trtqt1203).
- Reviewer: trtqt1203 reviews external contributions and build changes.
- Signing approver: trtqt1203 manually approves each signing request in SignPath.

The maintainer must enable MFA on GitHub and SignPath before production signing.
MFA and account configuration have not been verified by this document.

## What may be signed

Only EasyMount-owned artifacts built from the public repository
<https://github.com/trtqt1203/EasyMount> on GitHub-hosted Windows runners:

1. EasyMount.exe and EasyMount.dll.
2. The EasyMount MSI containing that signed application.
3. The detached WiX Burn engine, followed by the final reattached Setup bundle.

The installer keeps the original signed WinFsp MSI unchanged. Microsoft runtime
files and the modified upstream rclone engine are bundled under their own
licenses and are not re-signed with EasyMount's certificate. The Foundation must
review this packaging scope, including the WiX Burn engine, before activation.

## Release controls

The manual `Sign Windows release` workflow only accepts a version-matching Git
tag, builds from source, and uploads each unsigned stage as a GitHub Actions
artifact before submission. SignPath must verify the GitHub build origin and
require manual signing approval. No unsigned fallback is allowed in this
workflow. Every returned artifact must have a valid timestamped Windows
signature matching the approved certificate thumbprint. Hashes are calculated
after the final signature. The workflow produces reviewable artifacts; it does
not automatically publish a GitHub release or install software on user devices.

## Privacy

See the [EasyMount privacy policy](https://easymount.name.vn/privacy).
Users explicitly choose cloud services and authorize provider access. File and
cloud-token operations run on the user's computer; the product website is not
a file-transfer proxy. Product/update metadata requests contact the website.
Google Drive, Microsoft OneDrive and Dropbox handle authorized data under their
respective privacy policies. See bundled third-party notices for rclone,
WinFsp and Microsoft .NET. No user cloud configuration, cache, tokens or
personal files may be included in a CI artifact or signing request.
