# EasyMount Google OAuth setup

Date: 2026-10-30 (Asia/Bangkok).

## Configured and verified

- Project: EasyMount (`easymount-513208`), no organisation.
- Console: https://console.cloud.google.com/auth/overview?project=easymount-513208
- Google Drive API: Enabled.
- Branding: EasyMount; existing Google account used for support and developer contact.
- User confirmed acceptance of the Google API Services User Data Policy before submission.
- OAuth client: EasyMount Windows, Desktop app; created successfully.
- Audience: External / Testing. Existing owner account added and visibly verified as a test user.
- Declared scopes: `https://www.googleapis.com/auth/drive` and `https://www.googleapis.com/auth/drive.readonly`; Data access changes saved visibly verified.
- App configuration: `oauth-clients.json` contains the desktop application client ID and client secret. This is a public desktop client; the distributed secret is not a server authentication boundary.
- EasyMount 0.3.1 build and installer include the app OAuth configuration. User tokens, passwords and personal drive profiles are excluded.

## Runtime verification

- Launched native EasyMount 0.3.1 with a separate data directory and opened Google Drive setup without entering client ID/secret.
- Browser reached Google's account chooser displaying EasyMount.
- On continuation, an authenticated Google session was already present in that local test directory. A read-only `rclone about` request against it succeeded and returned Drive quota. Tokens and quota values were not printed.
- No upload, delete or write test was performed. Windows mount data integrity and long-running refresh remain unverified.
- The local account configuration remains outside release artifacts. Do not copy it into source or installer payloads.

## Community release remains pending

Testing allows only listed testers (up to 100). Drive refresh tokens issued in Testing expire after 7 days. This is not public production approval.

Before community production release, complete branding, owned/verified domains and public homepage/privacy disclosures, and the applicable Google review for Drive restricted scopes. Each user must grant their own requested access.

Sources:
- https://support.google.com/cloud/answer/15549945?hl=en
- https://developers.google.com/identity/protocols/oauth2/native-app
- https://developers.google.com/identity/protocols/oauth2/production-readiness/restricted-scope-verification
## 0.3.2 follow-up

Observed Google 403 reason: trtqt1203@gmail.com is not an approved tester. Only the initial owner account was approved in the initial setup. Multiple accounts are supported by EasyMount, but each account must be eligible to authorize the Google OAuth client. Testing is not the final distribution model.

Public materials prepared:
- Homepage: https://trtqt1203.github.io/EasyMount/
- Privacy: https://trtqt1203.github.io/EasyMount/privacy.html
- Source: https://github.com/trtqt1203/EasyMount

Google domain ownership/branding configuration and restricted-scope verification are still required; publication of these pages alone does not constitute Google approval.
