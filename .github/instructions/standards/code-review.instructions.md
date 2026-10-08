---
applyTo: "**/*.cs,**/*.ts,**/*.tsx,**/*.js,**/*.jsx"
description: "Compact AI-enforceable pre-PR review limits. Full source: standards/code-review-standards.md."
---

# Code Review Rules (compact)

Full source of truth: `standards/code-review-standards.md` (rule ids, severities, hard limits).
Read that file only when an exact rule or detail not listed here is required. Writing code that
already meets these limits avoids `/code-review` findings.

- Methods ≤ 30 lines, files ≤ 300 lines; cyclomatic complexity ≤ 10; nesting ≤ 3 levels; prefer
  guard clauses and early returns.
- ≤ 5 parameters per method (DI constructors excluded); otherwise use a request object; no boolean
  flag parameters that switch behavior.
- No duplicated logic: reuse existing helpers; no redundant `else`, always-true checks, or unused
  members.
- Null handling: check nulls and bounds at trust boundaries only; return empty collections, never `null`.
  .NET: `is null` / `is not null`; `string.IsNullOrWhiteSpace` for input; `ThrowIfNull` guards;
  `?.` / `??`. TS/JS: `?.`, `??`, and `===` only.
- No dead or commented-out code, unexplained suppressed warnings, magic values, debug output, or
  `TODO` without a work item.
- Handle failure paths explicitly; never swallow exceptions; catch specific types; .NET re-throw
  with `throw;`.
- Dispose resources; no `async void` or blocking on async; no remote/DB calls inside loops.
- No string-built SQL/commands, no secrets or tokens in code, logs, URLs, or client storage;
  validate input at trust boundaries; auth cookies `Secure`, `HttpOnly`, `SameSite`.
- Tests: meaningful assertion, Arrange-Act-Assert, one behavior, descriptive name, deterministic.
- React: components ≤ 150 lines including JSX; hooks only at the top level; complete dependency
  arrays; stable unique `key` on list items (not the index for changing lists); clean up
  subscriptions, timers, listeners, and requests in effects; never mutate state; functional
  updates when using previous state; no `any` in props/state/API types; no prop drilling through
  2+ components; no `dangerouslySetInnerHTML` with unsanitized data; component tests query by
  role/label/text, not internals or snapshots alone.
