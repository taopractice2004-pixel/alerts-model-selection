# Copilot Instructions — SDLC Automation Framework

This repository contains a **reusable, manual, state-driven SDLC pipeline** built on GitHub
Copilot customization features (skills and instructions). These rules apply globally to every
SDLC skill in this framework. They are the single home of the shared pipeline rules; skills
reference them instead of repeating them.

## Universal Rules

1. **Always inspect existing project patterns before modifying code.** Use
   `.sdlc/context/project-profile.md`, plus the actual neighboring source, before writing new
   code.
2. **Never invent project-specific rules.** If a standard, convention, or policy is not
   discoverable, mark it `NOT_AVAILABLE`, `NOT_CONFIGURED`, `TO_BE_DISCOVERED`, or
   `TO_BE_CONFIGURED` — do not fabricate one.
3. **Prefer cached/reusable context before expensive rereads.** Follow the Read Order below
   before rereading Jira, full documents, or rescanning the repository.
4. **Implement only the requested story scope.** Avoid unrelated refactoring.
5. **This packaged pipeline covers repository setup, story analysis, implementation, unit
   testing (acceptance-criteria verification), the bounded test → fix loop, PR preparation, and
   the L0/L1 code reviews.** AI prepares the PR draft and performs the L0 (code-level) and L1
   (engineering/design) reviews, but never creates, pushes, approves, or merges a PR, never
   performs the L2 client review, and never deploys. Do not assume integration testing, merge,
   deployment, or QA automation stages exist in this repository unless they are added back
   explicitly.
6. **Update workflow state after each stage.** Every stage updates
   `.sdlc/work/<ID>/log.md`.
7. **Preserve traceability** from repository context → plan → implementation and validation
   through the artifacts under `.sdlc/context/` and `.sdlc/work/<ID>/`.
8. **Stop after each manually invoked stage.** Never chain stages automatically.
9. **Recommend, never automatically invoke, the next SDLC command.** Every stage ends with
   standard outcome block below and one of `STAGE_PASSED`, `STAGE_FAILED`, `CHANGES_REQUIRED`
   (L0/L1 review findings only), `WAITING_FOR_HUMAN`, `BLOCKED_MISSING_INFORMATION`.
   `NEXT RECOMMENDED COMMAND` is always
   filled with the exact command and arguments to run next (for example
   `/unit-testing STORY-12 current_story`), chosen by the Next Command Rules below, or `None`
   with the reason.
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
    `/analyze-story`, `/implement-story`, `/fix-bugs`, `/unit-testing`). There are no custom
    agents and no prompt wrappers; a skill must never create or invoke a custom agent or another
    skill.
13. **Every stage runs isolated.** Each skill declares `context: fork`, so it executes as an
    isolated subagent, writes its required `.sdlc` artifacts, returns only the concise stage
    outcome block, and STOPs. Skills are manual-only (`disable-model-invocation: true`).
14. **Cross-stage state lives in `.sdlc/`, not chat.** A stage reads prior-stage state only
    from `.sdlc/context/*` and `.sdlc/work/<ID>/*` (with `work.json` as the authoritative work
    record), never from earlier conversation context.
15. **Implementation never tests.** `/implement-story`, `/fix-bugs`, and `/address-review-comments`
    change source code and run only the build/compile check for the repository's tech stack.
    They never run unit tests (`/address-review-comments` may edit test files to correct a test,
    but does not run them). Writing/running unit tests and verifying the acceptance criteria
    happen only in `/unit-testing`.
16. **Git is read-only for AI.** No skill may commit, push, create/switch branches, open,
    approve, or merge a PR, or change Git configuration. Read-only diff/status inspection is
    allowed. Creating/pushing the PR, L2 client review, merge, and QA deployment are human-only.
17. **PR preparation and reviews add no auto-chaining.** `/prepare-pr`, `/l0-review`,
    `/l1-review`, and `/address-review-comments` are manual, `context: fork`, and only recommend
    the next command. Any reviewed code change re-runs `/unit-testing` and re-enters review from
    `/l0-review`.

## Story Flow and Test → Fix Loop

```
/setup-repo-context → /analyze-story → /implement-story → /unit-testing
                                                               │ bugs found
                                                               ▼
                                          ┌──────────── /fix-bugs ◄──┐
                                          ▼                          │ bugs remain and
                                     /unit-testing ──────────────────┘ fix_iteration < 3
                                          │ no bugs → PR/review phase (/prepare-pr)
                                          │ bugs remain and fix_iteration = 3 → ESCALATED_TO_DEVELOPER
```

