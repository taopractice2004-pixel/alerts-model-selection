---
name: address-review-comments
description: "PR/review SDLC stage. Resolve L0, L1, or L2 review comments after an AI review (L0/L1) or a human L2 client review. Classifies every item as IN_SCOPE_TECHNICAL_FIX, REQUIREMENT_OR_SCOPE_CHANGE, or NEEDS_HUMAN_CLARIFICATION, applies the smallest in-scope production/test fixes (never tests), routes requirement changes back to /analyze-story, and runs as an isolated forked skill. After fixes it always recommends /unit-testing; any reviewed code re-enters review from L0."
argument-hint: "<WORK-ID> <source: l0|l1|l2> [L2 review comments when source is l2]"
user-invocable: true
disable-model-invocation: true
context: fork
---

# Address Review Comments

Complete authoritative procedure for the `/address-review-comments` SDLC stage. Developers
invoke this skill directly; there is no prompt wrapper or custom agent.

## Purpose
Resolve recorded review findings from one source — `l0`, `l1`, or `l2` — by classifying each
item and applying only the smallest in-scope fixes. It is the single skill that turns review
feedback into code changes; verification still happens in `/unit-testing`, and any changed code
re-enters review from L0.

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
Review-fix engineer: address approved, in-scope review findings with the smallest correct
change, and refuse to silently absorb requirement changes or guesses.
- May make **local** production and test-code changes required to address approved in-scope
  findings. **Git stays read-only** — do not commit, push, branch, open/approve/merge a PR, or
  change Git configuration.
- Do **not** run or author the unit-testing stage here (that is `/unit-testing`); running the
  narrowest build/compile check for the tech stack is allowed to confirm the fix compiles.
- Permitted capabilities: `read`, `search`, `edit` (production source, test files, and `.sdlc`
  artifacts), and `execute` limited to the build/compile/type-check command and read-only diff.
- Keep each fix in the smallest appropriate scope; no unrelated refactoring.

## Required Input
- Work ID
- Source: `l0`, `l1`, or `l2`
- For `l2` only: the client review comments (the developer pastes/provides them when invoking
  the skill). If the source is `l2` and no comments are provided, ask for them; STOP with
  `BLOCKED_MISSING_INFORMATION` only if they are still not supplied.

For `l0` / `l1`, the findings are read from `work.json` → `review.<source>.findings`; do not ask
the user for them.

## Prerequisites
- `.sdlc/work/<ID>/work.json`, `plan.md`, and `log.md` exist. If missing, STOP and recommend
  `/analyze-story`.
- For `l0` / `l1`: `work.json` → `review.<source>.findings` must contain at least one `OPEN`
  finding (and `review.<source>.status` = `CHANGES_REQUIRED`). If there are none, STOP with
  `BLOCKED_MISSING_INFORMATION` and recommend the appropriate next review command.

- **Loop limit guard:** if `work.json` → `review.cycle` ≥ `review.max_review_cycles` (3), do not
  fix anything. STOP with `WAITING_FOR_HUMAN`, summarize the still-open findings/comments, and
  recommend `None — escalated to developer`. Only a human may reset `review.cycle` to 0.

## Shared Rules
Follow the shared pipeline rules in `.github/copilot-instructions.md` (Read Order, Context
Rules, Effort Dial, Stage Outputs, Story Flow and Test → Fix Loop, Repository Modes,
exclusions) and the applicable `.github/instructions/*.instructions.md`. Do not duplicate those
rules here.

## Classification (every item)
Before changing anything, classify each finding/comment as exactly one of:
1. **IN_SCOPE_TECHNICAL_FIX** — a fix within the already-approved behavior (coding-quality or
   architecture correction within approved behavior, duplication cleanup, null/error handling,
   review-driven refactor, test correction, or a DB/API implementation correction that does not
   alter approved requirements).
2. **REQUIREMENT_OR_SCOPE_CHANGE** — the item changes requirements or scope.
3. **NEEDS_HUMAN_CLARIFICATION** — the item is ambiguous and cannot be safely resolved.

Record the chosen `classification` on each finding/comment in `work.json`.

## Procedure
1. Apply the Effort Dial, Read Order, and Context Rules.
2. Read `work.json` (scope, `review`, `pr`, `test_fix_loop`), the latest `log.md` entries, and
   `plan.md` only for boundaries. For `l2`, record each provided comment in `work.json` →
   `review.l2.comments` (`id` = `<WORK-ID>-L2-C<n>`, `text`, `status: OPEN`) and set
   `review.l2.status: COMMENTS`.
