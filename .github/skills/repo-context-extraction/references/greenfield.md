# Setup: GREENFIELD repository

Bootstrap context only from authoritative sources: the requirement document, company standards, and
architecture or technical decisions the developer supplied. Never pretend code, patterns, or commands
were discovered. Apply the Rules in `SKILL.md`.

Mark every fact with its source: `[REQ]` requirement document, `[STD]` company standards, `[DEV]`
developer-provided, `[PROPOSED]` suggested from known requirements and standards but not implemented.
Use `NOT_ESTABLISHED` for what cannot exist yet (no code), `NOT_CONFIGURED` for commands, and
`OPEN_DECISION: <question>` for important decisions no source settles. Never turn these into
guessed facts.

1. **Requirement document.** Read it once. If it cannot be read, stop with
   `BLOCKED_MISSING_INFORMATION` and ask for an accessible source; never invent requirements. Write a
   compact summary to `project-profile.md` section 15 (purpose, functional scope, actors, key rules,
   integrations, non-functional and security requirements, domain terms) so later stages use the
   summary instead of rereading the document.
2. **Project profile.** Fill `.sdlc/context/project-profile.md`:
   - 0 Repository Mode: `GREENFIELD`, maturity `INITIAL`, requirement source path.
   - 1 Overview, 2 Technology Stack, 4 Architecture And Flow: only what is `[REQ]`, `[STD]`, or
     `[DEV]`; anything else `OPEN_DECISION` or `[PROPOSED]`.
   - 3 Repository Structure, 5 Modules: whatever already exists (often only pipeline files), plus
     required areas marked `[PROPOSED]`.
   - 6 Pattern Examples: `NOT_ESTABLISHED` for every row. Never name or create example files.
   - 7 Testing Context: test expectations from requirements and standards; existing conventions
     `NOT_ESTABLISHED`.
   - 8 Configuration, 9 Context Policy, 14 Key Documents: what exists now (including the requirement
     document).
   - 10 Domain Glossary: terms from the requirement document.
   - 16 Open Decisions: every `OPEN_DECISION`, each with the source that raised it.
3. **Context policy.** Classify only paths that exist, with the same rules as existing mode
   (including the always-included `standards/` and pipeline entries). Add `readWhenRelevant` for the
   requirement document.
4. **Coding rules.** Write `standards-summary.md` from `standards/` only; every `Project conventions
   (observed)` line is `NOT_ESTABLISHED`. Keep sections for areas the requirements say will exist.
5. **Commands.** Do not run or guess build or test commands; every command is `NOT_CONFIGURED`
   unless the developer supplied it (then `NOT_RUN (supplied, unverified)`).
6. **Manifest.** Write `manifest.json`: `status` `BUILT`, `repositoryMode` `GREENFIELD`,
   `contextMaturity` `INITIAL`, `requirementSource` (path and read time), `establishment`
   `NOT_ESTABLISHED` for patterns and tests, `builtAtCommit` (null without Git), `commands`,
   `contextPolicy`, section `sources`.
7. **Instruction files.** Fill the Project line in `copilot-instructions.md` from `[REQ]`/`[DEV]`
   facts. Leave the default `applyTo` patterns in `.github/instructions/` until real paths exist.

Never create application code, business logic, controllers, services, components, tests, or project
scaffolding in this stage; they are created only through `/analyze-story`, approval, and
`/implement-story`.
