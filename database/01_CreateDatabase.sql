-- Creates the AlertService database (run once, e.g. in SSMS / Azure Data Studio / sqlcmd).
-- Tables are then created by EF Core migrations:
--   * automatically on startup in Development (Database:ApplyMigrationsOnStartup = true), or
--   * with: dotnet ef database update --project AlertService.Data.SQL --startup-project AlertService.API, or
--   * by running 02_AlertServiceDb_Migrations.sql (generated, idempotent) in this folder.

IF DB_ID(N'AlertServiceDb') IS NULL
BEGIN
    CREATE DATABASE [AlertServiceDb];
END;
GO

USE [AlertServiceDb];
GO

-- Optional seed data (run after the migration has created the Alerts table).
-- INSERT INTO [Alerts] ([Title], [Description], [Severity], [CreatedDate], [IsActive])
-- VALUES (N'CPU usage high', N'CPU above 90% on app-server-01', N'High', SYSUTCDATETIME(), 1),
--        (N'Disk almost full', N'/var at 95%', N'Critical', SYSUTCDATETIME(), 1);
