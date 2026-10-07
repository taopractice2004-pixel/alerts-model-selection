# Service Architecture Standards

## Microservice Design Principles

- Design services around business capabilities using domain-driven design.
- Keep each service focused on a single responsibility.
- Preserve service autonomy for development, testing, and deployment.
- Prefer asynchronous communication when it improves resilience and reduces coupling.
- Give each service ownership of its own data store where practical.

## Internal Layering For .NET APIs And BFFs

- Keep REST-facing classes separate from service-layer business logic.
- Route all persistence through the service layer rather than bypassing it from controllers or clients.
- Use dedicated clients for external dependencies.
- Keep migration or reconciliation flows isolated from normal request-processing paths.
- Maintain a clear boundary between external API contracts and internal domain models.

## API And BFF Architecture Checks

General code-quality checks (coverage, hardcoded values, null and exception handling, logging,
blocking async calls, sensitive data, unit tests) are in `code-review-standards.md`.

- `HttpClient` usage follows shared-instance patterns rather than per-request disposal.
- Dependency injection is configured correctly.
- Contract changes are reflected in Swagger or OpenAPI output.
