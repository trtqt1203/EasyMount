# Google OAuth demonstration capture

Status: capture plan, not a completed recording or a submitted review.
Use the tested release candidate, English UI, and an account owned by the
maintainer. Keep personal files, tokens, secrets and unrelated browser tabs
outside the recording. Do not use mock screens or invented video URLs.

## Recording sequence

1. Open https://easymount.name.vn/ and its Privacy Policy link. Show the
   EasyMount product name and explain: "EasyMount is a Windows desktop
   filesystem client. Cloud transfers occur directly on the user's PC."
2. Open the exact candidate's About dialog to establish its version. Use a
   separate application data directory for this demonstration, with no
   pre-existing personal profiles and no automatic mount connections.
3. Add Google Drive with upload/edit permission enabled. Show the complete
   browser authorization flow, the application name, requested permissions,
   and the URL containing the OAuth client ID. Include any expected Google
   unverified-app notice; do not splice in an old consent screen. The owner
   enters any required password/verification privately.
4. Explain: "Full Drive access is needed to browse and operate on existing
   arbitrary files in Explorer. drive.file alone does not provide that
   filesystem access." Show the actual consent grant and return to the app.
5. Mount a free drive letter. In a newly generated EasyMount-OAuth-demo-UUID
   folder, show an existing generated file, download/read it, upload a small
   local text file, edit and rename it. Independently show those changes in
   Google Drive. Remove only disposable demonstration files using normal
   provider recovery semantics. Never demonstrate against personal content.
6. Safely disconnect and show the drive disappearing from Explorer.
7. Start a separate Google Drive authorization with upload/edit disabled.
   Show the real drive.readonly consent and demonstrate listing/reading an
   existing generated file. Show the application's read-only access label.
8. Show adding another independently authorized account, if demonstrating
   the multi-account feature. Each owner grants their own permission.
9. End with the privacy link and explain: "The website and update API do not
   receive Google Drive tokens or file contents. No Drive data is used for
   advertising or AI training. Local pending uploads are preserved until
   safe disconnect can verify completion."

## Before attaching the URL

- Use English narration/captions and a readable browser address bar.
- Cover every OAuth client assigned to this project that is used by the app;
  inspect the Clients page rather than assuming one client exists.
- Review the complete capture for private information before uploading.
- Upload the actual recording to the owner's YouTube channel as Unlisted.
  New channel/account terms and any upload visibility change require the
  applicable owner confirmation. Do not publish credentials in any field.
- Paste the real accessible URL into Google Auth Platform > Data Access >
  YouTube link, save the justification, then inspect the verification summary.
- Submit only truthful answers. Branding verification is already complete;
  restricted-scope approval and any assessment decision remain with Google.

Reference:
https://developers.google.com/identity/protocols/oauth2/production-readiness/restricted-scope-verification
