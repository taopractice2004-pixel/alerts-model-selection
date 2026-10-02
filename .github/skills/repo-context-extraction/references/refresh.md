# Refresh Mode

Apply the Rules in `SKILL.md` to everything refreshed.

1. Take `refreshedAtCommit`, or `builtAtCommit` if never refreshed. Changed paths = the output of
   `git diff --name-only <commit>..HEAD` plus the paths in `git status --short` (uncommitted
   changes count). Without Git or a commit, refresh only the sections the developer names.
2. A section is stale when a changed path matches one of its `sources`; sections the developer names
   are stale too. `commands` are also stale when a `/unit-testing` log entry since the last refresh
   verified or corrected a command that `manifest.json` still lists as `NOT_RUN (unverified)`. If nothing is stale, stop and report that nothing needed refreshing.
3. Refresh only stale parts, with the setup-mode rules:
   - `contextPolicy` (and profile section 9) for changed paths it does not yet cover: new top-level
     folders, new build output, new generated or vendor code
   - stale sections of `project-profile.md` and `standards-summary.md`
   - `commands`: set to `VERIFIED` (or corrected) every command a `/unit-testing` log entry records as
     passed or discovered; when build, package, or test files changed, re-discover only the affected
     commands without running them (`NOT_RUN (unverified)`)
   - `applyTo` paths and the Project line, when folder structure changed
4. **GREENFIELD with `contextMaturity` `INITIAL`** once real project code exists: run the EXISTING
   setup steps for the parts that now exist (structure, modules, pattern examples from the real
   files, testing context, commands, `applyTo` paths). Keep `[REQ]` facts and resolved or open
   decisions in sections 15-16. Set `contextMaturity` `ESTABLISHED` when patterns, build, and tests
   exist (otherwise keep `INITIAL`). Never change `repositoryMode`; never edit application code.
5. Update `manifest.json`: `refreshedAtUtc`, `refreshedAtCommit`, updated `commands`, changed
   `sources`, `contextMaturity`, `establishment`.
