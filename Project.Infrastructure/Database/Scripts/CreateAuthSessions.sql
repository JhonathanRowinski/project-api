IF OBJECT_ID(N'dbo.AuthSessions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuthSessions
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        AccountId UNIQUEIDENTIFIER NOT NULL,
        RefreshTokenHash NVARCHAR(128) NOT NULL,
        CreatedAtUtc DATETIME2(7) NOT NULL,
        RefreshTokenExpiresAtUtc DATETIME2(7) NOT NULL,
        AbsoluteExpiresAtUtc DATETIME2(7) NOT NULL,
        RevokedAtUtc DATETIME2(7) NULL,
        Country NVARCHAR(100) NULL,
        CountryCode NVARCHAR(2) NULL,
        State NVARCHAR(100) NULL,
        StateCode NVARCHAR(10) NULL,
        City NVARCHAR(150) NULL,
        Browser NVARCHAR(100) NULL,
        BrowserVersion NVARCHAR(50) NULL,
        OperatingSystem NVARCHAR(100) NULL,
        OperatingSystemVersion NVARCHAR(50) NULL,
        DeviceType NVARCHAR(50) NULL,

        CONSTRAINT PK_AuthSessions PRIMARY KEY (Id),
        CONSTRAINT FK_AuthSessions_Accounts
            FOREIGN KEY (AccountId) REFERENCES dbo.Accounts (Id)
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_AuthSessions_Accounts'
      AND parent_object_id = OBJECT_ID(N'dbo.AuthSessions')
)
BEGIN
    ALTER TABLE dbo.AuthSessions WITH CHECK
        ADD CONSTRAINT FK_AuthSessions_Accounts
        FOREIGN KEY (AccountId) REFERENCES dbo.Accounts (Id);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.AuthSessions')
      AND name = N'Country'
)
BEGIN
    ALTER TABLE dbo.AuthSessions
        ADD Country NVARCHAR(100) NULL;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.AuthSessions')
      AND name = N'CountryCode'
)
BEGIN
    ALTER TABLE dbo.AuthSessions
        ADD CountryCode NVARCHAR(2) NULL;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.AuthSessions')
      AND name = N'State'
)
BEGIN
    ALTER TABLE dbo.AuthSessions
        ADD State NVARCHAR(100) NULL;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.AuthSessions')
      AND name = N'StateCode'
)
BEGIN
    ALTER TABLE dbo.AuthSessions
        ADD StateCode NVARCHAR(10) NULL;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.AuthSessions')
      AND name = N'City'
)
BEGIN
    ALTER TABLE dbo.AuthSessions
        ADD City NVARCHAR(150) NULL;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.AuthSessions')
      AND name = N'Browser'
)
BEGIN
    ALTER TABLE dbo.AuthSessions
        ADD Browser NVARCHAR(100) NULL;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.AuthSessions')
      AND name = N'BrowserVersion'
)
BEGIN
    ALTER TABLE dbo.AuthSessions
        ADD BrowserVersion NVARCHAR(50) NULL;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.AuthSessions')
      AND name = N'OperatingSystem'
)
BEGIN
    ALTER TABLE dbo.AuthSessions
        ADD OperatingSystem NVARCHAR(100) NULL;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.AuthSessions')
      AND name = N'OperatingSystemVersion'
)
BEGIN
    ALTER TABLE dbo.AuthSessions
        ADD OperatingSystemVersion NVARCHAR(50) NULL;
END;


IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.AuthSessions')
      AND name = N'DeviceType'
)
BEGIN
    ALTER TABLE dbo.AuthSessions
        ADD DeviceType NVARCHAR(50) NULL;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_AuthSessions_RefreshTokenHash'
      AND object_id = OBJECT_ID(N'dbo.AuthSessions')
)
BEGIN
    CREATE UNIQUE INDEX UX_AuthSessions_RefreshTokenHash
        ON dbo.AuthSessions (RefreshTokenHash);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_AuthSessions_AccountId'
      AND object_id = OBJECT_ID(N'dbo.AuthSessions')
)
BEGIN
    CREATE INDEX IX_AuthSessions_AccountId ON dbo.AuthSessions (AccountId);
END;
