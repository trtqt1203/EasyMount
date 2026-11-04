# EasyMount development

- Build a small native Windows app around rclone and WinFsp. Reuse the existing rclone configuration and official provider authentication flow.
- New drives default to read and write, as requested by the user. Preserve explicitly saved read-only choices. Never weaken validation, credential handling, pending-upload checks or safe disconnect to simplify code.
- Use argument lists, not shell concatenation, when launching rclone. Do not put tokens, passwords or full rclone config contents in logs or tests.
- AutoConnect means connection at application startup. Windows login startup is a separate feature requiring an explicit user choice.
- Run the project checks and inspect the launched desktop app after meaningful UI changes. A successful build does not establish successful cloud mounting.
- Use the installed REA skill when claims depend on a shipped binary/package or requested runtime evidence: `C:\Users\Administrator\.codex\skills\reverse-engineer-anything\SKILL.md`. Upstream: https://github.com/morluto/rea/tree/c0c3902207d6b1a33709e42b80c7e570da7db964/.agents/skills/reverse-engineer-anything .
- Ordinary source analysis and coding use repository tools directly. Skill installation is distinct from REA MCP registration and analysis-engine installation; preserve that distinction in reports.
- Keep findings traceable to a source or a runnable check. Distinguish observed behavior, implementation choices and unverified behavior. Never claim RaiDrive binary analysis or REA Evidence IDs without actually obtaining them.
- Document limitations and verification in `docs`. Keep user-facing deliverables in this project; do not bundle unrelated upstream repositories or user credentials.
