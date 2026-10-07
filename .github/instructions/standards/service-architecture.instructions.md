---
applyTo: "**/*Service.cs,**/*Client.cs,**/Program.cs,**/Startup.cs"
description: "Compact AI-enforceable service architecture rules. Full source: standards/service-architecture-standards.md."
---

# Service Architecture Rules (compact)

Full source of truth: `standards/service-architecture-standards.md`. Read that file only when an
exact rule or detail not listed here is required. Review limits and quality checks are in
`code-review.instructions.md`.

- Design services around a single business capability; preserve autonomy for build, test, and
  deploy; give each service ownership of its own data store where practical.
- Prefer asynchronous communication when it improves resilience and reduces coupling.
- Keep REST-facing classes separate from service-layer business logic; route all persistence
  through the service layer, never directly from controllers or clients.
- Use dedicated clients for external dependencies; keep migration/reconciliation flows isolated
  from normal request paths.
- Maintain a clear boundary between external API contracts and internal domain models.
- Use shared `HttpClient` patterns, not per-request disposal; configure dependency injection
  correctly.
- Reflect contract changes in Swagger/OpenAPI output.
