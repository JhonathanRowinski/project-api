IF OBJECT_ID(N'dbo.Accounts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Accounts
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        Email NVARCHAR(256) COLLATE Latin1_General_100_CI_AS NOT NULL,
        PasswordHash NVARCHAR(512) NOT NULL,
        CreatedAtUtc DATETIME2(7) NOT NULL
            CONSTRAINT DF_Accounts_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Accounts PRIMARY KEY (Id)
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.default_constraints
    WHERE name = N'DF_Accounts_CreatedAtUtc'
      AND parent_object_id = OBJECT_ID(N'dbo.Accounts')
)
BEGIN
    ALTER TABLE dbo.Accounts
        ADD CONSTRAINT DF_Accounts_CreatedAtUtc DEFAULT SYSUTCDATETIME() FOR CreatedAtUtc;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_Accounts_Email'
      AND object_id = OBJECT_ID(N'dbo.Accounts')
)
BEGIN
    CREATE UNIQUE INDEX UX_Accounts_Email ON dbo.Accounts (Email);
END;