- `/implement-story`: implement the finalized plan + build only. Next: `/unit-testing`.
- `/unit-testing`: write/update unit tests for the acceptance criteria in `work.json`, run them,
  and mark each criterion `MET` / `NOT_MET`. A failing test caused by the production code (not
  by the test itself) is a bug: record it in `work.json` → `test_fix_loop.open_bugs`.
  `/unit-testing` never changes production behavior to make a test pass.
- `/fix-bugs`: fix the bugs in `open_bugs`, increment `test_fix_loop.fix_iteration` by 1, build
  only. Next: `/unit-testing` (always — the fix is verified there, not in `/fix-bugs`).
- **Loop limit:** at most `max_fix_iterations` = **3** fix → retest cycles. If `/unit-testing`
  still finds bugs after the 3rd fix, it sets `test_fix_loop.status: ESCALATED_TO_DEVELOPER`,
  returns `WAITING_FOR_HUMAN`, and recommends no further pipeline command: a developer must
  investigate. `/fix-bugs` refuses to run (`WAITING_FOR_HUMAN`) once `fix_iteration` has reached
  3. Only a human may reset `fix_iteration` to 0 in `work.json` after investigating.
- The same loop applies to standalone bug work (`/fix-bugs` → `/unit-testing` → …).

## PR and Review Flow

When `/unit-testing` passes with all ACs `MET` and no open bugs, the work enters the PR/review
phase. These stages are all manual, `context: fork`, and never auto-chain. **AI never touches
Git** — creating/pushing the PR, the L2 client review, merge, and QA deployment are human-only.

```
/unit-testing PASS (review.return_after_testing = false)
        → /prepare-pr → [human creates/pushes PR] → /l0-review
              L0 PASS → /l1-review
                    L1 PASS → [human: L2 client review] → [human: merge] → [human: deploy QA]
```

Review-fix loop (L0/L1/L2 comments):
```
/address-review-comments <ID> <l0|l1|l2>
        → /unit-testing (review.return_after_testing = true)
              → [human updates SAME PR] → /l0-review → /l1-review → L2 again
```

- `/prepare-pr`: write the PR draft to `pr.md`, set `work.json` → `pr`, STOP `WAITING_FOR_HUMAN`.
  The developer manually creates/pushes the PR. Next: `/l0-review`.
- `/l0-review`: read-only **code-level** review (scope discipline, standards, code quality,
  security, configured static checks). `PASS` → `/l1-review`; `CHANGES_REQUIRED` →
  `/address-review-comments <ID> l0`.
- `/l1-review`: read-only **engineering/design** review (requirement correctness, architecture,
  design/maintainability, API/data/config design, test strategy) — never duplicates L0. `PASS`
  → human-only L2 client review; `CHANGES_REQUIRED` → `/address-review-comments <ID> l1`.
- `/address-review-comments <ID> <l0|l1|l2>`: classify each item as `IN_SCOPE_TECHNICAL_FIX`,
  `REQUIREMENT_OR_SCOPE_CHANGE`, or `NEEDS_HUMAN_CLARIFICATION`; apply only the smallest
  in-scope fixes (may edit production and test code, build only, no test execution), set
  `review.return_after_testing: true`, and recommend `/unit-testing`. A
  `REQUIREMENT_OR_SCOPE_CHANGE` routes to `/analyze-story`; `NEEDS_HUMAN_CLARIFICATION` STOPs
  `WAITING_FOR_HUMAN`.
- **Review re-entry rule:** any production-code change from L0/L1/L2 comments must re-run
  `/unit-testing`, then the developer updates the SAME PR, and review restarts from `/l0-review`
  → `/l1-review` → L2 — never jump straight back to the level that raised the comment. The
  `review.return_after_testing` flag tells `/unit-testing` to route to the update-PR →
  `/l0-review` path instead of `/prepare-pr`.
- There is **no `/l2-review` AI skill** and **no skills for pushing/creating the PR, merge, or QA
  deployment** — those are human-only.

### Next Command Rules

