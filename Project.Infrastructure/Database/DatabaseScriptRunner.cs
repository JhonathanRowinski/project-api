using System.Reflection;
using DbUp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Project.Infrastructure.Database;

/// <summary>Ensures the database exists and applies pending embedded SQL scripts.</summary>
public sealed class DatabaseScriptRunner(
    IConfiguration configuration,
    ILogger<DatabaseScriptRunner> logger)
{
    /// <summary>The configuration key used to retrieve the database connection string.</summary>
    private const string ConnectionStringName = "ProjectDb";

    /// <summary>The resource name prefix used to identify embedded SQL scripts.</summary>
    private const string ScriptsResourcePrefix = "Project.Infrastructure.Database.Scripts.";

    /// <summary>
    /// Ensures the database exists and applies any pending SQL scripts embedded in the assembly.
    /// Throws an exception if the connection string is missing or a script fails.
    /// </summary>
    public void Run()
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"Configure the ConnectionStrings:{ConnectionStringName} connection string.");

        EnsureDatabase.For.SqlDatabase(connectionString);

        var upgradeResult = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                Assembly.GetExecutingAssembly(),
                scriptName => scriptName.StartsWith(ScriptsResourcePrefix, StringComparison.Ordinal))
            .JournalToSqlTable("dbo", "SchemaVersions")
            .WithTransactionPerScript()
            .LogToConsole()
            .Build()
            .PerformUpgrade();

        if (!upgradeResult.Successful)
        {
            logger.LogError(upgradeResult.Error, "Database SQL scripts failed during startup.");
            throw new InvalidOperationException("Database SQL scripts failed during startup.", upgradeResult.Error);
        }

        logger.LogInformation("Database SQL scripts completed successfully.");
    }
}
