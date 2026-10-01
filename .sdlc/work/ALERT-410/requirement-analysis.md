# Requirement Analysis: ALERT-410

Classified `AMBIGUOUS`: the acceptance criteria describe required behavior but leave several
implementation-shape decisions open. Resolved assumptions below (also recorded in
`implementation-cache.json` → `design_decisions`) are conservative and reversible; flag during
`/implement-story` if any prove incorrect.

## Open Questions Resolved By Assumption
1. **Join table shape** — Acceptance criteria says "new join table/migration" but not whether to
   use EF Core's implicit skip-navigation many-to-many or an explicit join entity. Resolved:
   explicit `AlertTag` entity + configuration class, matching this repo's existing pattern of one
   explicit `IEntityTypeConfiguration<T>` per entity (`AlertConfiguration.cs`).
2. **Tag scope (global vs per-alert)** — Not stated whether identical tag text across different
   alerts should share one `Tag` row or create independent rows. Resolved: global/shared `Tag`
   table (standard normalized many-to-many design per `database.instructions.md` 3NF guidance),
   looked up case-insensitively so "Urgent" and "urgent" on different alerts reuse the same row.
3. **Case-insensitive dedup mechanism** — Resolved: case-insensitive comparison in the query
   layer (`OrdinalIgnoreCase`), mirroring the existing `ToLower()` search-filter pattern in
   `AlertRepository.GetAllAsync`, plus a unique index as a DB-level backstop.
4. **10-tag cap enforcement** — Resolved: reject the entire POST request (400) if the resulting
   distinct tag count would exceed 10; no partial application.
5. **DELETE 404 semantics** — Acceptance criteria explicitly lists two 404 cases (missing alert,
   missing tag assignment); both are modeled as a single 404 response from the controller, as with
   existing `Update`/`Deactivate`/`Delete` actions.
6. **AlertResponse.Tags shape** — Resolved: list of tag name strings (not tag objects/ids), to
   keep the contract minimal and consistent with the rest of `AlertResponse`.

## Items Still Flagged (not assumed away)
- Whether `database/02_AlertServiceDb_Migrations.sql` must be regenerated as part of this change.
- Whether SQL Server's default collation (assumed case-insensitive) is confirmed for this
  environment, versus needing an explicit normalized/lowercase shadow column for cross-provider
  (SQL Server vs Sqlite/InMemory test) consistency.

Both are recorded as `TO_BE_DISCOVERED` in `implementation-cache.json` → `missing_facts` and
should be confirmed or made a conservative, explicit implementation choice during
`/implement-story` rather than causing a block.