| Stage result | NEXT RECOMMENDED COMMAND |
|---|---|
| `/setup-repo-context`, `/refresh-repo-context` passed | `/analyze-story <STORY-ID>` |
| `/analyze-story` passed | `/implement-story <STORY-ID>` |
| `/implement-story` passed (build OK or `NOT_CONFIGURED`) | `/unit-testing <STORY-ID> current_story` |
| `/implement-story` build failed | `/implement-story <STORY-ID>` after the build error is addressed (`STAGE_FAILED`) |
| `/unit-testing`: tests pass, all `MET`, `review.return_after_testing` = false | `/prepare-pr <ID>` |
| `/unit-testing`: tests pass, all `MET`, `review.return_after_testing` = true | `WAITING_FOR_HUMAN` — developer updates the SAME PR, then `/l0-review <ID>` |
| `/unit-testing`: bugs found, `fix_iteration` < 3 | `/fix-bugs <WORK-ID> current_story` (or `existing_work_id <ID>` / `standalone`, matching the work folder) |
| `/unit-testing`: bugs found, `fix_iteration` = 3 | `None — escalated to developer` (`WAITING_FOR_HUMAN`) |
| `/fix-bugs` passed | `/unit-testing <WORK-ID> <same relationship mode>` |
| `/fix-bugs` with `fix_iteration` already 3 | `None — escalated to developer` (`WAITING_FOR_HUMAN`) |
| `/prepare-pr` passed | `WAITING_FOR_HUMAN` — developer creates/pushes PR, then `/l0-review <ID>` |
| `/l0-review` PASS | `/l1-review <ID>` |
| `/l0-review` `CHANGES_REQUIRED` | `/address-review-comments <ID> l0` |
| `/l1-review` PASS | `WAITING_FOR_HUMAN` — L2 client review (human-only); no AI command |
| `/l1-review` `CHANGES_REQUIRED` | `/address-review-comments <ID> l1` |
| `/address-review-comments` in-scope fixes applied | `/unit-testing <ID> current_story` |
| `/address-review-comments` requirement/scope change | `/analyze-story <ID>` |
| `/address-review-comments` needs clarification | `WAITING_FOR_HUMAN` — no AI command until clarified |
| any stage `BLOCKED_MISSING_INFORMATION` | the same command again once the missing input is supplied |

## Pipeline Files

```
.sdlc/
  context/                     repository cache (one per repo)
    manifest.json              mode, detected technologies, exclusions, standards routing index
    project-profile.md         stack, architecture, repository map, build/test/run, docs index
    standards-summary.md       compact standards catalog and selection rules
  templates/
    work.template.json         → work/<ID>/work.json
    plan.template.md           → work/<ID>/plan.md
    log.template.md            → work/<ID>/log.md
  work/<ID>/                   one folder per story, bug, or testcase
    work.json                  AUTHORITATIVE: scope, acceptance criteria, exact files, standards ids,
                               commands, test → fix loop state, PR + review state, gaps
    plan.md                    thin human review: intent, strategy, boundaries, risks
    log.md                     stage statuses + one appended entry per stage run
    pr.md                      prepared PR draft (written by /prepare-pr; human creates the PR)
```

**One fact, one owner.** Acceptance criteria, open bugs, loop state, exact files, anchors, adjacent dependencies, standard ids, commands,
constraints, missing facts, unresolved questions, and PR/review state (`pr`, `review`) live only
in `work.json`. `pr.md` holds only the human-readable PR draft content. `plan.md` and
`log.md` reference them and never repeat them. `plan.md` and `work.json` are overwritten fully when
regenerated; `log.md` has its status header updated in place and a new entry appended per run.

## Stage Outputs

| Stage | Writes |
|---|---|
| `/setup-repo-context` | all of `.sdlc/context/*` (from code with `1`, from the requirements doc with `2`); the exclusions section below |
| `/refresh-repo-context` | only stale parts of `.sdlc/context/*`; exclusions section only if exclusion facts materially changed |
| `/analyze-story` | `work.json`, `plan.md`, `log.md` |
| `/implement-story` | source changes (no tests); build result in `log.md`; `work.json` / `plan.md` only if the touched slice changed |
| `/fix-bugs` | creates `work.json`, `plan.md`, `log.md` when standalone with no work folder; source fixes (no tests); build result; `work.json` → `test_fix_loop` (`fix_iteration`, fixed bugs) ; `log.md` |
| `/unit-testing` | unit-test files only; unit-test, acceptance-criteria, and coverage results in `log.md`; `work.json` → `test_fix_loop` (`status`, `open_bugs`) |
| `/prepare-pr` | `pr.md` (PR draft); `work.json` → `pr`; `log.md`. Never touches Git |
| `/l0-review` | `work.json` → `review.l0` (code-level findings, status); `log.md`. Read-only, no code edits |
| `/l1-review` | `work.json` → `review.l1` (engineering/design findings, status); `log.md`. Read-only, no code edits |
| `/address-review-comments` | in-scope production/test fixes (no test execution); build result; `work.json` → `review.<source>` + `review.return_after_testing` / `origin` / `cycle`; `log.md` |

Every stage ends with:

```
CURRENT STAGE:
STATUS:
FILES CREATED/UPDATED:
SUMMARY:
TEST → FIX LOOP:            (only /unit-testing and /fix-bugs: <fix_iteration>/3 — <status>)
NEXT RECOMMENDED COMMAND:   (exact command + arguments per the Next Command Rules, or None — reason)
```

