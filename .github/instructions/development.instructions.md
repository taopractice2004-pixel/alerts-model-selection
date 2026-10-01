---
applyTo: "**"
description: "Reusable, technology-agnostic implementation rules for the SDLC framework."
---

# Development Instructions

These rules apply to the `developer`, `bug-fixer`, and `test-author` agents whenever they
modify source code or tests.

## Core Rules

1. **Implement only the requested story scope.** Do not perform unrelated refactoring,
   renames, or "drive-by" cleanups outside `implementation-plan.md` / `impact-map.md`.
2. **Follow existing project architecture and patterns.** Before writing new code, inspect
   `.sdlc/context/repo-profile.md`, `repository-map.md`, and the actual neighboring source
   files for the impacted area. Match existing conventions (naming, layering, error handling
   style, dependency injection patterns, etc.) rather than introducing new ones.
3. **No hardcoded technology assumptions.** This framework must not assume any specific
   language, framework, or runtime. All technology-specific behavior must be derived from
   discovered repository context (Mode C) or explicitly decided in `implementation-plan.md`
   (Mode A/B) — never hardcoded into agents/prompts/instructions.
4. **Minimal, consistent changes.** Prefer the smallest change that correctly satisfies the
   acceptance criteria and fits existing patterns.
5. **New/empty projects (Mode A/B):** implementation may create the necessary initial
   project/application structure, but only as described in the approved
   `implementation-plan.md` — never invent structure beyond what was planned.
6. **Build/compile where appropriate.** If a build step is available (per `repo-profile.md`),
   run it before declaring the stage complete. If not available, record
   `NOT_CONFIGURED` rather than skipping silently without note.
7. **Always update `changes.md` and `session.md`** after making changes.
8. **Never invent project-specific rules.** If a convention is unknown, mark it as
   `TO_BE_DISCOVERED` in the relevant context file and proceed with the most conservative,
   broadly reusable approach, flagging the assumption in `story-context.md`.
9. **Do not reread Jira or full project documentation** during implementation unless the
   cached `story-context.md` / `implementation-plan.md` is insufficient — see
   `.sdlc/framework/context-rules.md`.
