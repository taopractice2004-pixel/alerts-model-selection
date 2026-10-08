# Coding Standards

## Purpose

General engineering principles for all code. Measurable review limits and the pre-PR review
checklist (size, complexity, parameters, duplication, null handling, code smells, error
handling, resources, security, test quality, coverage) live in `code-review-standards.md`.

## General

- Keep changes aligned with existing project patterns and approved technologies.
- Keep naming simple, consistent, and scoped appropriately.
- Add comments only when they explain intent, tradeoffs, or future follow-up.

## Code Quality

- Use the smallest appropriate visibility for classes, methods, and fields.
- Keep boolean expressions readable.
- Ensure concurrency-sensitive code is thread-safe and free from race conditions.

## Testing Expectations

- Cover core scenarios, edge cases, and CRUD behavior where relevant.
