---
name: address-review-comments
description: "PR/review SDLC stage. Fix the open L0 or L1 review findings recorded in work.json with the smallest correct production/test changes (build only, never runs tests), and runs as an isolated forked skill. After fixes it always recommends /unit-testing; fixed code re-enters review from L0."
argument-hint: "<WORK-ID> <source: l0|l1>"
user-invocable: true
disable-model-invocation: true
context: fork
---

# Address Review Comments

Complete authoritative procedure for the `/address-review-comments` SDLC stage. Developers
invoke this skill directly; there is no prompt wrapper or custom agent.

## Purpose
Fix the recorded review findings from one source — `l0` or `l1` — with the smallest correct
changes. It is the single skill that turns review feedback into code changes; verification
still happens in `/unit-testing`, and any changed code re-enters review from L0.

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
Review-fix engineer: address every open review finding with the smallest correct change.
- May make **local** production and test-code changes required to address the findings. **Git stays read-only** — do not commit, push, branch, open/approve/merge a PR, or
  change Git configuration.
- Do **not** run or author the unit-testing stage here (that is `/unit-testing`); running the
  narrowest build/compile check for the tech stack is allowed to confirm the fix compiles.
- Permitted capabilities: `read`, `search`, `edit` (production source, test files, and `.sdlc`
  artifacts), and `execute` limited to the build/compile/type-check command and read-only diff.
- Keep each fix in the smallest appropriate scope; no unrelated refactoring.

## Required Input
- Work ID
- Source: `l0` or `l1`

Findings are read from `work.json` → `review.<source>.findings`; do not ask the user for them.

## Prerequisites
- `.sdlc/work/<ID>/work.json`, `plan.md`, and `log.md` exist. If missing, STOP and recommend
  `/analyze-story`.
- `work.json` → `review.<source>.findings` must contain at least one `OPEN` finding (and
  `review.<source>.status` = `CHANGES_REQUIRED`). If there are none, STOP with
  `BLOCKED_MISSING_INFORMATION` and recommend the appropriate next review command.

- **Loop limit guard:** if `work.json` → `review.cycle` ≥ `review.max_review_cycles` (3), do not
  fix anything. STOP with `WAITING_FOR_HUMAN`, summarize the still-open findings, and
  recommend `None — escalated to developer`. Only a human may reset `review.cycle` to 0.

## Shared Rules
Follow the shared pipeline rules in `.github/copilot-instructions.md` (Read Order, Context
Rules, Effort Dial, Stage Outputs, Story Flow and Test → Fix Loop, Repository Modes,
exclusions) and the applicable `.github/instructions/*.instructions.md`. Do not duplicate those
rules here.

## Procedure
1. Apply the Effort Dial, Read Order, and Context Rules.
2. Read `work.json` (scope, `review`, `pr`, `test_fix_loop`), the latest `log.md` entries, and
   `plan.md` only for boundaries.
3. For every `OPEN` finding in `review.<source>.findings`:
   - Apply the smallest correct change at the right scope, using the exact files from the
     finding location and `work.json`. Stay within the approved story scope; no unrelated
     refactoring.
   - Mark the finding `status: RESOLVED` and record any newly touched files in `work.json`.
4. Run only the narrowest `build_commands` to confirm it compiles (`NOT_CONFIGURED` if no build
   step). Do not run unit tests here. If the build fails, fix it before stopping; if it still
   fails, STOP with `STAGE_FAILED`.
5. Set the routing state in `work.json`: `review.return_after_testing: true`,
   `review.origin: <source>`, and increment `review.cycle` by 1. Leave
   `review.<source>.status` as `CHANGES_REQUIRED`; the next `/l0-review` / `/l1-review` run
   re-evaluates it.
6. Update `.sdlc/work/<ID>/log.md`: set the Current Stage to `ADDRESS_REVIEW_<SOURCE>` and
   append an entry with the resolved finding ids, changed files, build result,
   `Unit tests: NOT_RUN (verified in /unit-testing)`, and the next recommended command. Do not
   change the Test → Fix Loop counter (that is owned by `/unit-testing` / `/fix-bugs`).
7. Update `work.json` (and `plan.md` boundaries) only if the actual touched files changed.

## Review Re-entry Rule
Any code change made for L0 or L1 findings must re-run `/unit-testing`, then the developer
updates the SAME PR, and review restarts from `/l0-review` → `/l1-review` — never jump straight
back only to the level that raised the finding. The `review.return_after_testing: true` flag
set here tells `/unit-testing` to route to the update-PR → `/l0-review` path instead of
`/prepare-pr`.

## Cost-Control Behavior
- Read the recorded findings first; do not re-derive the review or rescan the repository.
- Fix the smallest slice from the finding location; reuse `work.json` exact files.
- Run only the narrowest build; never run unit tests here.
- Use `selected_standards` + compact instruction files; read a full `standards/*.md` only for
  an exact rule.
- Before any repository-wide search, honor `.sdlc/context/manifest.json` → `exclusions` (see
  `.github/copilot-instructions.md`).

## Outputs
- Production and/or test-code fixes for the open findings (no test *execution*)
- `work.json` → `review.<source>` (finding statuses), `review.return_after_testing`,
  `review.origin`, `review.cycle`
- `.sdlc/work/<ID>/log.md`

## Stop Condition
STOP after the fixes are applied/built. Do not invoke `/unit-testing` or any review skill; only
recommend the next command.

Fixes applied:
```
CURRENT STAGE: Address Review Comments (<source>)
STATUS: STAGE_PASSED
FILES CREATED/UPDATED: <list>, work.json, log.md
SUMMARY: Addressed <source> findings: <n> fixed; review cycle <cycle>/3; Build: <command → result | NOT_CONFIGURED>; Unit tests: not run in this stage
NEXT RECOMMENDED COMMAND: /unit-testing <ID> current_story
```

Build failed:
```
CURRENT STAGE: Address Review Comments (<source>)
STATUS: STAGE_FAILED
FILES CREATED/UPDATED: <list>, log.md
SUMMARY: Build failed after fixes: <error summary>
NEXT RECOMMENDED COMMAND: /address-review-comments <ID> <source> — after the build error is addressed
```

Cycle limit reached (`review.cycle` = 3):
```
CURRENT STAGE: Address Review Comments (<source>)
STATUS: WAITING_FOR_HUMAN
FILES CREATED/UPDATED: None
SUMMARY: Review cycle limit (3) reached; open findings: <ids>
NEXT RECOMMENDED COMMAND: None — escalated to developer
```

No open findings / missing cache:
```
CURRENT STAGE: Address Review Comments (<source>)
STATUS: BLOCKED_MISSING_INFORMATION
FILES CREATED/UPDATED: None
SUMMARY: <no open <source> findings | work cache missing>
NEXT RECOMMENDED COMMAND: /l0-review <ID> or /l1-review <ID> (matching the source) | /analyze-story <ID> when the cache is missing
```
