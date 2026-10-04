---
applyTo: "**"
description: "Reusable, technology-agnostic implementation rules for the SDLC framework."
---

# Development Instructions

These rules apply to the `/implement-story`, `/fix-bugs`, and `/unit-testing` skills whenever
they modify source code or tests.

## Core Rules

1. **Implement only the requested story scope.** Do not perform unrelated refactoring,
   renames, or "drive-by" cleanups outside the scope in `work.json` / `plan.md`.
2. **Follow existing project architecture and patterns.** Before writing new code, inspect
   `.sdlc/context/project-profile.md` and the actual neighboring source
   files for the impacted area. Match existing conventions (naming, layering, error handling
   style, dependency injection patterns, etc.) rather than introducing new ones.
3. **No hardcoded technology assumptions.** This framework must not assume any specific
   language, framework, or runtime. All technology-specific behavior must be derived from
   discovered repository context (`EXISTING_PROJECT`) or from the requirements summary and
   explicit decisions in `plan.md` (`NEW_PROJECT`) — never hardcoded into skills/instructions.
4. **Minimal, consistent changes.** Prefer the smallest change that correctly satisfies the
   acceptance criteria and fits existing patterns.
5. **New projects (`NEW_PROJECT`):** implementation may create the necessary initial
   project/application structure, but only as described in the approved
   `plan.md` — never invent structure beyond what was planned.
6. **Build/compile where appropriate.** If a build step is available for the repository's tech
   stack (per `project-profile.md` / `work.json` → `build_commands`), run it before declaring
   the stage complete. If not available, record `NOT_CONFIGURED` rather than skipping silently
   without note.
6a. **Testing belongs to `/unit-testing` only.** `/implement-story` and `/fix-bugs` never write,
   edit, or run unit tests; `/unit-testing` never changes production behavior. Bugs it finds go
   through the bounded test → fix loop (max 3 fix iterations, then a developer investigates).
7. **Always update `log.md`** after making changes (status header plus a new entry).
8. **Never invent project-specific rules.** If a convention is unknown, mark it as
   `TO_BE_DISCOVERED` in the relevant context file and proceed with the most conservative,
   broadly reusable approach, flagging the assumption in `plan.md`.
9. **Do not reread Jira or full project documentation** during implementation unless the
   cached `work.json` / `plan.md` is insufficient — see the Read Order in
   `.github/copilot-instructions.md`.
