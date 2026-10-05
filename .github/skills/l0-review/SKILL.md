---
name: l0-review
description: "PR/review SDLC stage. Perform the L0 code-level quality, standards, and security review of the changes on the open PR after the developer created it. Read-only: reviews scope discipline, coding/repository standards, code quality, security, and already-configured static findings, records findings in work.json, and runs as an isolated forked skill. Does not edit code, does not re-run unit testing, and does not cover the engineering/design concerns owned by /l1-review."
argument-hint: "<WORK-ID>"
user-invocable: true
disable-model-invocation: true
context: fork
---

# L0 Review

Complete authoritative procedure for the `/l0-review` SDLC stage. Developers invoke this skill
directly; there is no prompt wrapper or custom agent.

## Purpose
Answer one question: **"Is this code technically clean, safe and compliant?"** L0 is a
code-level quality / standards / security review of the changes on the open PR. It is the lower
of the two AI review gates and must **not** duplicate the engineering/design review owned by
`/l1-review`.

## Execution Context
- `context: fork` — this skill runs as an isolated subagent in its own context window. The
  parent Copilot agent only invokes it and receives its final stage result.
- This SKILL.md is the complete role and procedure. Do not create, delegate to, or invoke any
  custom agent or other skill from inside this stage.
- Do not rely on prior conversation context. Cross-stage state comes only from persistent
  `.sdlc/` artifacts (`.sdlc/context/*`, `.sdlc/work/<ID>/*`), with `work.json` as the
  authoritative scope.
- Write every required `.sdlc` artifact before returning; nothing else survives the fork.
- Return only the concise outcome block from the Stop Condition, then STOP for human review.

## Role
Code reviewer (L0): inspect the changed/relevant files only and judge technical cleanliness.
- **Read-only review.** Do **not** edit production or test code, and do **not** perform Git
  actions. Fixes are applied later by `/address-review-comments`.
- Do not re-run the unit-testing stage; reuse the results recorded by `/unit-testing`. Running
  an already-configured linter/static-analyzer in read-only mode on the changed files is
  allowed only if it does not modify files.
- Do not repeat the requirements/architecture/design review that belongs to `/l1-review`.
- Permitted capabilities: `read`, `search`, `edit` (`.sdlc` artifacts only), and `execute`
  limited to read-only diff and already-configured non-mutating static-analysis commands.
- Use only the standards relevant to the changed files; do not reload every standards source.

## Required Input
Work ID. If the only in-progress work item is not inferable, ask for the Work ID.

## Prerequisites
- `.sdlc/work/<ID>/work.json`, `plan.md`, `log.md`, and `pr.md` exist. If `pr.md` or the cache
  is missing, STOP and recommend `/prepare-pr <ID>`.
- `work.json` → `pr.prepared` must be `true` (a PR draft exists; the developer has created the
  PR). If not, STOP with `BLOCKED_MISSING_INFORMATION` and recommend `/prepare-pr <ID>`.

## Shared Rules
Follow the shared pipeline rules in `.github/copilot-instructions.md` (Read Order, Context
Rules, Effort Dial, Stage Outputs, Repository Modes, exclusions) and the applicable
`.github/instructions/*.instructions.md`. Do not duplicate those rules here.

## Review Scope (L0 only)
Review **only** these areas on the changed/relevant files:
- **A. Scope discipline** — changed files match the approved scope in `work.json`; no unrelated
  changes or refactoring; no accidental generated / vendor / protected-file changes.
- **B. Coding / repository standards** — naming, conventions, the applicable compact standards
  / instructions, and existing repository coding patterns.
- **C. Code quality** — dead/unnecessary code, obvious duplication, oversized/over-complex
  units, error handling, logging, null handling, obvious reliability problems.
- **D. Security** — secrets, unsafe input/data handling, authn/authz mistakes where relevant,
  obvious security-sensitive patterns.
- **E. Static / code-level checks** — compiler/linter/static-analysis findings where already
  configured and appropriate.

Do **not** review requirement correctness, architecture/layer placement, design/abstraction
quality, API/data-contract design, or test-strategy quality — those belong to `/l1-review`.

## Procedure
1. Apply the Effort Dial, Read Order, and Context Rules.
2. Read `work.json` (scope, exact files, `selected_standards`, `pr`, `review`), the latest
   `log.md` entries, and `pr.md`. Read `plan.md` only for boundaries not in `work.json`.
3. Determine the changed/relevant file set from a read-only diff (or `work.json` →
   `exact_source_files` / `exact_test_files` if diff tooling is unavailable).
4. Review only those files against A–E above, using the `selected_standards` ids and the
   compact `.github/instructions/standards/*.instructions.md`; read a full `standards/*.md`
   only for an exact rule.
5. Record each finding in `work.json` → `review.l0.findings` with: unique `id`
   (`<WORK-ID>-L0-F<n>`), `severity` (`BLOCKER` | `HIGH` | `MEDIUM` | `LOW`), `location`
   (file:line or area), `description`, `reason`, `suggestion`, and `status: OPEN`. Leave
   `classification` empty — `/address-review-comments` classifies.
6. Decide the L0 result:
   - **PASS** only when there are no findings that require code changes before progressing.
   - **CHANGES_REQUIRED** when at least one finding needs a code change.
7. Set `work.json` → `review.l0.status` (`PASS` or `CHANGES_REQUIRED`). Do not modify
   `test_fix_loop`, `pr`, or the other review levels.
8. Update `.sdlc/work/<ID>/log.md`: set the Current Stage to `L0_REVIEW` and append an entry
   with the result, finding ids with severities, and the next recommended command. Do not
   change the Test → Fix Loop counter.

## Cost-Control Behavior
- Review only changed/relevant files; never broad-scan the repository or re-review unchanged
  code.
- Reuse `/unit-testing` results; do not re-run the test suite or builds.
- Use `selected_standards` + compact instruction files; read a full `standards/*.md` only for
  an exact rule.
- Before any repository-wide search, honor `.sdlc/context/manifest.json` → `exclusions` (see
  `.github/copilot-instructions.md`).

## Outputs
- `work.json` → `review.l0`
- `.sdlc/work/<ID>/log.md`

## Stop Condition
STOP after recording the L0 result. L0 is read-only; do not edit code or invoke another skill.

L0 PASS:
```
CURRENT STAGE: L0 Review
STATUS: STAGE_PASSED
FILES CREATED/UPDATED: work.json, log.md
SUMMARY: L0 PASS — no code-level findings requiring changes
HUMAN ACTION: None
NEXT RECOMMENDED COMMAND: /l1-review <ID>
```

L0 findings require changes:
```
CURRENT STAGE: L0 Review
STATUS: CHANGES_REQUIRED
FILES CREATED/UPDATED: work.json, log.md
SUMMARY: <n> L0 findings recorded (<by severity>); details in work.json → review.l0.findings
HUMAN ACTION: None
NEXT RECOMMENDED COMMAND: /address-review-comments <ID> l0
```

Missing PR / cache:
```
CURRENT STAGE: L0 Review
STATUS: BLOCKED_MISSING_INFORMATION
FILES CREATED/UPDATED: None
SUMMARY: <pr.md missing / pr.prepared is false / work cache missing>
NEXT RECOMMENDED COMMAND: /prepare-pr <ID>
```