Work cases: `SIMPLE` (no Requirement Analysis section needed), `AMBIGUOUS` (fill the plan's
Requirement Analysis section), `STALE_REPLAN` (regenerate because inputs or repository facts
changed), `BUGFIX`, `TESTCASE`.

## Repository Modes

`/setup-repo-context 1` sets `manifest.json` → `repositoryMode: EXISTING_PROJECT` (context
from the code). `/setup-repo-context 2 <requirements doc>` sets `NEW_PROJECT` (context from
the requirements document; `project-profile.md` describes the planned structure). Every stage
copies the mode into `work.json` → `repository_mode` and behaves as follows:

- `EXISTING_PROJECT`: all rules in this file apply as written.
- `NEW_PROJECT`:
  - `/analyze-story` scopes against the planned structure in `project-profile.md`; exact source
    and test files may be planned paths marked ` (new)`, and validation commands may be
    `TO_BE_CONFIGURED` until the build/test tooling exists. Do not search for code that does not
    exist yet.
  - `/implement-story` may create the initial project structure, but only as planned in
    `plan.md` and `project-profile.md`; it records any `TO_BE_DECIDED` choice it makes in
    `plan.md`, sets real build and unit-test commands in `work.json` once tooling exists, and
    notes in `SUMMARY` that `/refresh-repo-context` should run after the first structure is
    created (the next command is still `/unit-testing`).
  - `/fix-bugs` and `/unit-testing` need existing code: if their anchor does not exist yet,
    STOP with `BLOCKED_MISSING_INFORMATION` and recommend `/implement-story`.
  - Do not reread the full requirements document; use the Requirements Summary in
    `project-profile.md`, and read a specific section of the document only for an exact missing
    detail.
- Once real code exists, `/refresh-repo-context` re-derives facts from the code and switches the
  mode to `EXISTING_PROJECT`.

## Effort Dial

- `low` (default): cache first, deterministic pre-pass first, leave Requirement Analysis as
  "Not required" unless blocked.
- `standard`: cache first, compact artifacts, Requirement Analysis only for real ambiguity.
- `deep`: broader reasoning for risky or complex work.

Escalate from `low` only when the work is ambiguous, risky, or repeatedly fails focused
validation. Record the chosen mode in `work.json` → `effort_mode`.

## Read Order

1. `.sdlc/work/<ID>/work.json`
2. `.sdlc/work/<ID>/log.md` (current stage and prior runs)
3. Exact source files and tests listed in `work.json`
4. `.sdlc/work/<ID>/plan.md` only when intent, strategy, boundaries, or risks are missing from
   `work.json`
5. `.sdlc/context/*` only when `work.json` sets `repo_context_fallback_needed` or records a
   missing fact
6. Repository-wide search only when no concrete anchor exists after the earlier steps

## Context Rules

- Cache first. Prefer exact files over folders, and folders over repository-wide search.
- If the input already provides a concrete file, test, stack trace, module, endpoint, class, or
  function anchor, do not broad-scan the repository.
- Read the smallest section or line range that can answer the next question.
- Do not reread external trackers during implementation, bug fixing, or unit testing.
- Keep `work.json` compact unless the slice is truly broader: `exact_source_files` ≤ 5,
  `exact_test_files` ≤ 3, `selected_standards` ≤ 5. Record only commands expected to run for
  the scoped slice.
- Standards: rely on `work.json` → `selected_standards` plus the auto-applied
  `.github/instructions/standards/*.instructions.md`; read a full `standards/*.md` only for an
  exact rule or missing detail. Never reread the whole `standards/` folder. Ids come from
  `.sdlc/context/manifest.json` → `standards.items`.

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

Before searching or reading repository files, check `.sdlc/context/manifest.json` →
`exclusions` (`excludedPaths`, `excludedPatterns`). Paths listed there are non-authoritative
context such as generated output, dependencies, build artifacts, caches, logs or temporary
files. Do not reference, search, summarize or load these paths by default. Only access an
excluded path when the current task explicitly requires it and there is no authoritative source
alternative. Prefer:
1. work.json
2. exact identified source/test files
3. .sdlc/context caches
4. relevant repository source/configuration files
before performing broader repository searches. Do not treat file extensions such as JSON, XML,
YAML, YML or properties as globally irrelevant. Use them when they contain meaningful project
configuration or architecture information.

## Where to Look

- Pipeline overview, flows, and diagram: [README.md](../README.md)
- Repository cache: `.sdlc/context/`
- Per-work-item state: `.sdlc/work/<ID>/`
- Templates: `.sdlc/templates/`
- Domain instructions: `.github/instructions/`
- Skills (sole stage execution path, `context: fork`): `.github/skills/`
