# Workflow

## Commands
1. `/setup-repo-context`
2. `/refresh-repo-context`
3. `/analyze-story`
4. `/implement-story`
5. `/fix-bugs`
6. `/create-testcases`

## Three Story Cases
- `SIMPLE`: create compact story cache without `requirement-analysis.md`
- `AMBIGUOUS`: create compact story cache and `requirement-analysis.md`
- `STALE_REPLAN`: regenerate the story cache because Jira or repository facts changed

## Default Flow
1. Run `/setup-repo-context` once.
2. Run `/analyze-story` for a Jira story.
3. Review the compact artifacts if needed. Treat `implementation-cache.json` as the
	authoritative machine-readable scope.
4. Run `/implement-story`.

## Bug Fix Flow
1. Run `/setup-repo-context` once.
2. Run `/fix-bugs` with a bug ID, bug description, relationship mode, and one scoping hint.
3. Review the focused bug-fix artifacts and changed files.

## Testcase Flow
1. Run `/setup-repo-context` once.
2. Run `/create-testcases` with a work ID, relationship mode, target behavior, and one scope
	anchor.
3. Review the created or updated unit test files, validation results, and scoped coverage
	results.

## Refresh Flow
1. Run `/refresh-repo-context` when repository context changed.
2. Re-run `/analyze-story` only for stories whose cache is stale.
3. Run `/implement-story`.

## Integration Model
- `/fix-bugs` can run as a standalone stage for already-implemented code.
- `/fix-bugs` can also attach to the currently active story work folder when the defect is part
	of the in-progress story.
- `/fix-bugs` reuses the compact work cache under `.sdlc/work/<ID>/` and should not trigger a
	broad repository scan when the bug input or existing cache already narrows the slice.
- `/create-testcases` can run after `/implement-story`, after `/fix-bugs`, or as a standalone
	unit-test stage for existing code.
- `/create-testcases` reuses the compact work cache under `.sdlc/work/<ID>/`, updates an
	existing nearby unit test file when one exists, creates a new unit test file only when needed,
	and runs the narrowest unit-test and coverage commands for the requested scope.
- Later stages should reuse `implementation-cache.json` first, then read only the exact source
	and test files needed for the slice.
- Human-readable artifacts should stay thin and should not duplicate exact file inventories or
	command lists already owned by the cache.
- No stage should broad-scan the repository when a concrete file, test, module, endpoint,
	class, or function anchor already exists.
