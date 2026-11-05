# EasyMount website and API

Landing page and shared metadata API for the EasyMount Windows app.

- Stack: React, TypeScript, Vinext, Tailwind 4, shadcn/Radix, Cloudflare Workers.
- Run: `npm run dev -- --port 4173`.
- Check: `npx tsc --noEmit` and `npm run verify:api` while the server runs.
- Build: use the installed Sites `scripts/build-site.mjs` helper.
- Deployment identity: `.openai/hosting.json`; preserve it when updating this site.
- Routes: `/`, `/guide`, `/privacy`.
- API: `/api/health`, `/api/providers`, `/api/releases`, POST `/api/pool-preview`.

The API is stateless. It does not accept cloud credentials or transfer cloud files. Run `node scripts/sync-release.mjs` before a release deployment to update the dated, validated release manifest. No public release is represented as `status: unpublished` instead of an invented download. The desktop `ReleaseClient` uses the release API and validates installer hosts before showing a link. Cloud file operations remain in the local app backend.

See `../docs/UI-BACKEND-040.md` for design decisions, behavior and verification. The Sites starter's `scripts/`, `vendor/` and `sites()` build integration are retained for portable builds and hosting. Local runtime state, credentials, dependencies and generated output are ignored by Git.
