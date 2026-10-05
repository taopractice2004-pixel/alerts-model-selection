IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260923090951_InitialCreate'
)
BEGIN
    CREATE TABLE [Alerts] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [Severity] nvarchar(20) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Alerts] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260923090951_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Alerts_IsActive] ON [Alerts] ([IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260923090951_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260923090951_InitialCreate', N'8.0.31');
END;
GO

COMMIT;
GO

