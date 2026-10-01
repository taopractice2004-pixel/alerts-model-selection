# Stage Rules

Each stage below is owned by a skill in `.github/skills/<stage>/SKILL.md` (the authoritative
procedure). The `.github/prompts/*.prompt.md` files are thin `prompt-*` wrappers that delegate
to the matching skill.

Every stage ends with:

```
CURRENT STAGE:
STATUS:
FILES CREATED/UPDATED:
SUMMARY:
NEXT RECOMMENDED COMMAND:
```

## /setup-repo-context
- Writes `.sdlc/context/repo-profile.md`
- Writes `.sdlc/context/repository-map.md`
- Writes `.sdlc/context/project-docs-index.md`
- Writes `.sdlc/context/standards-summary.md`
- Writes `.sdlc/context/standards-index.json`
- Writes `.sdlc/context/context-exclusions.json`
- Updates the `## Repository Context Exclusions` section in `.github/copilot-instructions.md`
- Writes `.sdlc/context/context-manifest.json`

## /refresh-repo-context
- Updates only stale or changed files under `.sdlc/context/`
- Recalculates `context-exclusions.json` (and the exclusions section in
	`.github/copilot-instructions.md`) only when structure, technology, `.gitignore`, build
	configuration, or generated-output patterns materially changed

## /analyze-story
- Always writes `session.md`
- Always writes `story-context.md`
- Always writes `implementation-plan.md`
- Always writes `impact-map.md`
- Always writes `implementation-cache.json`
- Writes `requirement-analysis.md` only for `AMBIGUOUS` cases
- `implementation-cache.json` is the authoritative machine-readable output for later stages
- Records relevant standards as `selected_standards` ids from
	`.sdlc/context/standards-index.json`; downstream stages rely on those ids plus the auto-applied
	compact `.github/instructions/standards/*.instructions.md` files
- Other story artifacts should stay compact and must not duplicate exact file inventories or
	command lists already owned by the cache

## /implement-story
- Reads `implementation-cache.json` first, then `session.md`, then only the exact source files
	and tests needed for the slice
- Reads other work artifacts only for missing intent, constraints, or adjacent dependency facts
- Reads shared repository context only for explicit missing facts recorded in the cache
- Writes `changes.md`
- Updates `session.md`
- May update `implementation-cache.json` and `impact-map.md` if the touched slice changed

## /fix-bugs
- Reads the minimal relevant work cache first when it exists
- Supports standalone bug-fix work without requiring a prior story-analysis stage
- Creates `session.md`, `story-context.md`, `implementation-plan.md`, `impact-map.md`, and
	`implementation-cache.json` when invoked standalone without an existing work cache
- Writes `changes.md`
- Updates `session.md`
- May update `implementation-cache.json` and `impact-map.md` if the touched slice changed
- Writes `requirement-analysis.md` only when the bug is ambiguous, risky, or blocked
- Must not broad-scan the repository when the bug input already provides a concrete anchor

## /unit-testing
- Reads the minimal relevant work cache first when it exists
- Supports standalone testcase work without requiring a prior story-analysis stage
- Creates `session.md`, `story-context.md`, `implementation-plan.md`, `impact-map.md`, and
	`implementation-cache.json` when invoked standalone without an existing work cache
- Writes `changes.md`
- Updates `session.md`
- May update `implementation-cache.json` and `impact-map.md` if the touched slice changed
- Writes `requirement-analysis.md` only when the unit-test target is ambiguous, risky, or blocked
- Runs the narrowest unit-test command and scoped coverage command available for the target slice
- Must not broad-scan the repository when the testcase input already provides a concrete anchor
