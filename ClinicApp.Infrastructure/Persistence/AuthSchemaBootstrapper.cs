using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Infrastructure.Persistence;

public static class AuthSchemaBootstrapper
{
    public static async Task EnsureApplicationUserColumnsAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        const string sql = """
IF COL_LENGTH(N'[dbo].[AspNetUsers]', N'FullName') IS NULL
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [FullName] NVARCHAR(150) NOT NULL CONSTRAINT [DF_AspNetUsers_FullName] DEFAULT (N'') WITH VALUES;
END;

IF COL_LENGTH(N'[dbo].[AspNetUsers]', N'Role') IS NULL
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [Role] NVARCHAR(20) NOT NULL CONSTRAINT [DF_AspNetUsers_Role] DEFAULT (N'Patient') WITH VALUES;
END;

IF COL_LENGTH(N'[dbo].[AspNetUsers]', N'AvatarUrl') IS NULL
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [AvatarUrl] NVARCHAR(500) NULL;
END;

IF COL_LENGTH(N'[dbo].[AspNetUsers]', N'AuthProvider') IS NULL
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [AuthProvider] NVARCHAR(20) NULL;
END;

IF COL_LENGTH(N'[dbo].[AspNetUsers]', N'ProviderUserId') IS NULL
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [ProviderUserId] NVARCHAR(100) NULL;
END;

        IF COL_LENGTH(N'[dbo].[AspNetUsers]', N'IsFirstLogin') IS NULL
        BEGIN
            ALTER TABLE [dbo].[AspNetUsers] ADD [IsFirstLogin] BIT NOT NULL CONSTRAINT [DF_AspNetUsers_IsFirstLogin] DEFAULT ((0)) WITH VALUES;
        END;

        IF NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
            INNER JOIN sys.tables t ON t.object_id = c.object_id
            INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
            WHERE s.name = N'dbo'
              AND t.name = N'AspNetUsers'
              AND c.name = N'IsFirstLogin'
        )
        BEGIN
            ALTER TABLE [dbo].[AspNetUsers] ADD CONSTRAINT [DF_AspNetUsers_IsFirstLogin] DEFAULT ((0)) FOR [IsFirstLogin];
        END;

        IF COL_LENGTH(N'[dbo].[AspNetUsers]', N'IsActive') IS NULL
        BEGIN
            ALTER TABLE [dbo].[AspNetUsers] ADD [IsActive] BIT NOT NULL CONSTRAINT [DF_AspNetUsers_IsActive] DEFAULT ((1)) WITH VALUES;
        END;

        IF NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
            INNER JOIN sys.tables t ON t.object_id = c.object_id
            INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
            WHERE s.name = N'dbo'
              AND t.name = N'AspNetUsers'
              AND c.name = N'IsActive'
        )
        BEGIN
            ALTER TABLE [dbo].[AspNetUsers] ADD CONSTRAINT [DF_AspNetUsers_IsActive] DEFAULT ((1)) FOR [IsActive];
        END;

IF COL_LENGTH(N'[dbo].[AspNetUsers]', N'CreatedAt') IS NULL
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_AspNetUsers_CreatedAt] DEFAULT (SYSUTCDATETIME()) WITH VALUES;
END;

IF COL_LENGTH(N'[dbo].[AspNetUsers]', N'UpdatedAt') IS NULL
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [UpdatedAt] DATETIME2 NULL;
END;

UPDATE u
SET
    FullName = CASE
        WHEN NULLIF(LTRIM(RTRIM(u.FullName)), N'') IS NOT NULL THEN u.FullName
        WHEN NULLIF(LTRIM(RTRIM(u.UserName)), N'') IS NOT NULL THEN u.UserName
        WHEN NULLIF(LTRIM(RTRIM(u.Email)), N'') IS NOT NULL THEN u.Email
        ELSE N''
    END,
    Role = COALESCE(
        (
            SELECT TOP (1) r.Name
            FROM [dbo].[AspNetUserRoles] ur
            INNER JOIN [dbo].[AspNetRoles] r ON r.Id = ur.RoleId
            WHERE ur.UserId = u.Id
            ORDER BY CASE r.Name
                WHEN N'Admin' THEN 1
                WHEN N'Staff' THEN 2
                WHEN N'Doctor' THEN 3
                WHEN N'Patient' THEN 4
                ELSE 5
            END
        ),
        CASE WHEN NULLIF(LTRIM(RTRIM(u.Role)), N'') IS NOT NULL THEN u.Role ELSE N'Patient' END
    ),
    IsActive = COALESCE(u.IsActive, 1),
    IsFirstLogin = COALESCE(u.IsFirstLogin, 0),
    CreatedAt = COALESCE(u.CreatedAt, SYSUTCDATETIME())
FROM [dbo].[AspNetUsers] u;
""";

        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    public static async Task EnsureCustomAuthTablesAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        const string sql = """
IF OBJECT_ID(N'[dbo].[RefreshTokens]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RefreshTokens](
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [Token] NVARCHAR(256) NOT NULL,
        [UserId] NVARCHAR(450) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [CreatedByIp] NVARCHAR(64) NULL,
        [ExpiresAt] DATETIME2 NOT NULL,
        [RevokedAt] DATETIME2 NULL,
        [ReplacedByToken] NVARCHAR(256) NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefreshTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX [IX_RefreshTokens_Token] ON [dbo].[RefreshTokens] ([Token]);
END;

IF OBJECT_ID(N'[dbo].[ExternalLoginAccounts]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ExternalLoginAccounts](
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [UserId] NVARCHAR(450) NOT NULL,
        [Provider] NVARCHAR(20) NOT NULL,
        [ProviderUserId] NVARCHAR(100) NOT NULL,
        [ProviderEmail] NVARCHAR(256) NOT NULL,
        [ProviderDisplayName] NVARCHAR(200) NOT NULL,
        [ProviderPhotoUrl] NVARCHAR(500) NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [LastLoginAt] DATETIME2 NULL,
        CONSTRAINT [PK_ExternalLoginAccounts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ExternalLoginAccounts_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX [IX_ExternalLoginAccounts_Provider_ProviderUserId] ON [dbo].[ExternalLoginAccounts] ([Provider], [ProviderUserId]);
    CREATE UNIQUE INDEX [IX_ExternalLoginAccounts_UserId_Provider] ON [dbo].[ExternalLoginAccounts] ([UserId], [Provider]);
END;
""";

        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }
}
