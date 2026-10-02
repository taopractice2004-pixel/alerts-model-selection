---
applyTo: "AlertService.Data/**/*.cs,AlertService.Data.SQL/**/*.cs,database/**/*.sql"
description: "Points Copilot to the data-access and migration rules. Loaded only when working on data-access or migration files."
---

# Data Instructions

<!-- /setup-repo-context replaces applyTo with this repository's real data-access and migration paths.
     Rules are not repeated here; they live in .sdlc/context/standards-summary.md. -->

Before writing or changing code in these files, apply the `Data Access` and `Security` sections of
`.sdlc/context/standards-summary.md`. Schema and migration changes need plan approval. Copy the
repository / data access example listed in section 6 of `.sdlc/context/project-profile.md`.
