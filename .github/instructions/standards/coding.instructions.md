---
applyTo: "**/*.cs,**/*.ts,**/*.tsx,**/*.js,**/*.jsx,**/*.sql"
description: "Compact AI-enforceable global coding rules. Full source: standards/coding-standards.md."
---

# Global Coding Rules (compact)

Full source of truth: `standards/coding-standards.md`. Read that file only when an exact rule or
detail not listed here is required. Review limits and quality checks are in
`code-review.instructions.md`.

- Keep changes aligned with existing project patterns and approved technologies.
- Use the smallest appropriate visibility; keep boolean expressions readable.
- Comment only to explain intent, tradeoffs, or follow-up.
- Keep concurrency-sensitive code thread-safe and free from race conditions.
- Tests: cover core, edge, and CRUD scenarios where relevant.
