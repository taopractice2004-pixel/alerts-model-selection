# UI Standards

## Markup Guidelines

- Use semantic elements such as `<header>`, `<nav>`, `<main>`, `<section>`, `<article>`, `<figure>`, and `<footer>` where they fit the content.
- Use `<div>` only when no semantic element is appropriate.
- Do not use deprecated presentational tags; move presentation concerns into CSS.
- Do not use `<br>` for layout.
- Always provide useful `alt` text for images.
- Keep markup readable and consistently formatted.

## Accessibility And Readability

- Prefer semantic structure over ARIA where native HTML already provides meaning.
- Avoid unnecessary `role` attributes on semantic elements like `<nav>`.
- Use link and control text that remains meaningful without tooltip behavior.
- Be careful with `title` attributes because they are not reliably accessible across devices.

## CSS Guidelines

- Use two-space indentation and keep formatting consistent across files.
- Prefer lowercase, hyphen-separated class names.
- Use shorthand properties where it improves readability.
- Keep selectors modular and low-specificity.
- Avoid IDs and tag-heavy selectors for styling.
- Use comments to explain intent, not obvious implementation details.
- Quote `url()` and `@import()` values consistently.
- Minify and combine CSS and JS assets in production builds when the delivery setup expects it.