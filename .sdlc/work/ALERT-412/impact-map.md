# Impact Map — ALERT-412

## Primary Slice
- New trends endpoint surface in `AlertsController`.
- Service orchestration for trends response shaping and zero-fill behavior.
- Repository aggregation query over `CreatedDate` and `Severity` for N-day UTC range.
- Trends DTO contract for request and response payload structure.

## Adjacent Dependencies
- Existing summary severity convention (`Low`, `Medium`, `High`, `Critical`) used as ordering
  reference.
- Existing ASP.NET Core model-binding and data-annotation validation behavior for query
  parameters (`ValidationProblemDetails` on invalid input).

## Out Of Scope
- Changes to create/update/delete/deactivate/tag flows.
- Changes to summary endpoint shape or semantics.
- Non-UTC trend grouping or arbitrary timezone support.

## Cache Reference
- Exact touched files and validation commands are owned by `implementation-cache.json`.
