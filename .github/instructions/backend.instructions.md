---
applyTo: "AlertService.API/**/*.cs,AlertService.Common/**/*.cs,AlertService.DTO/**/*.cs,AlertService.Models/**/*.cs,AlertService.Data/**/*.cs"
description: "Points Copilot to the backend and API rules. Loaded only when working on backend files."
---

# Backend Instructions

<!-- /setup-repo-context replaces applyTo with this repository's real backend paths.
     Rules are not repeated here; they live in .sdlc/context/standards-summary.md. -->

Before writing or changing code in these files, apply these sections of
`.sdlc/context/standards-summary.md`: `General`, `Backend / API`, `Security`, and `Integrations`
when the code calls an external system. Copy the matching example file listed in section 6 of
`.sdlc/context/project-profile.md`.
