using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Commerce.Demo.Business.Migrations;

/// <summary>
/// Creates the dedicated CookieConsentAudit SQL table in the CMS database.
/// CMS MigrationStep subclasses are auto-discovered and executed once during initialization.
/// </summary>
public class Migration4CookieConsentAuditTable : RunOnceMigrationStep
{
    private static readonly ILogger Logger = LogManager.GetLogger(typeof(Migration4CookieConsentAuditTable));

    protected override void RunOnce()
    {
        // MigrationStep is not guaranteed constructor-injectable, so resolve dependencies via the service locator.
        var configuration = ServiceLocator.Current.GetInstance<IConfiguration>();
        var connectionString = configuration.GetConnectionString("EPiServerDB");

        if (string.IsNullOrEmpty(connectionString))
        {
            Logger.Warning("EPiServerDB connection string not found; skipping CookieConsentAudit table creation.");
            return;
        }

        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            using var command = new SqlCommand(CreateTableSql, connection);
            command.ExecuteNonQuery();

            Logger.Information("CookieConsentAudit table ensured.");
        }
        catch (Exception ex)
        {
            // Don't block initialization; the service degrades gracefully if the table is missing.
            Logger.Error("Failed to create CookieConsentAudit table.", ex);
        }
    }

    private const string CreateTableSql = @"
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CookieConsentAudit]') AND type = N'U')
BEGIN
    CREATE TABLE [dbo].[CookieConsentAudit] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_CookieConsentAudit] PRIMARY KEY,
        [Timestamp] DATETIME2(7) NOT NULL,
        [Action] NVARCHAR(20) NOT NULL,
        [CategoriesSelected] NVARCHAR(256) NOT NULL,
        [VisitorId] NVARCHAR(256) NULL,
        [IsAuthenticated] BIT NOT NULL,
        [PolicyVersion] NVARCHAR(64) NULL,
        [IpAddress] NVARCHAR(64) NULL,
        [UserAgent] NVARCHAR(1024) NULL,
        [CookieName] NVARCHAR(128) NULL
    );

    CREATE INDEX [IX_CookieConsentAudit_Timestamp] ON [dbo].[CookieConsentAudit] ([Timestamp] DESC);
    CREATE INDEX [IX_CookieConsentAudit_Action] ON [dbo].[CookieConsentAudit] ([Action]);
END";
}
