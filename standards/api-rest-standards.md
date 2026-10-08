# REST API Standards

## Contract First

- Define every REST API using OpenAPI 3.x.
- Keep the API specification in source control alongside the implementation.
- Prefer YAML for the contract document.
- Update the specification version whenever the contract changes.

## Resource And URI Design

- Model resources as nouns, not verbs.
- Use predictable URI patterns such as `/domain/v1/resources`.
- Use hyphenated resource names instead of camelCase.
- Use HTTP methods to express the action rather than encoding operations in the path.
- Represent hierarchy only when the relationship is real and useful.

## HTTP Method Guidance

- `POST` creates new resources and typically returns `201 Created`.
- `GET` reads data and must not change resource state.
- `PUT` replaces or atomically updates a full resource.
- `PATCH` applies partial or workflow-specific updates.
- `DELETE` removes a resource and should respond clearly when the resource no longer exists.

## Responses And Errors

- Use JSON as the default request and response format unless there is a justified alternative.
- Return arrays for search endpoints, including empty arrays when there are no results.
- Standardize error payloads using an RFC7807-style structure.
- Use clear status codes such as `400`, `403`, `404`, and `500` consistently.
- Include correlation identifiers for cross-service traceability.

## Versioning With .NET

- Use URL path versioning, for example `v1`, `v1.2`, or `v1.2.3`.
- Prefer domain-level versioning over per-endpoint versioning when the whole API evolves together.
- Separate versions using controller versioning.
- Use version interleaving when versions are few and changes are modest.
- Use namespace-based version separation when contracts diverge significantly.
- Version DTOs/contracts as well as controllers so one version cannot accidentally break another.

## Backward Compatibility

- Treat breaking changes as major version changes.
- Keep older versions available long enough for clients to migrate.
- Mark deprecated behavior clearly before removing it.
- Communicate breaking changes in advance and publish the updated contract early.