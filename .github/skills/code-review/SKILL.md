---
name: code-review
description: "Primary SDLC stage. Pre-PR code review of the work's changed code (.NET and React / TS / JS) after /unit-testing passes, so recurring L1/L2 review comments are fixed before a pull request is raised. Checks the change against standards/code-review-standards.md (method/component/file size, complexity, parameters, duplication, null handling, smells, error handling, performance, security, React rules, scope, test quality, coverage) and the standards that apply to the changed files, using only analyzers configured in the repository (never machine-installed tools) and manual measurement otherwise. Review only: records findings in work.json, never edits code. Blocking findings drive the bounded review → fix loop (max 2 fix iterations through /fix-bugs, then escalate). Runs as an isolated forked skill."
argument-hint: "<WORK-ID>"
user-invocable: true
disable-model-invocation: true
context: fork
---

# Code Review

Complete authoritative procedure for the `/code-review` SDLC stage. Developers invoke this skill
directly; there is no prompt wrapper or custom agent.

## Purpose
Act as the pre-PR reviewer. After the work is implemented and its unit tests pass, review the
changed code the way L1/L2 reviewers would, so their recurring comments are fixed inside the
pipeline instead of as PR rework. The review checks quality and standards; it does not re-verify
business correctness (that is `/unit-testing`) or redesign architecture (left to human L2).

This stage does not change code. Fixes go through `/fix-bugs`, are re-verified by
`/unit-testing`, and are re-reviewed here (see `.github/copilot-instructions.md` → Review → Fix
Loop).

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
Reviewer: measure and inspect the changed code against the review standard, report precise,
actionable findings with a rule id and severity, and decide the next step of the review → fix
loop.
- **Never edit production code or tests.** Findings go to `work.json` → `code_review.findings`
  for `/fix-bugs`.
- Review only what the work changed (see Review Scope in `standards/code-review-standards.md`);
  do not report untouched code or ask for unrelated refactoring.
- Every finding must name a rule id from `standards/code-review-standards.md` or a standard
  that applies to the file. Never invent a rule or a threshold; if a needed limit is not defined, record it in
  `missing_facts` as `TO_BE_CONFIGURED` instead of reporting a finding.
- Permitted capabilities: `read`, `search`, `edit` limited to `.sdlc/work/<ID>/` artifacts, and
  `execute` limited to the recorded `build_commands`, repository-configured analyzer commands
  (see Analyzers), and read-only version-control commands (for example `git diff`,
  `git status`). Do not install or restore packages or tools, change analyzer configuration, or
  run unit tests.

## Analyzers
The review must give the same result on every developer's machine, so it uses only analyzers the
repository itself defines.
- **Allowed:** analyzers whose configuration is committed in the repository and that run through
  the repository's own commands once its normal dependencies are restored. For example:
  - .NET: analyzers that run during `dotnet build` — the SDK's built-in analyzers configured in a
    committed `.editorconfig`, or analyzer packages referenced in `.csproj` /
    `Directory.Build.props` (for example SonarAnalyzer.CSharp, StyleCop.Analyzers)
  - React / TS / JS: ESLint declared as a `devDependency` in `package.json` with a committed
    config, run through a `package.json` script or the local binary only (`npx --no-install
    eslint …`), so nothing is downloaded
- **Not allowed, even when present on this machine:** globally installed CLIs (global npm
  packages, global `dotnet tool`s, a standalone Sonar scanner), IDE extensions (SonarLint,
  ReSharper, IDE code-metrics windows), or any tool the repository does not declare. Their
  presence varies between developers, so their results are never used, not even as a fallback.
- **Not configured:** if the repository defines no analyzer for a stack, record
  `NOT_CONFIGURED` for that stack in `code_review.analyzer_commands` and measure manually.
- **Configured but cannot run:** if the analyzer command fails because dependencies are not
  restored or the tool errors, do not install anything and do not fall back to a machine tool.
  Record `NOT_RUN: <reason>`, measure manually, and say in `SUMMARY` that the developer can
  restore dependencies and rerun `/code-review` for analyzer-measured results.
- Severity always comes from `standards/code-review-standards.md`, never from the analyzer's own
  configured threshold. Map warnings to rule ids with its Analyzer Mapping table.

## Required Input
- Work ID

The files to review, the standards, and the coverage result all come from the work folder; do not
ask the user for them.

## Prerequisites
- `.sdlc/context/manifest.json` must exist; otherwise STOP and recommend `/setup-repo-context`.
- `.sdlc/work/<ID>/work.json` and `log.md` must exist; otherwise STOP with
  `BLOCKED_MISSING_INFORMATION` (there is no change to review).