3. Classify every item (see Classification). Then handle by class:

   **IN_SCOPE_TECHNICAL_FIX**
   - Apply the smallest correct change at the right scope, using the exact files from the
     finding location and `work.json`.
   - Run only the narrowest `build_commands` to confirm it compiles (`NOT_CONFIGURED` if no
     build step). Do not run unit tests here.
   - Mark the finding/comment `status: RESOLVED`, and record the changed files in `work.json`.
   - When all actionable items of this source are resolved, set
     `review.<source>.status: PASS` (for `l2`, `review.l2.status: APPROVED` only if the client
     comments are fully addressed — otherwise leave `COMMENTS`).
   - Set the review-fix routing state: `review.return_after_testing: true`,
     `review.origin: <source>`, and increment `review.cycle` by 1.

   **REQUIREMENT_OR_SCOPE_CHANGE**
   - Do **not** silently implement it as a review fix. Mark the finding/comment
     `status: DEFERRED` with `classification: REQUIREMENT_OR_SCOPE_CHANGE`, and set
     `review.origin: <source>`.
   - Flag `REANALYSIS_REQUIRED` in the log and route back to `/analyze-story <ID>`; the normal
     flow (analyze → human approval → implement → unit testing) then applies, and the developer
     later updates the SAME PR so review restarts from L0.

   **NEEDS_HUMAN_CLARIFICATION**
   - Do not guess and do not change code. Mark the item `status: OPEN` with
     `classification: NEEDS_HUMAN_CLARIFICATION` and state the unresolved question clearly in
     the outcome and log.
4. Do not change production behavior for items that are not IN_SCOPE_TECHNICAL_FIX.
5. Update `.sdlc/work/<ID>/log.md`: set the Current Stage to `ADDRESS_REVIEW_<SOURCE>` and
   append an entry with the per-item classification, changed files, build result,
   `Unit tests: NOT_RUN (verified in /unit-testing)`, and the next recommended command. Do not
   change the Test → Fix Loop counter (that is owned by `/unit-testing` / `/fix-bugs`).
6. Update `work.json` (and `plan.md` boundaries) only if the actual touched files changed.

## Review Re-entry Rule
Any production-code change made for L0, L1, or L2 comments must re-run `/unit-testing`, then the
developer updates the SAME PR, and review restarts from `/l0-review` → `/l1-review` → L2 — never
jump straight back only to the level that raised the comment. This guarantees modified code is
re-checked through all lower review levels. The `review.return_after_testing: true` flag set
here is what tells `/unit-testing` to route to the update-PR → `/l0-review` path instead of
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
- Production and/or test-code fixes for IN_SCOPE_TECHNICAL_FIX items (no test *execution*)
- `work.json` → `review.<source>` (finding/comment statuses, classifications),
  `review.return_after_testing`, `review.origin`, `review.cycle`
- `.sdlc/work/<ID>/log.md`

## Stop Condition
STOP after the findings are classified and in-scope fixes are applied/built. Do not invoke
`/unit-testing` or any review skill; only recommend the next command.

In-scope fixes applied:
```
CURRENT STAGE: Address Review Comments (<source>)
STATUS: STAGE_PASSED
FILES CREATED/UPDATED: <list>, work.json, log.md
SUMMARY: Addressed <source> findings: <n> in-scope fixes applied; Build: <command → result | NOT_CONFIGURED>; Unit tests: not run in this stage
NEXT RECOMMENDED COMMAND: /unit-testing <ID> current_story
```

Requirement/scope change found:
```
CURRENT STAGE: Address Review Comments (<source>)
STATUS: WAITING_FOR_HUMAN
FILES CREATED/UPDATED: work.json, log.md
SUMMARY: REANALYSIS_REQUIRED — <item(s)> change requirements/scope; not implemented as a review fix
NEXT RECOMMENDED COMMAND: /analyze-story <ID>
```

Clarification needed:
```
CURRENT STAGE: Address Review Comments (<source>)
STATUS: WAITING_FOR_HUMAN
FILES CREATED/UPDATED: work.json, log.md
SUMMARY: NEEDS_HUMAN_CLARIFICATION — <unresolved question(s)>; no code changed
NEXT RECOMMENDED COMMAND: None — HUMAN ACTION (provide clarification), then /address-review-comments <ID> <source>
```

Missing inputs / no open findings:
```
CURRENT STAGE: Address Review Comments (<source>)
STATUS: BLOCKED_MISSING_INFORMATION
FILES CREATED/UPDATED: None
SUMMARY: <missing L2 comments | no open L0/L1 findings | work cache missing>
NEXT RECOMMENDED COMMAND: /address-review-comments <ID> l2 <comments> — when L2 comments are supplied | the relevant /l0-review|/l1-review <ID> when there are no open findings
```
