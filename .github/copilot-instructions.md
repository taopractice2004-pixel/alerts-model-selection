# Copilot Instructions — SDLC Automation Framework

This repository contains a **reusable, manual, state-driven SDLC pipeline** built on GitHub
Copilot customization features (skills and instructions). These rules apply globally to every
SDLC skill in this framework. They are the single home of the shared pipeline rules; skills
reference them instead of repeating them.

## Universal Rules

1. **Always inspect existing project patterns before modifying code.** Use
   `.sdlc/context/project-profile.md`, plus the actual neighboring source, before writing new
   code, and match its conventions (naming, layering, error handling, dependency injection)
   rather than introducing new ones.
2. **Never invent project-specific rules.** If a standard, convention, or policy is not
   discoverable, mark it `NOT_AVAILABLE`, `NOT_CONFIGURED`, `TO_BE_DISCOVERED`, or
   `TO_BE_CONFIGURED` — do not fabricate one. When work must proceed anyway, take the most
   conservative approach and record the assumption in `work.json` → `assumptions`.
3. **Prefer cached/reusable context before expensive rereads.** Follow the Read Order below
   before rereading Jira, full documents, or rescanning the repository.
4. **Implement only the requested story scope.** Avoid unrelated refactoring, renames, or
   drive-by cleanups; prefer the smallest change that satisfies the acceptance criteria.
5. **This packaged pipeline covers repository setup, story analysis, implementation, unit
   testing (acceptance-criteria verification), the bounded test → fix loop, and the pre-PR
   code review with its bounded review → fix loop only.** Do not assume integration testing,
   PR creation, human L1/L2 review, merge, deployment, or QA stages exist in this repository
   unless they are added back explicitly.
6. **Update workflow state after each stage.** Every stage updates
   `.sdlc/work/<ID>/log.md`.
7. **Preserve traceability** from repository context → plan → implementation and validation
   through the artifacts under `.sdlc/context/` and `.sdlc/work/<ID>/`.
8. **Stop after each manually invoked stage.** Never chain stages automatically.
9. **Recommend, never automatically invoke, the next SDLC command.** Every stage ends with
   the standard outcome block below and one of `STAGE_PASSED`, `STAGE_FAILED`,
   `WAITING_FOR_HUMAN`, `BLOCKED_MISSING_INFORMATION`. `NEXT RECOMMENDED COMMAND` is always
   filled with the exact command and arguments to run next (for example
   `/unit-testing STORY-12`), chosen by the Next Command Rules below, or `None` with the
   reason.
10. **No orchestrator.** No skill triggers another SDLC skill on its own. The parent Copilot
    agent only invokes the skill the developer requested and relays its final stage result.
11. **Technology-agnostic by design.** Nothing in this framework hardcodes a language,
    framework, project name, Jira ID, repository, branch, test framework, build command, or
    company-specific rule. All of that is discovered per-repository via `/setup-repo-context` /
    `/refresh-repo-context` or supplied per story, bug, or unit-test task via `/analyze-story`,
    `/fix-bugs`, or `/unit-testing`.
12. **Pure skill-first design.** Each SDLC stage is a skill in
    `.github/skills/<stage>/SKILL.md`, which is the complete authoritative role and procedure.
    Developers invoke skills directly (`/setup-repo-context`, `/refresh-repo-context`,
    `/analyze-story`, `/implement-story`, `/fix-bugs`, `/unit-testing`, `/code-review`). There
    are no custom agents and no prompt wrappers; a skill must never create or invoke a custom
    agent or another skill.
13. **Every stage runs isolated.** Each skill declares `context: fork`, so it executes as an
    isolated subagent, writes its required `.sdlc` artifacts, returns only the concise stage
    outcome block, and STOPs. Skills are manual-only (`disable-model-invocation: true`).
14. **Cross-stage state lives in `.sdlc/`, not chat.** A stage reads prior-stage state only
    from `.sdlc/context/*` and `.sdlc/work/<ID>/*` (with `work.json` as the authoritative work
    record), never from earlier conversation context.