- `work.json` → `test_fix_loop.status` must be `TESTS_PASSED`. Code is reviewed only once its
  tests pass, so review fixes are protected by tests. Otherwise STOP with
  `BLOCKED_MISSING_INFORMATION` and recommend `/unit-testing <WORK-ID>`.

## Shared Rules
Follow the shared pipeline rules in `.github/copilot-instructions.md` (Read Order, Context
Rules, Effort Dial, Stage Outputs, Work IDs and Work Folders, Review → Fix Loop, Next Command
Rules, Repository Modes, exclusions). Do not duplicate those rules here.

## Procedure
1. Apply the Effort Dial, Read Order, and Context Rules. Read `work.json` and `log.md`; apply
   the Prerequisites. Read `code_review` to know the `review_fix_iteration` and which findings the
   last `/fix-bugs` marked `FIX_APPLIED`.
2. Load the review rules:
   - `standards/code-review-standards.md` (read in full — it is the checklist for this stage)
   - the compact `.github/instructions/standards/*.instructions.md` files whose `applyTo`
     matches the touched files; read a full `standards/*.md` file only for an exact rule
3. Determine the review scope:
   - files: `exact_source_files[].path` (skip `delete` entries) and `exact_test_files[].path`
   - changed lines: if version control is available, use a read-only diff of those files against
     the base branch or last commit to find the changed hunks and touched methods; if not, review
     the `anchor` of each file entry plus everything in `create` files
   - also list any other changed file the diff shows outside `work.json` (input to CR-SCOPE-01)
   - on a re-review after `/fix-bugs`, re-check the `FIX_APPLIED` findings and the lines the fix
     changed first, then the rest of the changed code only if the fix touched it
4. Run the deterministic pass first:
   - for each stack in the touched files (.NET, React / TS / JS), find the analyzers the
     repository defines, following Analyzers above: `project-profile.md` → Static analysis
     first, then only the committed files (`.editorconfig`, `.csproj` /
     `Directory.Build.props`, `package.json`, ESLint config). Record the commands in
     `code_review.analyzer_commands` per stack, or `NOT_CONFIGURED` / `NOT_RUN: <reason>`.
   - run the narrowest `build_commands` / analyzer commands for the touched files and map their
     warnings on changed lines to rule ids with the Analyzer Mapping table
   - measure what the analyzer does not report, for each touched method or function: body
     lines, cyclomatic complexity, nesting depth, parameter count; for each touched React
     component: total lines including JSX; and for each touched file: line count. Mark these
     findings `measured manually`.
   - read the coverage result of the latest `/unit-testing` entry in `log.md` for CR-COV-01; if it
     is `NOT_CONFIGURED`, or does not cover a changed project, report one MINOR finding naming
     the unmeasured project. Its `suggested_fix` is "rerun `/unit-testing <WORK-ID>`, which adds
     the missing coverage command" — never ask the developer to edit commands
5. Run the judgement pass over the changed lines: duplication and redundant code, null/empty
   handling, smells, error handling, resources/async/performance, security, React rules (for
   React files), scope and layering, test quality, and PR readiness — exactly as listed in
   `standards/code-review-standards.md`.
6. Build each finding:
   - `id` `<WORK-ID>-R<n>` (continue numbering across runs), `rule`, `severity`, `file`, `line`,
     `anchor`, `problem` (one sentence, with the measured value when there is one), and
     `suggested_fix` (one concrete sentence: what to extract, rename, guard, or replace)
   - `auto_fixable: false` when the fix would change a public contract, a signature used by
     `adjacent_dependencies`, behavior covered by an acceptance criterion, or files outside
     `exact_source_files`; otherwise `true`
   - `status: OPEN`
   - apply the pre-existing rule: a method or file that already broke a limit, not made worse by
     this change, is MINOR with `problem` starting `pre-existing:`
7. Reconcile with the previous run:
   - a `FIX_APPLIED` finding that no longer reproduces → `RESOLVED`; one that still reproduces →
     `OPEN` again (note `fix did not hold` in `problem`)
   - compact every `RESOLVED` finding to `{ "id", "rule", "severity", "status" }` (plus
     `decided_via` when present). Its full details are already in `log.md`, so `work.json` stays
     small as review rounds add findings.
   - keep `WAIVED` findings (developer-set, with `waiver_reason`) as they are and do not re-report
     the same rule at the same file and anchor
   - keep `APPROVED_FOR_FIX` findings that are not yet fixed as they are
