# SDLC Pipeline

This is a fresh pipeline package designed to reduce Copilot credit usage.

## Execution Model
- **Sole execution path = skills** in `.github/skills/<stage>/SKILL.md`. Each skill is the
  complete authoritative role and procedure, is manual-only
  (`disable-model-invocation: true`), and declares `context: fork` so it runs as an isolated
  subagent. There are no custom agents and no prompt wrappers.
- **Flow:** developer invokes skill → skill runs isolated → writes `.sdlc` artifacts → returns
  concise stage result → STOP → human reviews and invokes the next skill.
- **Cross-stage state** comes only from `.sdlc` artifacts, never from earlier chat context.
- **Shared/always rules = instructions** in `.github/copilot-instructions.md` and
  `.github/instructions/`.
- **Runtime context/state = the `.sdlc` cache.**

## Commands
- `/setup-repo-context`
- `/refresh-repo-context`
- `/analyze-story`
- `/implement-story`
- `/fix-bugs`
- `/unit-testing`

Developers invoke these skills directly. No skill chains into another.

## Design Goals
- compact repository cache
- compact story cache
- cache-first downstream stages
- no duplicate artifact writing
- optional requirement analysis
- implementation cache for minimal rereading
- focused standalone bug-fix flow
- focused unit-test creation with scoped validation and coverage
- company standards preserved through `standards/`
- fresh and ready to bootstrap

## Main Folders
- `config/`
- `context/`
- `framework/`
- `templates/`
- `work/`

## Work Files Created
- `session.md`
- `story-context.md`
- `implementation-plan.md`
- `impact-map.md`
- `implementation-cache.json`
- optional `requirement-analysis.md`
- `changes.md`

`implementation-cache.json` is the authoritative machine-readable work artifact.

The markdown work files are intentionally thin human-review summaries. They should not repeat
exact file inventories, validation commands, or repository facts already recorded in the cache.

These same compact artifacts are reused by `/fix-bugs` when the bug attaches to an existing
story or when a standalone bug work folder is created.

These same compact artifacts are also reused by `/unit-testing` when unit tests are added
after story implementation, after bug fixing, or as a standalone test task.

Later stages must prefer the compact work cache and the exact anchored files before rereading
shared repository context. When a concrete file, test, module, endpoint, class, or function
anchor already exists, the pipeline should not broad-scan the repository.

## Copy Package Later
To reuse this pipeline elsewhere, copy:
- `.github/skills/setup-repo-context/SKILL.md`
- `.github/skills/refresh-repo-context/SKILL.md`
- `.github/skills/analyze-story/SKILL.md`
- `.github/skills/implement-story/SKILL.md`
- `.github/skills/fix-bugs/SKILL.md`
- `.github/skills/unit-testing/SKILL.md`
- `.github/copilot-instructions.md`
- `.github/instructions/`
- `.sdlc/`
