# Coding Standards

## Purpose

This checklist is intended for pull requests and peer reviews across the development team.

## Review Basics

- Keep changes aligned with existing project patterns and approved technologies.
- Avoid hardcoded values; prefer configuration, constants, or shared message resources.
- Remove dead code, unused imports/usings, commented-out blocks, and suppressed warnings unless there is a documented reason.
- Keep naming simple, consistent, and scoped appropriately.
- Add comments only when they explain intent, tradeoffs, or future follow-up.

## Code Quality

- Use the smallest appropriate visibility for classes, methods, and fields.
- Prefer reusable abstractions over duplicated logic.
- Keep boolean expressions readable and avoid deeply nested control flow.
- Validate nulls, bounds, and failure paths explicitly.
- Ensure concurrency-sensitive code is thread-safe and free from race conditions.

## Reliability And Security

- Handle exceptions deliberately; do not swallow them.
- Release disposable resources correctly, especially for files, streams, network calls, and database connections.
- Do not log secrets or expose sensitive values in stack traces.
- Defend against common risks such as SQL injection, XSS, token exposure, and insecure cookies.

## Testing Expectations

- Cover core scenarios, edge cases, and CRUD behavior where relevant.
- Keep tests readable, deterministic, and focused on one behavior at a time.
- Follow AAA style and use clear naming for test methods.
- Maintain at least 90% coverage for new or changed code when that project rule applies.

## Pull Request Expectations

- Use a short title that includes the work item or JIRA reference.
- Describe what changed, how it was implemented, and why the chosen approach was used.
- Keep PRs small enough for effective review.
- Resolve merge conflicts before requesting review.
- Exclude generated files unless they are intentionally part of the deliverable.
- Add screenshots, design links, or context documents when they help reviewers validate the change.

## Reviewer Expectations

- Start reviews promptly.
- Give precise, constructive feedback with reasons.
- Verify build health, static analysis results, and functional behavior.
- Test realistic and edge-case flows instead of assuming the code works.
- Enforce standards consistently, not selectively.