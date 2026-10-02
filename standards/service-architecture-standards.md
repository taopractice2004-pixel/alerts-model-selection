# Service Architecture Standards

## Microservice Design Principles

- Design services around business capabilities using domain-driven design.
- Keep each service focused on a single responsibility.
- Preserve service autonomy for development, testing, and deployment.
- Prefer asynchronous communication when it improves resilience and reduces coupling.
- Give each service ownership of its own data store where practical.

## Operational Guidance

- Containerize services for consistent deployment and efficient runtime isolation.
- Build with security in mind: encrypted transport, restricted access, vulnerability scanning, and strong authentication.
- Favor immutable deployment patterns where possible.
- Support fast delivery with CI/CD and DevOps collaboration.
- Use micro frontends only when separate UI ownership and deployment genuinely help.

## Internal Layering For .NET APIs And BFFs

- Keep REST-facing classes separate from service-layer business logic.
- Route all persistence through the service layer rather than bypassing it from controllers or clients.
- Use dedicated clients for external dependencies.
- Keep migration or reconciliation flows isolated from normal request-processing paths.
- Maintain a clear boundary between external API contracts and internal domain models.

## API And BFF Review Checklist

- New code coverage meets the team threshold.
- No hardcoded values, unnecessary usings, or commented-out code remain.
- Null handling, exception handling, and logging are implemented deliberately.
- Async code is used correctly without blocking calls such as `Task.Wait()` or `Task.Result`.
- `HttpClient` usage follows shared-instance patterns rather than per-request disposal.
- Dependency injection is configured correctly.
- Contract changes are reflected in Swagger or OpenAPI output.
- Unit tests exist for the changed behavior and follow a clear naming convention.
- No sensitive information is exposed in logs, exceptions, or configuration.