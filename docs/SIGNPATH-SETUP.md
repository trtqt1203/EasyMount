# Free Windows signing: prepared, not activated

The application is at https://signpath.org/apply.html. No application has been
submitted and no paid subscription or certificate has been purchased.

## Before submitting

1. Publish the reviewed signing policy and link it from the homepage and
   download/release page. The README link is prepared locally.
2. Publish a source-matched, explicitly unsigned beta installer. The public
   GitHub Releases API returned no published release on 2026-11-06. Local Setup
   files and a private draft do not satisfy the released-project condition.
3. Identify the maintainer's actual first/last name; use contact email
   travistrtqt@gmail.com only with the maintainer's approval for this application.
4. Review and explicitly accept SignPath's Code of Conduct and personal-data
   processing. Optional marketing consent is unnecessary.
5. Declare the early stage honestly: no independent adoption statistics or
   community reputation have been demonstrated. Acceptance is discretionary.

## After Foundation approval

Enable MFA on GitHub and SignPath. Configure a SignPath GitHub.com trusted build
system with origin verification, GitHub-hosted runners and manual approval for
each signing request. Link only the EasyMount repository. Installing its GitHub
App grants new access and requires the account owner's approval.

Create artifact configurations with slugs `payload`, `msi`, `engine`, `setup`
from `tools/signpath/*.xml`. These are prepared against the documented schema;
server-side validation and certificate policy approval are still required.
The `version` parameter is the assembly ProductVersion for `payload` and the
numeric installer version for the other three configurations.

Create GitHub environment `code-signing`. Configure these environment variables:

- `SIGNPATH_ORGANIZATION_ID`: assigned by SignPath, never invented.
- `SIGNPATH_PROJECT_SLUG`: the actual SignPath project slug.
- `SIGNPATH_SIGNING_POLICY_SLUG`: the approved production policy.
- `SIGNPATH_CERTIFICATE_THUMBPRINT`: the exact approved certificate thumbprint.

Configure environment secrets `SIGNPATH_API_TOKEN` (limited submitter role) and
`OAUTH_CLIENTS_JSON` (desktop application identity matching the distributed
EasyMount build, never user tokens or rclone.conf). Do not paste secrets in chat.

Publish the reviewed source on a version tag matching InformationalVersion,
then manually run `Sign Windows release` from that tag. Approve the four signing
stages in SignPath after inspecting their origin. The workflow verifies the
returned application, MSI, Burn engine and final bundle, then generates SHA256.
It leaves a `verified-signed-installer` artifact for manual release review.
It cannot run successfully until the organization, certificate and credentials
have actually been granted. It never substitutes an unsigned result.

## Verification status

Local validation covers workflow parsing, PowerShell syntax, rejection of an
unsigned binary and the MSI-only packaging stage. A real SignPath request,
trusted signature, GitHub-hosted execution and signed install/uninstall remain
unverified until the Foundation enables the service. Existing installers on the
user's machine remain unsigned.

Sources: https://signpath.org/terms.html,
https://docs.signpath.io/trusted-build-systems/github,
https://docs.signpath.io/artifact-configuration/syntax,
https://docs.firegiant.com/wix/tools/signing/.
