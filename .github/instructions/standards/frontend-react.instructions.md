---
applyTo: "**/*.tsx,**/*.jsx,**/*.ts,**/*.js"
description: "Compact AI-enforceable React/frontend rules. Full source: standards/frontend-react-standards.md."
---

# Frontend React Rules (compact)

Full source of truth: `standards/frontend-react-standards.md`. Read that file only when an exact
rule or detail not listed here is required. Review limits and React review rules are in
`code-review.instructions.md`.

- Keep components small, focused, and testable; prefer functional components unless lifecycle
  or local state complexity requires otherwise.
- Define prop types (or the project-equivalent contract) for every component.
- Use PascalCase for component files and exported components.
- Do not update state inside loops; minimize logic in render paths (move heavy logic to helpers).
- Remove listeners and subscriptions on teardown.
- Validate objects before reading nested values; destructure props/state for clarity.
- Separate presentational UI from data-loading containers; make API calls through the approved
  side-effect/data layer, not directly in presentation code.
- Avoid hardcoded UI strings; use localization or shared message files.
- Avoid inline styles except for truly dynamic values; prefer the team-approved CSS-in-JS.
- Avoid `!important`; animate with `transform`, not layout properties (`width`/`height`/`top`/`left`).
- Remove unused dependencies; keep stable `data-testid` hooks where expected.
