# Google OAuth verification preparation

Prepared 2026-11-05. Draft for the project owner; not submitted or approved.
Project: easymount-513208. Product: EasyMount, Windows desktop filesystem client.
Current source: release/0.7.1-beta.2. Current public site:
https://easymount.pages.dev/ ; privacy: https://easymount.pages.dev/privacy .

## Scope justification for the review form

EasyMount lets users mount their existing Google Drive files in Windows
Explorer, read them on demand, and, when they choose write access, upload,
edit, rename and delete files from Explorer. Users can combine multiple
authorized accounts into one virtual drive with whole-file write priority.
Each account is separately authorized by its owner.

- `https://www.googleapis.com/auth/drive`: requested when the user enables
  upload/edit access. Existing arbitrary files and folders must be accessible
  through the mounted filesystem, including files not created by EasyMount.
- `https://www.googleapis.com/auth/drive.readonly`: requested for explicitly
  read-only authorization. It supports listing and reading existing files
  without granting write access. The dialog selects one scope for that flow;
  it does not request both merely because both are declared in the console.
- `drive.file` is narrower but does not implement the requested filesystem
  access to arbitrary existing Drive content. Replacing the scope with it
  would change the product's behavior; it is not a verification workaround.

Source reference: AccountWindow.cs sets scope from the user's write choice.
Google classifies the two full-drive scopes as restricted; classification and
accepted use must be reviewed by Google, not inferred from a passing test.

## Actual data flow to explain

1. The desktop app launches a local rclone worker and provider browser login.
2. The user authenticates with Google, reviews the scopes and grants access.
3. Account credentials/configuration remain in the Windows user's EasyMount
   data directory. An authenticated local control endpoint binds to loopback.
4. The local worker communicates directly with Google APIs to list metadata,
   obtain quota, read files and execute authorized file operations.
5. Cached file content stays on the user's machine. Pending uploads must be
   drained and verified before safe disconnect/removal can complete.
6. The public website/API provides documentation and release metadata. It
   does not receive cloud tokens or file contents. Hosting still processes
   normal technical web-request data. Do not describe this as zero data use.

No Google data is used for advertisements or AI training by EasyMount.
The published privacy page contains the Limited Use statement. Do not submit
real tokens, configuration files, personal file listings or unsanitized logs.
The applicability of a security assessment must be confirmed against the
architecture and Google's review; this document does not grant an exemption.

## Demonstration recording still required

Use disposable accounts/data and the exact candidate. Record the product
name, homepage/privacy links, Google account chooser and actual consent
screen with requested scopes visible. Show connecting a drive, listing a
generated folder, uploading/downloading and renaming a generated file,
then disconnecting safely. Separately show the read-only choice. Show a
second account being added independently; avoid exposing personal content.
Do not replace the consent flow with screenshots from an earlier build.
No recording or verification submission has been completed by this document.

## Owner inputs and outstanding submission gates

- A domain whose DNS the project owner controls. The Pages address is live,
  but does not itself provide DNS ownership of the parent pages.dev domain.
- Verify a DNS-level Domain property with the correct Google project-owner
  account; then configure homepage/privacy/authorized domain and branding.
- Confirm the support/developer contacts, declared scopes, final artifact,
  demo video and complete truthful responses in the Google review form.
- Retain Testing restrictions until a real change is observed in Console.
  Switching audience state alone is not restricted-scope verification.

No domain purchase, Production toggle, review submission or approval is
recorded here. Do not tell users that unrestricted Google login is available.

## Official references checked 2026-11-05

- https://developers.google.com/workspace/drive/api/guides/api-specific-auth
- https://developers.google.com/identity/protocols/oauth2/production-readiness/restricted-scope-verification
- https://support.google.com/cloud/answer/13804266?hl=en