15. **Implementation never tests.** `/implement-story` and `/fix-bugs` change source code and run
    only the build/compile check for the repository's tech stack (`NOT_CONFIGURED` when there is
    none). They never write, edit, or run unit tests. Writing unit tests, running them, and
    verifying the acceptance criteria happen only in `/unit-testing`, which never changes
    production behavior.
16. **Review never edits code.** `/code-review` only records findings in `work.json` →
    `code_review`; the fixes are made by `/fix-bugs`, verified by `/unit-testing`, and
    re-reviewed by `/code-review`. Review limits come only from
    `standards/code-review-standards.md` and the standards that apply to the changed files,
    never from the skill. It uses only analyzers defined in the repository, never tools
    installed on one machine, so every developer gets the same review.
17. **Plan approval is given in the implement command.** A story plan is approved only when
    the developer runs `/implement-story <STORY-ID> approved` (the word `approved` is
    case-insensitive). Without it, `/implement-story` changes nothing and STOPs with
    `WAITING_FOR_HUMAN`. See Plan Review and Open Questions below.

## Plan Review and Open Questions

After `/analyze-story`, the developer reviews `plan.md` — the complete readable plan: files to
change (create / modify / delete and what changes), implementation steps, AC coverage with
planned tests, contract / data changes, impact on callers, assumptions, risks, open questions,
and a review checklist. Corrections and answers are made in `work.json`, which owns every fact.

- **No open questions** (`/analyze-story` → `STAGE_PASSED`): if the plan is right, run
  `/implement-story <STORY-ID> approved`. If it is wrong, rerun `/analyze-story <STORY-ID>`
  with the corrected story inputs.
- **Open questions** (`/analyze-story` → `WAITING_FOR_HUMAN`): answer every question in
  `work.json` → `unresolved_questions[].answer`, save the file, then run
  `/implement-story <STORY-ID> approved`. Rerunning `/analyze-story` is not needed for
  clarifying answers.
- `/implement-story <STORY-ID> approved` then:
  1. refuses (`WAITING_FOR_HUMAN`, nothing changed) if any answer is empty, naming the
     unanswered question ids;
  2. refuses (`WAITING_FOR_HUMAN`, nothing changed) if an answer changes scope — it adds or
     changes an acceptance criterion, or needs files outside `exact_source_files[].path` — and
     recommends `/analyze-story <STORY-ID>` to re-plan, because `/unit-testing` verifies only
     the criteria recorded in `work.json`;
  3. otherwise removes each answered question from `unresolved_questions` and records it once
     in `constraints` (for example `"Q1: include archived records → no, exclude them"`), sets
     `plan_review` to
     `status: APPROVED` with `approved_via` and `date`, logs this in `log.md`, and implements.
- `plan_review.status` values: `PENDING` (awaiting developer review), `APPROVED`,
  `NOT_REQUIRED` (standalone bug or testcase folders created by `/fix-bugs` or
  `/unit-testing`). Every `/analyze-story` run resets it to `PENDING`, so a re-planned story
  must be approved again. No skill sets `APPROVED` except `/implement-story` when the developer
  passed `approved`.

## Work IDs and Work Folders

`/fix-bugs`, `/unit-testing`, and `/code-review` take a work ID (a story, bug, or testcase id):
- If `.sdlc/work/<ID>/` exists, the stage uses that folder; its `work.json` → `work_type` says
  whether it is a story, bug, or testcase.
- If it does not exist, `/fix-bugs` and `/unit-testing` start **standalone** work: they ask for
  the missing details and create the folder (`plan_review.status: NOT_REQUIRED`).
  `/code-review` STOPs with `BLOCKED_MISSING_INFORMATION`, because there is nothing to review.

## Story Flow and Test → Fix Loop

```
/setup-repo-context → /analyze-story → [developer reviews plan, answers open questions in work.json]
                    → /implement-story <ID> approved → /unit-testing
                                                               │ bugs found
                                                               ▼
                                          ┌──────────── /fix-bugs ◄──┐
                                          ▼                          │ bugs remain and
                                     /unit-testing ──────────────────┘ fix_iteration < 3
                                          │ bugs remain and fix_iteration = 3 → ESCALATED_TO_DEVELOPER
                                          │ no bugs (TESTS_PASSED)
                                          ▼
                                     /code-review ◄──────────────────────────────┐
                                          │ blocking findings                     │
                                          ▼                                       │
                                     /fix-bugs (review findings) → /unit-testing ─┘
                                          │ no blocking findings → COMPLETE (ready for PR)
                                          │ blocking findings remain and review_fix_iteration = 2 → ESCALATED_TO_DEVELOPER
```

