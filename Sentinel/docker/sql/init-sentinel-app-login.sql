-- This script is executed by sentinel-db-init with SQL Server administrator
-- credentials. Sentinel itself never uses the SQL Server `sa` account.
--
-- SQL_APP_PASSWORD is supplied by Docker Compose. Use the documented generated
-- value (letters, digits and ! only) so it is safe in both a connection string
-- and this SQLCMD script.
IF N'$(SQL_APP_PASSWORD)' = N''
BEGIN
    RAISERROR ('SQL_APP_PASSWORD must be supplied.', 16, 1);
    RETURN;
END;

IF N'$(SQL_BACKUP_PASSWORD)' = N''
BEGIN
    RAISERROR ('SQL_BACKUP_PASSWORD must be supplied.', 16, 1);
    RETURN;
END;
GO

IF DB_ID(N'SentinelDb') IS NULL
BEGIN
    CREATE DATABASE [SentinelDb];
END;
GO

DECLARE @password nvarchar(256) = N'$(SQL_APP_PASSWORD)';
DECLARE @backupPassword nvarchar(256) = N'$(SQL_BACKUP_PASSWORD)';
DECLARE @statement nvarchar(max);

IF EXISTS (SELECT 1 FROM sys.sql_logins WHERE name = N'sentinel_app')
BEGIN
    SET @statement = N'ALTER LOGIN [sentinel_app] WITH PASSWORD = '
        + QUOTENAME(@password, N'''')
        + N', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;';
END
ELSE
BEGIN
    SET @statement = N'CREATE LOGIN [sentinel_app] WITH PASSWORD = '
        + QUOTENAME(@password, N'''')
        + N', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;';
END;

EXEC sys.sp_executesql @statement;

IF EXISTS (SELECT 1 FROM sys.sql_logins WHERE name = N'sentinel_backup')
BEGIN
    SET @statement = N'ALTER LOGIN [sentinel_backup] WITH PASSWORD = '
        + QUOTENAME(@backupPassword, N'''')
        + N', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;';
END
ELSE
BEGIN
    SET @statement = N'CREATE LOGIN [sentinel_backup] WITH PASSWORD = '
        + QUOTENAME(@backupPassword, N'''')
        + N', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;';
END;

EXEC sys.sp_executesql @statement;
GO

USE [SentinelDb];
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'sentinel_app')
BEGIN
    CREATE USER [sentinel_app] FOR LOGIN [sentinel_app];
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'sentinel_backup')
BEGIN
    CREATE USER [sentinel_backup] FOR LOGIN [sentinel_backup];
END;
GO

-- Sentinel applies its EF Core migrations at startup, so the application
-- login requires read/write plus schema-management rights in SentinelDb only.
IF NOT EXISTS
(
    SELECT 1
    FROM sys.database_role_members AS membership
    INNER JOIN sys.database_principals AS rolePrincipal
        ON rolePrincipal.principal_id = membership.role_principal_id
    INNER JOIN sys.database_principals AS memberPrincipal
        ON memberPrincipal.principal_id = membership.member_principal_id
    WHERE rolePrincipal.name = N'db_datareader'
      AND memberPrincipal.name = N'sentinel_app'
)
    ALTER ROLE [db_datareader] ADD MEMBER [sentinel_app];

IF NOT EXISTS
(
    SELECT 1
    FROM sys.database_role_members AS membership
    INNER JOIN sys.database_principals AS rolePrincipal
        ON rolePrincipal.principal_id = membership.role_principal_id
    INNER JOIN sys.database_principals AS memberPrincipal
        ON memberPrincipal.principal_id = membership.member_principal_id
    WHERE rolePrincipal.name = N'db_datawriter'
      AND memberPrincipal.name = N'sentinel_app'
)
    ALTER ROLE [db_datawriter] ADD MEMBER [sentinel_app];

IF NOT EXISTS
(
    SELECT 1
    FROM sys.database_role_members AS membership
    INNER JOIN sys.database_principals AS rolePrincipal
        ON rolePrincipal.principal_id = membership.role_principal_id
    INNER JOIN sys.database_principals AS memberPrincipal
        ON memberPrincipal.principal_id = membership.member_principal_id
    WHERE rolePrincipal.name = N'db_ddladmin'
      AND memberPrincipal.name = N'sentinel_app'
)
    ALTER ROLE [db_ddladmin] ADD MEMBER [sentinel_app];
GO

-- The backup identity can create database backups only. It cannot read or
-- modify Sentinel application data and is never used for normal application
-- requests. Restore remains an explicit DBA operation.
IF NOT EXISTS
(
    SELECT 1
    FROM sys.database_role_members AS membership
    INNER JOIN sys.database_principals AS rolePrincipal
        ON rolePrincipal.principal_id = membership.role_principal_id
    INNER JOIN sys.database_principals AS memberPrincipal
        ON memberPrincipal.principal_id = membership.member_principal_id
    WHERE rolePrincipal.name = N'db_backupoperator'
      AND memberPrincipal.name = N'sentinel_backup'
)
    ALTER ROLE [db_backupoperator] ADD MEMBER [sentinel_backup];
GO
