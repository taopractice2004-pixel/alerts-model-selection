---
name: repo-context-extraction
description: "Builds or refreshes the cached repository context in .sdlc/context (project-profile.md, standards-summary.md, manifest.json) for an EXISTING repository (discovered from code) or a GREENFIELD repository (bootstrapped from a requirement document and standards). Use for /setup-repo-context and /refresh-repo-context."
user-invocable: false
---

# Repository Context Extraction

Setup takes the repository mode from the prompt:
- `EXISTING`: discover what exists (steps below).
- `GREENFIELD`: follow [references/greenfield.md](./references/greenfield.md) instead (loaded only
  for a new repository).

Refresh updates only what changed (see the end of this file).

## Rules
- Build a MAP of the repository, not a copy. Name important folders, modules, and one example file
  per pattern. Never list every file, class, or method; never paste source or document bodies.
- One home per fact: repository facts in `project-profile.md`, coding rules in
  `standards-summary.md`, commands, context policy, and source paths in `manifest.json`.
- Read efficiently: build and package files, the root README, `docs/` indexes, configuration files,
  and 1-2 representative files per layer. Never read the whole repository.
- Never record secrets, passwords, tokens, connection strings, or other sensitive values; record
  configuration file names and environment variable names only.
- Write `TO_BE_DISCOVERED` or `NOT_APPLICABLE` instead of guessing.
- Never overwrite the developer-maintained sections 10-13 of `project-profile.md` (Domain Glossary,
  Known Pitfalls, Story Conventions, Shared Files) unless the repository documents them at setup.

## Setup: EXISTING repository
1. **Project profile.** Fill `.sdlc/context/project-profile.md`, keeping its section structure
   (section 0: mode `EXISTING`, maturity `ESTABLISHED`; sections 15-16: `NOT_APPLICABLE`):
   1. Overview: what the application does; main responsibilities.
   2. Technology Stack: languages, frameworks, important versions, build and package tools, data
      store, test framework.
   3. Repository Structure: important folders only, with purpose (about 15 rows at most).
   4. Architecture And Flow: style, how layers or components interact, the main request or data flow,
      entry points and composition root, architecture rules visible in the code.
   5. Modules: name, path, responsibility for each major module, component, or service.
   6. Pattern Examples: one representative existing file per common change (API endpoint or
      controller, service, repository or data access, external integration client, UI component,
      unit test). Pick a clean, typical, recently maintained file, not an exception.
   7. Testing Context: test framework, mocking and assertion libraries, test location and naming,
      one representative test file, coverage tooling and target if already configured.
   8. Configuration: configuration files and purpose, environment variable names and purpose, where
      secrets come from. No values.
   9. Context Policy: a short summary of step 2 (main paths per category).
   14. Key Documents: architecture, setup, or domain documents worth knowing (path and purpose).
2. **Context policy.** Classify paths by purpose. List the top-level folders and notable files, then
   go one or two levels deeper where a folder mixes purposes. Decide each category from what the path
   is in this repository, not from its extension alone. Evidence: output folders named in build and
   project files, `.gitignore` entries, "auto-generated" or "do not edit" headers, lock files, vendor
   or third-party folders, CODEOWNERS or docs that assign ownership, whether the main build references
   the code.
   - `normalPaths`: source and test areas normally relevant to development.
   - `readWhenRelevant`: configuration, infrastructure, project, package, build, CI/CD,
     documentation, schema, script, and similar files needed only for some tasks.
   - `skipByDefault`: dependencies, generated output, binaries, caches, build and coverage
     artifacts, other low-value paths.
   - `doNotModify`: generated, protected, vendor, legacy, or team-owned areas that need developer
     approval to change. A path may also appear in one other category.
   Use globs relative to the repository root, a short reason per entry, folders rather than files
   where possible.
   Always include: `standards/` in `skipByDefault` and `doNotModify` (only setup and refresh read it;
   stages use `standards-summary.md`), and the pipeline's own files (`.github/prompts/`,
   `.github/agents/`, `.github/skills/`, `.sdlc/templates/`, `.sdlc/work/`) in `skipByDefault`, so
   searches find project code, not pipeline text. Stages still open pipeline files by exact path.
3. **Coding rules.** Edit `.sdlc/context/standards-summary.md` in place:
   - Replace each `Project conventions (observed)` line with what the code actually does: naming,
     reuse, dependency injection, error handling, async, logging; for API, database, and
     integrations, only conventions that exist here (routes, versioning, error payloads, ORM or SQL
     style, migration tool, client patterns).
   - Keep the condensed company rules that apply; add missing rules from `standards/` as short
     actionable lines; never copy whole documents.
   - On conflict, keep the project convention and note it on that line.
   - Remove sections for areas the repository does not have.
4. **Discover commands (do not run them).** For restore, build, type check, lint, unit test,
   integration test (only if an automated suite exists), and coverage: read the solution, project,
   package, and build files, scripts, README, and CI configuration to find the command, its working
   directory, the narrow variant (one project; one test file, class, or filter), and the quiet
   option. Record each as `NOT_RUN (unverified)`, or `NOT_CONFIGURED` when none exists. Setup never
   builds, restores, or runs tests; `/unit-testing` verifies commands on first use.
5. **Manifest.** Write `.sdlc/context/manifest.json`: `status` = `BUILT`, `repositoryMode` =
   `EXISTING`, `contextMaturity` = `ESTABLISHED`, `builtAtUtc`,
   `builtAtCommit` (`git rev-parse HEAD`, or null without git), `commands` (step 4), `contextPolicy`
   (step 2), and for each section the repository paths or globs it came from (`sources`).
6. **Instruction files.** In `.github/copilot-instructions.md` fill only the Project line (what the
   application is and its main stack). In `.github/instructions/backend`, `frontend`, `data`, and
   `tests` `.instructions.md`, set `applyTo` to this repository's real paths as comma-separated globs,
   or `__not_used__/**` when the area does not exist. Add no rules to those files.

## Refresh mode
Follow [references/refresh.md](./references/refresh.md) (loaded only for `/refresh-repo-context`).