- `/implement-story`: implement the finalized plan + build only. Next: `/unit-testing`.
- `/unit-testing`: write/update unit tests for the acceptance criteria in `work.json`, run them,
  and mark each criterion `MET` / `NOT_MET`. A failing test caused by the production code (not
  by the test itself) is a bug: record it in `work.json` → `test_fix_loop.open_bugs`.
- `/fix-bugs`: fix the bugs in `open_bugs`, increment `test_fix_loop.fix_iteration` by 1, build
  only. Next: `/unit-testing` (always — the fix is verified there, not in `/fix-bugs`).
- **Loop limit:** at most `max_fix_iterations` = **3** fix → retest cycles. If `/unit-testing`
  still finds bugs after the 3rd fix, it sets `test_fix_loop.status: ESCALATED_TO_DEVELOPER`,
  returns `WAITING_FOR_HUMAN`, and recommends no further pipeline command: a developer must
  investigate. `/fix-bugs` refuses to run (`WAITING_FOR_HUMAN`) once `fix_iteration` has reached
  3. Only a human may reset `fix_iteration` to 0 in `work.json` after investigating.
- The same loop applies to standalone bug work (`/fix-bugs` → `/unit-testing` → …).

### Review → Fix Loop

- `/code-review` runs only when `test_fix_loop.status` is `TESTS_PASSED`. It reviews the changed
  code against `standards/code-review-standards.md` and the standards that apply to the changed
  files, and records findings (`BLOCKER` / `MAJOR` / `MINOR`) in `work.json` →
  `code_review.findings`. Only `BLOCKER` and `MAJOR` findings are blocking; `MINOR` findings are
  listed for the developer. Findings in test files are always `MINOR`.
- `/fix-bugs` takes its input from `test_fix_loop.open_bugs` when `test_fix_loop.status` is
  `BUGS_OPEN` (bugs always first), otherwise from the blocking review findings when
  `code_review.status` is `FINDINGS_OPEN`. A review fix is a behavior-preserving change; it
  increments `code_review.review_fix_iteration`, marks the findings `FIX_APPLIED`, sets
  `code_review.status: FIXES_APPLIED` and `test_fix_loop.status: RETEST_REQUIRED`. Next:
  `/unit-testing`, then `/code-review` again.
- A review fix that breaks a test is a normal bug in the test → fix loop.
- Findings that would change a public contract, a caller's signature, or acceptance-criterion
  behavior are `auto_fixable: false`. The developer decides them in the command, not by editing
  `work.json`: `/fix-bugs <WORK-ID> approve <ids>` or `/fix-bugs <WORK-ID> waive <ids>
  "<reason>"`. `/fix-bugs` records the decision (with the exact command in `decided_via`). Only
  a human waives a finding; any blocking finding may be waived this way, even at the loop limit.
- **Fixing MINOR findings by choice:** `MINOR` findings never block, but the developer can opt in
  to fixing chosen ones in the same round with `approve` (ids separated by spaces or commas),
  for example `/fix-bugs <WORK-ID> approve R3 R9`. This costs no extra round. `/fix-bugs` fixes
  approved findings in production files; the next `/unit-testing` fixes approved findings in
  test files. Only `/fix-bugs` counts the round (`review_fix_iteration`), once per run.
- When a finding is `RESOLVED`, `/code-review` keeps only its id, rule, severity and status in
  `work.json`; its full text stays in `log.md`.
- **Loop limit:** at most `max_review_fix_iterations` = **2** review fix cycles. If blocking
  findings remain after the 2nd, `/code-review` sets `code_review.status:
  ESCALATED_TO_DEVELOPER` and returns `WAITING_FOR_HUMAN`. `/fix-bugs` refuses review fixes once
  `review_fix_iteration` has reached 2. Only a human may reset it.
