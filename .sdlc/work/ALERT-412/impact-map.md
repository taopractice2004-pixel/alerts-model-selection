# ALERT-412 Impact Map

## In Scope
- Trends endpoint route and query handling.
- Service-layer response shaping for complete UTC day buckets.
- Repository aggregation for per-day/per-severity counts.
- Focused tests for boundary validation and response correctness.

## Adjacent Dependencies
- Existing severity enum ordering conventions used by summary behavior.
- Existing ASP.NET Core query model-validation pipeline and `ValidationProblemDetails` responses.
- Existing date/time handling seams used by tests.

## Out of Scope
- Changes to existing summary endpoint behavior.
- Unrelated endpoint or schema refactors.
- Broad test-suite expansion beyond ALERT-412 behavior.
