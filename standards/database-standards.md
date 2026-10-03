# Database Standards

## Review Expectations

- Review schema and design changes with the DBA team before implementation.
- Discuss database-related test cases and impacted areas with QA.
- Prepare rollback scripts and rollback plans for production-impacting changes.
- Take backups before high-risk DML operations.
- Consider indexing as part of schema, stored procedure, and EF query changes.
- Review execution plans for performance-sensitive query changes.

## Query And Procedure Guidelines

- Select only required columns; do not use `SELECT *`.
- Prefer set-based logic over cursors and procedural loops.
- Use transactions carefully: start late, finish early, and keep multi-step data changes atomic.
- Avoid unnecessary `ORDER BY` clauses and unnecessary columns in projections.
- Avoid expensive scalar functions and table-valued functions unless there is a clear need.
- Treat EF and LINQ-generated database access with the same scrutiny as handwritten SQL.
- Prefer stored procedures with output parameters when returning a single record in procedure-driven designs.

## Modeling Standards

- Default to 3NF for new tables unless there is an approved reason to denormalize.
- Create new schemas only when they are truly needed.
- Coordinate DDL changes with the DBA team.

## Naming Conventions

- Use PascalCase-style names for tables and columns.
- Prefix views with `v_`.
- Keep names descriptive and avoid obscure abbreviations.
- Name indexes using `IX_[TableName]_[ColumnNames]`.
- Name primary keys using `PK_[TableName]_[ColumnName]`.
- Name foreign keys using `FK_[ColumnName]_[ReferencedTable]_[ReferencedColumn]`.
- Prefix stored procedures with `p_` followed by a singular entity name and verb, for example `p_Customer_Get`.