- A fresh `/implement-story` resets `code_review` (`review_fix_iteration: 0`, `status:
  NOT_STARTED`, `findings: []`).

### Next Command Rules

| Stage result | NEXT RECOMMENDED COMMAND |
|---|---|
| `/setup-repo-context`, `/refresh-repo-context` passed | `/analyze-story <STORY-ID>` |
| `/analyze-story` passed (no open questions) | `/implement-story <STORY-ID> approved — after the developer reviews plan.md and work.json` |
| `/analyze-story` with open questions (`WAITING_FOR_HUMAN`) | `/implement-story <STORY-ID> approved — after the developer answers every question in work.json → unresolved_questions[].answer and saves` |
| `/implement-story` without `approved` | `/implement-story <STORY-ID> approved — after reviewing the plan` (`WAITING_FOR_HUMAN`) |
| `/implement-story` with an unanswered question | `/implement-story <STORY-ID> approved — after answering <Q ids> in work.json` (`WAITING_FOR_HUMAN`) |
| `/implement-story`: an answer changes scope | `/analyze-story <STORY-ID>` to re-plan, then `/implement-story <STORY-ID> approved` (`WAITING_FOR_HUMAN`) |
| `/implement-story` passed (build OK or `NOT_CONFIGURED`) | `/unit-testing <STORY-ID>` |
| `/implement-story` build failed | `/implement-story <STORY-ID> approved` after the build error is addressed (`STAGE_FAILED`) |
| `/unit-testing`: all tests pass, all criteria `MET` | `/code-review <WORK-ID>` |
| `/unit-testing`: bugs found, `fix_iteration` < 3 | `/fix-bugs <WORK-ID>` |
| `/unit-testing`: bugs found, `fix_iteration` = 3 | `None — escalated to developer` (`WAITING_FOR_HUMAN`) |
| `/fix-bugs` passed | `/unit-testing <WORK-ID>` |
| `/fix-bugs` with `fix_iteration` already 3 | `None — escalated to developer` (`WAITING_FOR_HUMAN`) |
| `/fix-bugs` with `review_fix_iteration` already 2 (review findings only) | `None — escalated to developer` (`WAITING_FOR_HUMAN`) |
| `/code-review`: no blocking findings | `None — ready for PR`; optionally `/fix-bugs <WORK-ID> approve <MINOR ids>` while review rounds remain |
| `/code-review`: blocking findings, all `auto_fixable`, `review_fix_iteration` < 2 | `/fix-bugs <WORK-ID>`, optionally with `approve <MINOR ids>` (`STAGE_FAILED`) |
| `/code-review`: blocking findings need a developer decision | `/fix-bugs <WORK-ID> approve <ids>` or `/fix-bugs <WORK-ID> waive <ids> "<reason>"` (`WAITING_FOR_HUMAN`) |
| `/fix-bugs`: decisions recorded, nothing left to fix | `/code-review <WORK-ID>` |
| `/code-review`: blocking findings, `review_fix_iteration` = 2 | `None — escalated to developer` (`WAITING_FOR_HUMAN`) |
| `/code-review` before tests pass | `/unit-testing <WORK-ID>` (`BLOCKED_MISSING_INFORMATION`) |
| any stage `BLOCKED_MISSING_INFORMATION` | the same command again once the missing input is supplied |

## Pipeline Files

```
.sdlc/
  context/                     repository cache (one per repo)
    manifest.json              mode, detected technologies, exclusions, standards routing index
    project-profile.md         stack, architecture, repository map, build/test/run, docs index
  templates/
    work.template.json         → work/<ID>/work.json
    plan.template.md           → work/<ID>/plan.md
    log.template.md            → work/<ID>/log.md
  work/<ID>/                   one folder per story, bug, or testcase
    work.json                  AUTHORITATIVE: scope, acceptance criteria, per-file changes, steps,
                               planned tests, contract changes, boundaries, assumptions, risks,
                               commands, test → fix loop state, code review findings +
                               review → fix loop state, gaps, open questions + answers, plan review
    plan.md                    readable plan for developer review, written once from work.json at
                               analysis time (snapshot; not updated by later stages)
    log.md                     stage statuses + one appended entry per stage run
```

