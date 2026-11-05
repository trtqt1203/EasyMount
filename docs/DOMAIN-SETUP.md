# EasyMount domain setup — 2026-11-05

- Registered domain: easymount.name.vn, active at Tino; expiry shown as 2027-11-05.
- Registrar remains Tino. DNS uses the existing Cloudflare account, Free plan.
- Assigned nameservers: brett.ns.cloudflare.com and hope.ns.cloudflare.com.
  Tino read-back and public DNS confirmed both after the change.
- Zone: 4736a6c42ba158390bf1e9aa6eb04402.
- Original www/webmail CNAME and apex MX records were retained during migration;
  the website's apex and www CNAME now target easymount.pages.dev.
- Both names were added to the existing EasyMount Pages project. Cloudflare
  showed Active / SSL enabled for each. HTTPS GET returned 200 for both home
  URLs, /privacy and /api/releases. All 16 live API/page checks passed on the
  apex host. No TLS validation was bypassed.
- Google Search Console Domain property easymount.name.vn was verified using
  a DNS TXT record with the Google Cloud project-owner account. The UI showed
  ownership verified using the domain provider. Keep this TXT record in DNS.
- Google Auth Platform Branding was saved with homepage
  https://easymount.name.vn/ , privacy https://easymount.name.vn/privacy and
  authorized domain easymount.name.vn. The UI confirmed Branding changes saved.
- No Google Production toggle, restricted-scope verification submission,
  review approval, release publication or code signing was performed.

This supersedes the earlier missing-domain/DNS-ownership blocker in the
release status/audit. Remaining OAuth review and other release gates are
separate; website HTTPS is not Windows installer code signing.

Private/local UI evidence is retained outside the public source tree:
EasyMount-domain-verified.png and EasyMount-domain-configured.png in the
workspace outputs directory. No identity documents or registrar contact
details are included in this report.
