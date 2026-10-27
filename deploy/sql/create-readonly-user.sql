-- Least-privilege read-only SQL login for the web app.
-- Run as sa/admin AFTER migrations + seed. Replace <APP_PASSWORD> with a strong secret
-- from the secret store. Never store the value in Git or logs.
USE [master];
GO
IF NOT EXISTS (SELECT 1 FROM sys.sql_logins WHERE name = N'inkasso_app')
BEGIN
    CREATE LOGIN [inkasso_app]
        WITH PASSWORD = N'<APP_PASSWORD>',
             CHECK_POLICY = ON,
             CHECK_EXPIRATION = OFF;
END
GO
USE [InkassoDb];
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'inkasso_app')
BEGIN
    CREATE USER [inkasso_app] FOR LOGIN [inkasso_app];
END
GO
-- Read-only baseline for all tables.
ALTER ROLE [db_datareader] ADD MEMBER [inkasso_app];
GO
-- Defense in depth: deny writes even if app guard is bypassed.
DENY INSERT, UPDATE, DELETE, ALTER, CREATE TABLE, CREATE VIEW,
     CREATE PROCEDURE, CREATE FUNCTION TO [inkasso_app];
GO
