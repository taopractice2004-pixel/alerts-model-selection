---
name: l1-review
description: "PR/review SDLC stage. Perform the L1 higher-level engineering/design review after L0 passed. Read-only: judges requirement correctness against the approved plan, architecture/layer placement, design and maintainability, API/data/config design when relevant, and test-strategy quality, records findings in work.json, and runs as an isolated forked skill. Does not edit code, does not re-run unit testing, and does not repeat the code-level checks owned by /l0-review. On PASS the work goes to the human-only L2 client review."
argument-hint: "<WORK-ID>"
user-invocable: true
disable-model-invocation: true
context: fork
---

# L1 Review

Complete authoritative procedure for the `/l1-review` SDLC stage. Developers invoke this skill
directly; there is no prompt wrapper or custom agent.

## Purpose
Answer one question: **"Even if the code is technically clean, is this the correct engineering
solution?"** L1 is the higher-level engineering/design review and runs only after `/l0-review`
passed. It must **not** repeat the code-level checks owned by `/l0-review`.

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
Engineering reviewer (L1): judge whether the verified, clean code is the right solution.
- **Read-only review.** Do **not** edit production or test code, and do **not** perform Git
  actions. Fixes are applied later by `/address-review-comments`.
- Do not re-run the unit-testing stage; reuse `/unit-testing` results. Evaluate test *strategy*
  quality, not test execution.
- Do not repeat L0's formatting/naming/basic static checks unless they expose a design issue.
- Permitted capabilities: `read`, `search`, `edit` (`.sdlc` artifacts only), and `execute`
  limited to read-only diff inspection.
- Use the approved `plan.md` and only the standards relevant to the change.

## Required Input
Work ID. If the only in-progress work item is not inferable, ask for the Work ID.

## Prerequisites
- `.sdlc/work/<ID>/work.json`, `plan.md`, `log.md`, and `pr.md` exist. If missing, STOP and
  recommend `/prepare-pr <ID>`.
- `work.json` → `review.l0.status` must be `PASS`. If L0 has not passed, STOP with
  `BLOCKED_MISSING_INFORMATION` and recommend `/l0-review <ID>` (or
  `/address-review-comments <ID> l0` if L0 findings are open).

## Shared Rules
Follow the shared pipeline rules in `.github/copilot-instructions.md` (Read Order, Context
Rules, Effort Dial, Stage Outputs, Repository Modes, exclusions) and the applicable
`.github/instructions/*.instructions.md`. Do not duplicate those rules here.

## Review Scope (L1 only)
Review **only** these areas:
- **A. Requirement implementation** — implementation matches the approved `plan.md`; acceptance
  criteria are correctly represented; behavior is semantically correct; no criterion is
  superficially satisfied while behavior is wrong.
- **B. Architecture** — correct responsibility/layer placement
  (controller/service/repository/domain where applicable), dependency direction, existing
  architecture patterns, unnecessary coupling.
- **C. Design & maintainability** — appropriate abstractions, readability, extensibility; avoid
  over-engineering and fragile shortcuts.
- **D. API / Data / Configuration design when relevant** — API contracts and compatibility,
  DB/migration correctness, config/environment implications, data-access design.
- **E. Testing strategy quality** — meaningful scenarios and important edge cases are covered;
  tests validate behavior rather than merely becoming green.

Do **not** re-review naming/formatting, basic static warnings, secrets scanning, or other
code-level items owned by `/l0-review` unless they expose a genuine design problem.

## Procedure
1. Apply the Effort Dial, Read Order, and Context Rules.
2. Read `work.json` (scope, acceptance criteria, `review`), `plan.md` (intent, strategy,
   boundaries, risks), the latest `log.md` entries, and `pr.md`.
3. Determine the changed/relevant file set from a read-only diff (or the cached file lists).
4. Review against A–E above, comparing the implementation to the approved plan and the
   repository's existing architecture/patterns.
5. Record each finding in `work.json` → `review.l1.findings` with: unique `id`
   (`<WORK-ID>-L1-F<n>`), `severity`, `location`, `description`, `reason`, `suggestion`, and
   `status: OPEN`. Leave `classification` empty — `/address-review-comments` classifies.
6. Decide the L1 result:
   - **PASS** only when there are no findings that require changes before progressing to the
     human-only L2 client review.
   - **CHANGES_REQUIRED** when at least one finding needs a change.
7. Set `work.json` → `review.l1.status` (`PASS` or `CHANGES_REQUIRED`). Do not modify
   `test_fix_loop`, `pr`, `review.l0`, or `review.l2`.
8. Update `.sdlc/work/<ID>/log.md`: set the Current Stage to `L1_REVIEW` and append an entry
   with the result, finding ids with severities, and the next recommended command/action. Do
   not change the Test → Fix Loop counter.

## Cost-Control Behavior
- Review only changed/relevant files; reuse `plan.md` instead of re-deriving requirements.
- Reuse `/unit-testing` results; do not re-run tests or builds.
- Use `selected_standards` + compact instruction files; read a full `standards/*.md` only for
  an exact rule.
- Before any repository-wide search, honor `.sdlc/context/manifest.json` → `exclusions` (see
  `.github/copilot-instructions.md`).

## Outputs
- `work.json` → `review.l1`
- `.sdlc/work/<ID>/log.md`

## Stop Condition
STOP after recording the L1 result. L1 is read-only; do not edit code or invoke another skill.
There is no `/l2-review` AI skill — L2 is a human-only client review.

L1 PASS:
```
CURRENT STAGE: L1 Review
STATUS: WAITING_FOR_HUMAN
FILES CREATED/UPDATED: work.json, log.md
SUMMARY: L1 PASS — engineering/design review clean; ready for L2 client review
HUMAN ACTION: Send the PR for L2 client review (human-only)
NEXT RECOMMENDED COMMAND: None — HUMAN ACTION (L2 client review). If L2 raises comments, run /address-review-comments <ID> l2 <comments>
```

L1 findings require changes:
```
CURRENT STAGE: L1 Review
STATUS: CHANGES_REQUIRED
FILES CREATED/UPDATED: work.json, log.md
SUMMARY: <n> L1 findings recorded (<by severity>); details in work.json → review.l1.findings
HUMAN ACTION: None
NEXT RECOMMENDED COMMAND: /address-review-comments <ID> l1   (if review.cycle = review.max_review_cycles (3): None — escalated to developer, STATUS WAITING_FOR_HUMAN)
```

L0 not passed / missing cache:
```
CURRENT STAGE: L1 Review
STATUS: BLOCKED_MISSING_INFORMATION
FILES CREATED/UPDATED: None
SUMMARY: <L0 not PASS / pr.md missing / work cache missing>
NEXT RECOMMENDED COMMAND: /l0-review <ID> — run or re-run L0 first
```
