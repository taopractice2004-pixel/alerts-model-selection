---
applyTo: "**/*Controller.cs,**/openapi*.yaml,**/openapi*.yml,**/swagger*.yaml,**/swagger*.yml,**/*.openapi.yaml,**/*.openapi.yml"
description: "Compact AI-enforceable REST API rules. Full source: standards/api-rest-standards.md."
---

# REST API Rules (compact)

Full source of truth: `standards/api-rest-standards.md`. Read that file only when an exact rule
or detail not listed here is required.

- Define every REST API with OpenAPI 3.x (prefer YAML), kept in source control and versioned
  whenever the contract changes.
- Model resources as nouns with predictable URIs like `/domain/v1/resources`; use hyphenated
  resource names; express actions via HTTP methods, not the path.
- `POST` creates (return `201`), `GET` reads (no state change), `PUT` replaces, `PATCH` partial
  update, `DELETE` removes.
- Use JSON by default; return arrays (including empty) for search endpoints.
- Standardize errors with an RFC7807-style structure; use consistent status codes
  (`400`, `403`, `404`, `500`); include correlation identifiers.
- Use URL path versioning (`v1`, `v1.2`); version DTOs/contracts alongside controllers.
- Treat breaking changes as major version changes; deprecate clearly before removal.
