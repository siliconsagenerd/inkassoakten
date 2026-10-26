using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace InkassoAkten.IntegrationTests;

internal static class TestDatabaseGuard
{
    private static readonly HashSet<string> BlockedDatabaseNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "", "master", "tempdb", "model", "msdb", "InkassoDb", "InkassoVerify"
    };

    public static void AssertSafeForDestructiveTests(string connectionString)
    {
        // Testcontainers mode: the entire SQL Server container is ephemeral, so no
        // shared persistent database needs protection. The guard only applies when a
        // shared server is explicitly configured via TEST_SQLSERVER_CONNECTIONSTRING.
        var overrideConn = Environment.GetEnvironmentVariable("TEST_SQLSERVER_CONNECTIONSTRING");
        if (string.IsNullOrWhiteSpace(overrideConn))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string is empty. Refusing destructive test.");

        var builder = new SqlConnectionStringBuilder(connectionString);
        var database = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(database))
            throw new InvalidOperationException("Connection string has no database name. Refusing destructive test.");

        if (BlockedDatabaseNames.Contains(database))
            throw new InvalidOperationException($"Database '{database}' is not a disposable test database. Refusing destructive test.");

        if (!database.StartsWith("InkassoAkten_Test_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Shared-server test database '{database}' must be named 'InkassoAkten_Test_<suffix>'. Refusing destructive test.");
        }
    }
}