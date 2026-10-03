# Context Rules

## Authoritative Work Cache
- `implementation-cache.json` is the authoritative machine-readable source for work scope,
	exact files, validation commands, standards references, and missing facts.
- Markdown work artifacts exist for human review. They should reference the cache instead of
	repeating file inventories, command lists, or repository facts already recorded there.

## Read Order
1. `.sdlc/work/<ID>/implementation-cache.json`
2. `.sdlc/work/<ID>/session.md`
3. Exact source files and tests listed in `implementation-cache.json`
4. `.sdlc/work/<ID>/implementation-plan.md` only when change intent or validation rationale is
	missing from the cache
5. `.sdlc/work/<ID>/story-context.md` only when business constraints, work relationship, or
	tracker context is missing from the cache
6. `.sdlc/work/<ID>/impact-map.md` only when adjacent dependencies or out-of-scope edges are
	missing from the cache
7. Optional `.sdlc/work/<ID>/requirement-analysis.md` only when unresolved questions remain
8. `.sdlc/context/*` only for explicit missing facts recorded in `implementation-cache.json`
9. Repository-wide search only when no concrete anchor exists after the earlier steps

## Context Exclusions
- Before any repository-wide search or directory scan, consult
	`.sdlc/context/context-exclusions.json`.
- Do not reference, search, summarize, or load any path matching its `excluded_paths` or
	`excluded_patterns` by default (generated output, dependencies, build artifacts, caches,
	logs, binaries, runtime noise).
- Access an excluded path only when the task explicitly requires it and no authoritative source
	alternative exists.
- Never treat configuration-bearing extensions (`*.json`, `*.xml`, `*.yaml`, `*.yml`,
	`*.properties`) as globally irrelevant; use them when they hold real project configuration or
	architecture information.
- This file is the single shared source for exclusions. Skills reference it rather than copying
	the list.

## Rules
- Cache first.
- Prefer exact files over folders, and folders over repository-wide search.
- If the input already provides a concrete file, test, stack trace, module, endpoint, class, or
	function anchor, do not broad-scan the repository.
- Read the smallest section or line range that can answer the next question.
- Do not reread external trackers during implementation, bug fixing, or unit testing.
- Do not reread full standards documents after bootstrap unless they changed.
- Keep `implementation-cache.json` compact:
	- `exact_source_files`: at most 5 paths unless the slice is truly broader
	- `exact_test_files`: at most 3 paths unless the slice is truly broader
	- `selected_standards`: at most 5 references unless required by policy
- Record only the commands that are expected to run for the scoped slice.
- Overwrite work artifacts fully when regenerating them.
- Never duplicate the same fact across multiple work files when one file already owns it.

## Standards Loading Order
Standards use a three-tier split to avoid repeated context (see
`.sdlc/context/standards-index.json`):
1. `implementation-cache.json` `selected_standards` — the standard ids relevant to this work.
2. `.github/instructions/standards/*.instructions.md` — compact rules that auto-apply via
	`applyTo` globs for the touched files; no extra reading required.
3. `standards/*.md` — the full source of truth, read only when an exact rule or missing detail
	is required.
Never reread the entire `standards/` folder by default. `/analyze-story` (and standalone
`/fix-bugs` or `/unit-testing`) selects the relevant standard ids from
`.sdlc/context/standards-index.json` into the cache; downstream stages rely on those ids plus
the auto-applied compact instruction files.

## Staleness
Refresh repository cache with `/refresh-repo-context` when repository structure, standards,
documentation, or build/test commands change.

Regenerate a story cache only when:
- story inputs or acceptance criteria changed
- impacted code changed enough to invalidate the story cache
- repository refresh changed facts the story depends on

Create or patch a standalone bug-fix cache when:
- the bug is not attached to an existing work folder
- the provided bug scope narrows to a different slice than the cached one
- the actual touched files or validation commands differ from the cached slice

Create or patch a standalone testcase cache when:
- the unit-test task is not attached to an existing work folder
- the provided target behavior or source anchor narrows to a different slice than the cached one
- the actual touched test files, validation commands, or coverage commands differ from the
	cached slice
