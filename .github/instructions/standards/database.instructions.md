---
applyTo: "**/*.sql,**/Migrations/**,**/*DbContext.cs,**/*Repository.cs"
description: "Compact AI-enforceable database rules. Full source: standards/database-standards.md."
---

# Database Rules (compact)

Full source of truth: `standards/database-standards.md`. Read that file only when an exact rule
or detail not listed here is required.

- Select only required columns; never `SELECT *`.
- Prefer set-based logic over cursors/loops; keep transactions short (start late, finish early)
  and multi-step changes atomic.
- Avoid unnecessary `ORDER BY` and unneeded projection columns; avoid expensive scalar/TVF use
  without a clear need.
- Treat EF/LINQ-generated access with the same scrutiny as handwritten SQL; consider indexing
  and review execution plans for performance-sensitive changes.
- Default new tables to 3NF unless an approved reason to denormalize exists.
- Naming: PascalCase tables/columns; views `v_`; indexes `IX_[Table]_[Columns]`; primary keys
  `PK_[Table]_[Column]`; foreign keys `FK_[Column]_[RefTable]_[RefColumn]`; stored procedures
  `p_[Entity]_[Verb]` (e.g. `p_Customer_Get`).
- Prepare rollback scripts/backups for production-impacting or high-risk DML changes.
