# EasyMount short website address

This Pages advanced-mode Worker serves the existing public EasyMount site and
release metadata through a short Cloudflare address. The Sites deployment remains
the origin; this is not a full hosting migration. It needs no Cloudflare API key
at runtime, database, cloud tokens or Node.js compatibility flag.

GET/HEAD and a small public rendering/cache-header allowlist are forwarded.
The existing POST /api/pool-preview contract is also forwarded, with a bounded
32 KiB body; other POST paths remain disallowed. This preserves the public API
without restoring the removed simulator UI.
Cookies and Authorization are never forwarded; upstream cookies are stripped.
Desktop file transfers remain direct to each cloud provider.

Upload a ZIP containing `_worker.js` at the root to a dedicated Cloudflare Pages
project. Do not upload this directory's README or any user configuration. Verify
the home page, guide, privacy, scripts/styles and `/api/releases` after deployment.

A pages.dev address does not grant DNS control over its parent domain or complete
Google OAuth verification. DNS ownership remains a separate release requirement.