**One fact, one owner.** Acceptance criteria, per-file changes, implementation steps, planned
tests, contract changes, boundaries, assumptions, risks, open bugs, review findings and their
waivers, loop state, anchors, adjacent dependencies, commands, constraints, missing facts,
unresolved questions and their answers, and plan review status are owned by `work.json`, the
only work file humans edit.
- `plan.md` is written from `work.json` only by `/analyze-story` (and once when `/fix-bugs` or
  `/unit-testing` create a standalone folder). It never adds a fact that is not in `work.json`
  and is not updated by later stages: after approval it is a snapshot of the approved plan, and
  later changes are recorded in `work.json` and `log.md`.
- `log.md` records what happened per run and does not repeat the plan. Its status header is
  updated in place and a new entry is appended per run.
- `plan.md` and `work.json` are overwritten fully when regenerated by `/analyze-story`.
- When a stage finds that a recorded fact is wrong (a build command, a file, a standard),
  it corrects the owning file immediately instead of leaving a stale copy.
- Artifacts are summaries, not copies: never paste full tickets, requirements documents,
  standards, or source files into `.sdlc/`. Use the work ID as the folder key, and create no
  work files beyond the three above.

## Stage Outputs

| Stage | Writes |
|---|---|
| `/setup-repo-context` | all of `.sdlc/context/*` (from code with `1`, from the requirements doc with `2`) |
| `/refresh-repo-context` | only stale parts of `.sdlc/context/*` |
| `/analyze-story` | `work.json`, `plan.md`, `log.md` |
| `/implement-story` | source changes (no tests); build result and plan approval in `log.md`; `work.json` → `plan_review`, `constraints` (from answers), loop resets; other `work.json` fields only if the touched slice changed |
| `/fix-bugs` | creates `work.json`, `plan.md`, `log.md` when standalone with no work folder; source fixes (no tests); build result; `work.json` → `test_fix_loop` (`fix_iteration`, fixed bugs) or, for review findings, `code_review` (`review_fix_iteration`, finding status) and `test_fix_loop.status`; `log.md` |
| `/unit-testing` | creates `work.json`, `plan.md`, `log.md` when standalone with no work folder; unit-test files only; unit-test, acceptance-criteria, and coverage results in `log.md`; `work.json` → `test_fix_loop` (`status`, `open_bugs`), and approved test-file findings it fixed |
| `/code-review` | no source or test changes; `work.json` → `code_review` (`status`, `analyzer_commands`, `findings`); review results in `log.md` |

Every stage ends with:

```
CURRENT STAGE:
STATUS:
FILES CREATED/UPDATED:
SUMMARY:
TEST → FIX LOOP:            (only /unit-testing and /fix-bugs: <fix_iteration>/3 — <status>)
REVIEW → FIX LOOP:          (only /code-review, and /fix-bugs on review findings: <review_fix_iteration>/2 — <status>)
NEXT RECOMMENDED COMMAND:   (exact command + arguments per the Next Command Rules, or None — reason)
```

## Repository Modes

`/setup-repo-context 1` sets `manifest.json` → `repositoryMode: EXISTING_PROJECT` (context
from the code). `/setup-repo-context 2 <requirements doc>` sets `NEW_PROJECT` (context from
the requirements document; `project-profile.md` describes the planned structure). Every stage
copies the mode into `work.json` → `repository_mode` and behaves as follows:

- `EXISTING_PROJECT`: all rules in this file apply as written.
- `NEW_PROJECT`:
  - `/analyze-story` scopes against the planned structure in `project-profile.md`; exact source
    and test files may be planned paths with `action: create`, and validation commands may be
    `TO_BE_CONFIGURED` until the build/test tooling exists. Do not search for code that does not
    exist yet.
  - `/implement-story` may create the initial project structure, but only as planned in
    `work.json` and `project-profile.md`; it records any `TO_BE_DECIDED` choice it makes in
    `work.json` → `assumptions`, sets real build and unit-test commands in `work.json` once
    tooling exists, and notes in `SUMMARY` that `/refresh-repo-context` should run after the
    first structure is created (the next command is still `/unit-testing`).
  - `/fix-bugs`, `/unit-testing`, and `/code-review` need existing code: if their anchor does
    not exist yet, STOP with `BLOCKED_MISSING_INFORMATION` and recommend `/implement-story`.
  - Do not reread the full requirements document; use the Requirements Summary in
    `project-profile.md`, and read a specific section of the document only for an exact missing
    detail.