8. Decide the result. Blocking findings are `BLOCKER` / `MAJOR` with status `OPEN` or
   `APPROVED_FOR_FIX`.
   - no blocking findings → `code_review.status: PASSED`, `STAGE_PASSED`
   - blocking findings and `review_fix_iteration` ≥ `max_review_fix_iterations` (2) →
     `ESCALATED_TO_DEVELOPER`, `WAITING_FOR_HUMAN`
   - any blocking `OPEN` finding with `auto_fixable: false` → `FINDINGS_OPEN`,
     `WAITING_FOR_HUMAN`: the developer decides each such finding by command —
     `/fix-bugs <WORK-ID> approve <ids>` or `/fix-bugs <WORK-ID> waive <ids> "<reason>"`. Name
     the ids and both exact commands in `SUMMARY`; never ask the developer to edit `work.json`.
   - otherwise → `FINDINGS_OPEN`, `STAGE_FAILED`
9. Write `work.json` → `code_review` (`status`, `analyzer_commands`, `findings`). Change nothing
   else in `work.json` except `missing_facts` for an undefined limit.
10. Update `.sdlc/work/<ID>/log.md`: set the status header (Code Review status, Review → Fix Loop
    `<review_fix_iteration>/2 — <status>`, Current Stage `COMPLETE` / `BUG_FIX` /
    `ESCALATED_TO_DEVELOPER` / `CODE_REVIEW` when waiting for decisions) and append an entry with
    the files reviewed, analyzer commands and results, finding counts by severity, one line per
    new finding (`id rule severity file:line — problem`), the ids resolved this run, and the next
    recommended command. These lines are the permanent record once a finding is compacted.

## Outputs
- `work.json` → `code_review` (status, analyzer commands, findings)
- `.sdlc/work/<ID>/log.md`
- No source or test file changes

## Stop Condition
STOP after the findings and loop state are recorded. Do not invoke `/fix-bugs` or any other skill;
recommend the next command. A human reviews and invokes it.

No blocking findings:
```
CURRENT STAGE: Code Review
STATUS: STAGE_PASSED
FILES CREATED/UPDATED: <work.json, log.md>
SUMMARY: Reviewed <n> files; Analyzers: <per stack: command → result | NOT_CONFIGURED | NOT_RUN: reason>; Findings: 0 blocking, <n> MINOR (<ids>); Coverage: <result>
REVIEW → FIX LOOP: <review_fix_iteration>/2 — PASSED
NEXT RECOMMENDED COMMAND: None — ready for PR. To fix MINOR findings before the PR (only while review rounds remain): /fix-bugs <WORK-ID> approve <MINOR ids>
```

Blocking findings, all fixable by the pipeline:
```
CURRENT STAGE: Code Review
STATUS: STAGE_FAILED
FILES CREATED/UPDATED: <work.json, log.md>
SUMMARY: Findings: <b> BLOCKER, <m> MAJOR, <n> MINOR; blocking: <id rule — one line each>; MINOR: <id rule — one line each>
REVIEW → FIX LOOP: <review_fix_iteration>/2 — FINDINGS_OPEN
NEXT RECOMMENDED COMMAND: /fix-bugs <WORK-ID> — add "approve <MINOR ids>" to also fix chosen MINOR findings in the same round
```

Blocking findings that need a developer decision:
```
CURRENT STAGE: Code Review
STATUS: WAITING_FOR_HUMAN
FILES CREATED/UPDATED: <work.json, log.md>
SUMMARY: <ids> need your decision (<one line each: rule, problem, why it changes a contract or behavior>). Approve to let the pipeline fix them, or waive with a reason to keep the code as is.
REVIEW → FIX LOOP: <review_fix_iteration>/2 — FINDINGS_OPEN
NEXT RECOMMENDED COMMAND: /fix-bugs <WORK-ID> approve <ids> | /fix-bugs <WORK-ID> waive <ids> "<reason>"
```

Blocking findings remain after 2 review fix iterations:
```
CURRENT STAGE: Code Review
STATUS: WAITING_FOR_HUMAN
FILES CREATED/UPDATED: <work.json, log.md>
SUMMARY: Blocking findings remain after 2 review fix iterations: <ids>; details in work.json → code_review.findings
REVIEW → FIX LOOP: 2/2 — ESCALATED_TO_DEVELOPER
NEXT RECOMMENDED COMMAND: None — escalated to developer; fix the code yourself or waive with /fix-bugs <WORK-ID> waive <ids> "<reason>", then /unit-testing <WORK-ID> and /code-review <WORK-ID>
```

Prerequisites not met:
```
CURRENT STAGE: Code Review
STATUS: BLOCKED_MISSING_INFORMATION
FILES CREATED/UPDATED: None
SUMMARY: <no work folder | unit tests have not passed (test_fix_loop.status = <status>)>
NEXT RECOMMENDED COMMAND: /unit-testing <WORK-ID> | /analyze-story <STORY-ID>
```
