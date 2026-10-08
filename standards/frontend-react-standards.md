# Frontend React Standards

## Core Principles

- Keep components small, focused, and easy to test.
- Prefer functional components when local state or lifecycle complexity is not required.
- Follow existing project patterns for data fetching, state management, and API integration.
- Avoid hardcoded UI strings; use localization or shared message files.

## Component Design

- Define prop types or the project-equivalent contract for every component.
- Use clear PascalCase names for component files and exported components.
- Minimize logic inside render paths; move repeated or heavy logic into helpers.
- Do not update state inside loops.
- Remove listeners and subscriptions during teardown.
- Use unique testing hooks such as stable `data-testid` values where the team expects them.

## Event And State Handling

- Keep handler names consistent and intention-revealing.
- Destructure props and state where it improves clarity.
- Validate objects before reading nested values.
- Keep concerns separated between presentational UI and data-loading containers.

## Styling Rules

- Avoid inline styles except for truly dynamic values.
- Prefer styled components or the team-approved CSS-in-JS approach.
- Use consistent naming, shared units, and low-specificity selectors.
- Prefer flexbox for layout when appropriate.
- Avoid `!important`.
- Use `transform` for motion rather than animating layout properties such as `width`, `height`, `top`, or `left`.

## Code Review Checklist

General code-quality and React review rules (hooks, keys, state, duplication, test quality,
coverage) are in `code-review-standards.md`.

- Components are reusable, reasonably sized, and consistent with existing patterns.
- API calls are implemented through the approved side-effect/data layer, not directly in presentation code.
- Unused dependencies and packages are removed.
- Naming for files, variables, and translations is consistent.
- CSS and JS formatting pass the project linters and formatters.
