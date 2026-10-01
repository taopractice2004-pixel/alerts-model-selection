---
applyTo: "**/*.html,**/*.css,**/*.scss"
description: "Compact AI-enforceable UI markup/styling rules. Full source: standards/ui-standards.md."
---

# UI Markup & Styling Rules (compact)

Full source of truth: `standards/ui-standards.md`. Read that file only when an exact rule or
detail not listed here is required.

- Declare the HTML5 doctype; set document language and character encoding explicitly; include a
  meaningful `<title>` and appropriate metadata.
- Use semantic elements (`<header>`, `<nav>`, `<main>`, `<section>`, `<article>`, `<figure>`,
  `<footer>`); use `<div>` only when no semantic element fits.
- Do not use deprecated presentational tags or `<br>` for layout; always provide useful `alt`
  text for images.
- Prefer semantic structure over ARIA; avoid unnecessary `role` attributes on semantic elements;
  do not rely on `title` attributes for accessibility.
- CSS: two-space indentation; lowercase hyphen-separated class names; keep selectors modular and
  low-specificity; avoid IDs and tag-heavy selectors for styling.
- Use shorthand properties where they improve readability; quote `url()`/`@import()` values
  consistently; minify/combine assets in production when the delivery setup expects it.
