---
applyTo: "**"
description: "Generic, technology-agnostic secure development requirements."
---

# Security Instructions

These rules apply to `developer`, `bug-fixer`, and `test-author`. They are intentionally
generic (OWASP-aligned) since no project-specific security standard is available yet. Extend
this file once real project/company security standards are discovered by
`/setup-repo-context` (recorded in `.sdlc/context/standards-summary.md`).

## Core Rules

1. **Validate all input at trust boundaries** (API inputs, file uploads, external service
   responses, user-provided data) regardless of language/framework.
2. **Never hardcode secrets, credentials, tokens, or keys.** Use the project's existing
   configuration/secret-management mechanism; if none is discoverable, flag this as a gap in
   `story-context.md` rather than inventing one.
3. **Avoid injection vulnerabilities** (SQL/NoSQL injection, command injection, template
   injection, XSS) by using parameterized queries/APIs and proper output encoding consistent
   with the existing codebase's approach.
4. **Enforce least privilege** in any new access control, permission, or role logic
   introduced by the change.
5. **Do not log sensitive data** (PII, credentials, tokens, secrets) introduced or touched by
   the change.
6. **Apply secure defaults** for any new configuration (fail closed, not open).
7. **Dependency changes must be deliberate.** Do not add new third-party dependencies unless
   required by the implementation plan; note any added dependency in `changes.md`.
8. **Error handling must not leak internals** (stack traces, internal paths, system details)
   in user-facing output.
9. **Carry security requirements into implementation artifacts.** Record any story-specific
   security constraints or unresolved policy gaps in `story-context.md`,
   `implementation-plan.md`, or `changes.md` so downstream manual processes can verify them.
10. **Flag, don't guess.** If a security-relevant decision depends on unavailable
    project-specific policy, mark it `TO_BE_CONFIGURED` rather than assuming a rule.
