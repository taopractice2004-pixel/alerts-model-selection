# BUG-301 Story Context

- Relationship mode: standalone
- Bug scope: SQL-backed severity sorting for GET /api/alerts when sortBy=severity.
- Endpoint reproduction: GET /api/alerts?sortBy=severity&sortDirection=desc
- Expected order: Critical, High, Medium, Low
- Risk: provider-specific string ordering can diverge from the business severity rank because Severity is stored as a string.
- Primary anchor: AlertRepository severity sorting path.
- Applicable standards: coding, backend-dotnet, service-architecture, and database standards from .sdlc/context/standards-summary.md.
- Security note: no new trust boundary or secret handling changes; keep behavior scoped to deterministic query ordering.
