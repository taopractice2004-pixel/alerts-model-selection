# SDLC Pipeline

This is a fresh pipeline package designed to reduce Copilot credit usage.

## Commands
- `/setup-repo-context`
- `/refresh-repo-context`
- `/analyze-story`
- `/implement-story`
- `/fix-bugs`
- `/unit-testing`

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
- `.github/prompts/setup-repo-context.prompt.md`
- `.github/prompts/refresh-repo-context.prompt.md`
- `.github/prompts/analyze-story.prompt.md`
- `.github/prompts/implement-story.prompt.md`
- `.github/prompts/fix-bugs.prompt.md`
- `.github/prompts/unit-testing.prompt.md`
- `.github/agents/requirement-analyst.agent.md`
- `.github/agents/developer.agent.md`
- `.github/agents/bug-fixer.agent.md`
- `.github/agents/test-author.agent.md`
- `.sdlc/`
