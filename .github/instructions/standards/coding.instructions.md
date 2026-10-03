---
applyTo: "**/*.cs,**/*.ts,**/*.tsx,**/*.js,**/*.jsx,**/*.sql"
description: "Compact AI-enforceable global coding rules. Full source: standards/coding-standards.md."
---

# Global Coding Rules (compact)

Full source of truth: `standards/coding-standards.md`. Read that file only when an exact rule or
detail not listed here is required.

- Keep changes aligned with existing project patterns and approved technologies.
- Avoid hardcoded values; prefer configuration, constants, or shared message resources.
- Remove dead code, unused imports/usings, commented-out blocks, and suppressed warnings unless
  documented.
- Use the smallest appropriate visibility; prefer reusable abstractions over duplicated logic.
- Validate nulls, bounds, and failure paths explicitly; keep boolean expressions readable and
  avoid deeply nested control flow.
- Handle exceptions deliberately (do not swallow); release disposable resources correctly.
- Do not log secrets or expose sensitive values; defend against SQL injection, XSS, token
  exposure, and insecure cookies.
- Tests: cover core, edge, and CRUD scenarios; keep them deterministic and single-behavior;
  follow AAA style with clear names; maintain >=90% coverage for new/changed code where the
  project rule applies.
