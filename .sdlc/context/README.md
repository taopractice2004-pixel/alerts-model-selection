# Context Starter Folder

This folder is a reusable starter version of `.sdlc/context/` for the current compact
pipeline.

It is meant to be copied into a new repository before the first run of
`/setup-repo-context`. The files here are starter shapes only. They should describe what the
current pipeline expects, without carrying facts from this repository.

## Files In This Folder
- `repo-profile.md`: compact repository facts such as stack, architecture, build/run/test
  commands, runtime shape, and known gaps.
- `repository-map.md`: compact navigation map of the important application areas, modules,
  packages, services, apps, or libraries.
- `project-docs-index.md`: compact index of documents, standards, and pipeline docs that may
  matter for story analysis.
- `standards-summary.md`: compact standards catalog and selection rules.
- `standards-index.json`: routing index mapping each standard id to its full source file,
  compact instruction file, and applicable file types (no rule text).
- `context-exclusions.json`: repository-specific list of non-authoritative paths and patterns
  (generated output, dependencies, build artifacts, caches, logs, binaries) that stages should
  not search or load by default. Computed by `/setup-repo-context` from detected technologies,
  `.gitignore`, and actual structure. Never excludes normal source or config files by extension.
- `context-manifest.json`: machine-readable starter manifest that says repository context has
  not been built yet.

## How To Use It
- Copy this folder as `.sdlc/context/` into a new repository.
- Run `/setup-repo-context` to replace these starter values with real repository facts.
- Run `/refresh-repo-context` later only when those facts become stale.

## Do Not Do This
- Do not keep real context from one project and reuse it in another.
- Do not treat these starter files as completed cache output.