- Once real code exists, `/refresh-repo-context` re-derives facts from the code and switches the
  mode to `EXISTING_PROJECT`.

## Effort Dial

- `low` (default): cache first, deterministic pre-pass first, `plan.md` sections of 1–3 lines,
  unless the plan has open questions or is a re-plan (then full detail).
- `standard`: cache first, compact `work.json`, fuller `plan.md` detail only where real
  ambiguity or risk exists.
- `deep`: broader reasoning for risky or complex work; full `plan.md` detail.

Escalate from `low` only when the work is ambiguous, risky, or repeatedly fails focused
validation. Record the chosen mode in `work.json` → `effort_mode`.

## Read Order

1. `.sdlc/work/<ID>/work.json`
2. `.sdlc/work/<ID>/log.md` (current stage and prior runs)
3. Exact source files and tests listed in `work.json`
4. `.sdlc/context/*` only when `work.json` sets `repo_context_fallback_needed` or records a
   missing fact
5. Repository-wide search only when no concrete anchor exists after the earlier steps

Later stages never read `plan.md`: it only renders `work.json`.

## Context Rules

- Cache first. Prefer exact files over folders, and folders over repository-wide search.
- Pipeline setup mistakes are corrected by the pipeline, not the developer: when a stage finds
  a missing or wrong command, file entry, or test project in `work.json`, it corrects it and
  logs the correction in `log.md`. Developers edit `work.json` only to answer open questions or
  correct the plan before approval, and to reset a loop counter after an escalation.
- If the input already provides a concrete file, test, stack trace, module, endpoint, class, or
  function anchor, do not broad-scan the repository.
- Read the smallest section or line range that can answer the next question.
- Do not reread external trackers during implementation, bug fixing, unit testing, or review.
- Keep `work.json` compact unless the slice is truly broader: `exact_source_files` ≤ 5,
  `exact_test_files` ≤ 3. When a slice genuinely needs more, record why in
  `scope_override_reason` instead of leaving needed files out. Record only commands expected to
  run for the scoped slice.
- Keep every text value in `work.json` to one short sentence (each `change`, `reason`, step,
  planned test, assumption, risk, finding `problem` and `suggested_fix`), and store each fact
  once. Explanations and history belong in `log.md`. Every stage reads `work.json` in full, so
  its size is paid on every run.
- Standards: the compact `.github/instructions/standards/*.instructions.md` files apply
  automatically to matching files (`applyTo`). Read a full `standards/*.md` only for an exact
  rule or missing detail, finding it through `.sdlc/context/manifest.json` → `standards.items`
  (the items whose `applies_to` matches the touched files). Never reread the whole
  `standards/` folder.

## Staleness

- Run `/refresh-repo-context` when repository structure, standards, documentation, or
  build/test commands change.
- Regenerate a story's `work.json` / `plan.md` only when story inputs or acceptance criteria
  changed, impacted code changed enough to invalidate it, or a refresh changed facts it depends
  on.
- Create or patch a standalone bug or testcase work folder when the work is not attached to an
  existing folder, narrows to a different slice than the cached one, or the actual touched
  files / validation / coverage commands differ from the cache.

## Repository Context Exclusions

Before any search or broad read, check `.sdlc/context/manifest.json` → `exclusions`
(`excludedPaths`, `excludedPatterns`: generated output, dependencies, build artifacts, caches,
logs). Do not search, reference, or load those paths unless the task explicitly requires it and
no source alternative exists. Configuration files (`*.json`, `*.xml`, `*.yaml`, `*.yml`,
`*.properties`) are not excluded by extension; use them when they carry project configuration.

## Where to Look

- Repository cache: `.sdlc/context/`
- Per-work-item state: `.sdlc/work/<ID>/`
- Templates: `.sdlc/templates/`
- Domain instructions: `.github/instructions/`
- Standards: `standards/`
- Skills (sole stage execution path, `context: fork`): `.github/skills/`
