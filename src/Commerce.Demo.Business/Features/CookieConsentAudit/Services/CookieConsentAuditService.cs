using Commerce.Demo.Business.Features.CookieConsentAudit.Enums;
using Commerce.Demo.Business.Features.CookieConsentAudit.Interfaces;
using Commerce.Demo.Business.Features.CookieConsentAudit.Models;
using Commerce.Demo.Business.Features.CookieConsentAudit.Options;
using Commerce.Demo.Business.Infrastructure.Caching;
using EPiServer.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Commerce.Demo.Business.Features.CookieConsentAudit.Services;

public class CookieConsentAuditService : ICookieConsentAuditService
{
    private const string StatsCacheKeyPrefix = "cookie-consent-stats";
    private const string TableName = "[dbo].[CookieConsentAudit]";
    private const int MaxUserAgentLength = 1024;

    private readonly ILogger<CookieConsentAuditService> _logger;
    private readonly ICacheService _cache;
    private readonly CookieConsentAuditOptions _options;
    private readonly string _connectionString;

    public CookieConsentAuditService(
        ILogger<CookieConsentAuditService> logger,
        ICacheService cache,
        IOptions<DataAccessOptions> dataAccessOptions,
        IOptions<CookieConsentAuditOptions> options)
    {
        _logger = logger;
        _cache = cache;
        _options = options.Value;

        // Same connection string the platform itself uses (DataAccessOptions.DefaultConnectionStringName).
        var dataAccess = dataAccessOptions.Value;
        _connectionString = dataAccess.ConnectionStrings
            .FirstOrDefault(c => string.Equals(c.Name, dataAccess.DefaultConnectionStringName, StringComparison.OrdinalIgnoreCase))
            ?.ConnectionString
            ?? throw new InvalidOperationException(
                $"Connection string '{dataAccess.DefaultConnectionStringName}' is not configured; cookie consent audit cannot access the database.");
    }

    public async Task RecordConsent(
        RecordConsentRequest request,
        string? visitorId,
        bool isAuthenticated,
        string? ipAddress,
        string? userAgent)
    {
        try
        {
            // Normalize the action to the canonical enum name so statistics counting is deterministic.
            var action = Enum.TryParse<ConsentAction>(request.Action, ignoreCase: true, out var parsed)
                ? parsed.ToString()
                : request.Action;

            if (userAgent is { Length: > MaxUserAgentLength })
                userAgent = userAgent[..MaxUserAgentLength];

            var sql = $@"
INSERT INTO {TableName}
    ([Timestamp],[Action],[CategoriesSelected],[VisitorId],[IsAuthenticated],[PolicyVersion],[IpAddress],[UserAgent],[CookieName])
VALUES
    (@Timestamp,@Action,@CategoriesSelected,@VisitorId,@IsAuthenticated,@PolicyVersion,@IpAddress,@UserAgent,@CookieName)";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Timestamp", DateTime.UtcNow);
            command.Parameters.AddWithValue("@Action", action);
            command.Parameters.AddWithValue("@CategoriesSelected", request.CategoriesSelected);
            command.Parameters.AddWithValue("@VisitorId", (object?)visitorId ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsAuthenticated", isAuthenticated);
            command.Parameters.AddWithValue("@PolicyVersion", (object?)request.PolicyVersion ?? DBNull.Value);
            command.Parameters.AddWithValue("@IpAddress", (object?)ipAddress ?? DBNull.Value);
            command.Parameters.AddWithValue("@UserAgent", (object?)userAgent ?? DBNull.Value);
            command.Parameters.AddWithValue("@CookieName", (object?)request.CookieName ?? DBNull.Value);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            _logger.LogInformation(
                "Cookie consent recorded: Action={Action}, Visitor={VisitorId}, Categories={Categories}",
                action, visitorId, request.CategoriesSelected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record cookies consent audit entry");
        }
    }

    public async Task<ConsentAuditQueryResult> GetAuditEntries(
        int page,
        int pageSize,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? visitorId = null,
        string? visitorAction = null)
    {
        try
        {
            var where = BuildWhereClause(fromDate, toDate, visitorId, visitorAction, out var parameters);

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            int totalCount;
            using (var countCommand = new SqlCommand($"SELECT COUNT(*) FROM {TableName}{where}", connection))
            {
                AddParameters(countCommand, parameters);
                totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
            }

            var items = new List<ConsentAuditItem>();
            var selectSql = $@"
SELECT [Id],[Timestamp],[Action],[CategoriesSelected],[VisitorId],[IsAuthenticated],[PolicyVersion],[IpAddress],[UserAgent]
FROM {TableName}{where}
ORDER BY [Timestamp] DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            using (var selectCommand = new SqlCommand(selectSql, connection))
            {
                AddParameters(selectCommand, parameters);
                selectCommand.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                selectCommand.Parameters.AddWithValue("@PageSize", pageSize);

                using var reader = await selectCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    items.Add(MapItem(reader));
            }

            return new ConsentAuditQueryResult
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query cookies consent audit entries");
            return new ConsentAuditQueryResult
            {
                Items = [],
                TotalCount = 0,
                Page = page,
                PageSize = pageSize
            };
        }
    }

    public async Task<int> PurgeExpiredEntries(int retentionDays)
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddDays(-retentionDays);

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand($"DELETE FROM {TableName} WHERE [Timestamp] < @Cutoff", connection);
            command.Parameters.AddWithValue("@Cutoff", cutoff);

            await connection.OpenAsync();
            var deleted = await command.ExecuteNonQueryAsync();

            // Statistics are cached; invalidate them after a purge so the dashboard reflects the new totals.
            _cache.RemoveStartsWith(StatsCacheKeyPrefix);

            _logger.LogInformation(
                "Purged {Count} expired cookies consent audit entries older than {CutoffDate}",
                deleted, cutoff);

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to purge expired cookies consent audit entries");
            return 0;
        }
    }

    public async Task<ConsentAuditStatistics> GetStatistics(
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var cacheKey = $"{StatsCacheKeyPrefix}|{fromDate?.Ticks}|{toDate?.Ticks}";

        try
        {
            // A throwing factory is not cached, so a failed query never gets stored as empty statistics.
            return await _cache.GetOrSetAsync(
                cacheKey,
                () => ComputeStatistics(fromDate, toDate),
                TimeSpan.FromSeconds(_options.StatisticsCacheSeconds)) ?? new ConsentAuditStatistics();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cookies consent audit statistics");
            return new ConsentAuditStatistics();
        }
    }

    private async Task<ConsentAuditStatistics> ComputeStatistics(DateTime? fromDate, DateTime? toDate)
    {
        var where = BuildWhereClause(fromDate, toDate, null, null, out var parameters);

        // Aggregate in a single round trip rather than materializing rows.
        var sql = $@"
SELECT
    COUNT(*) AS Total,
    SUM(CASE WHEN [Action] = @Accepted THEN 1 ELSE 0 END) AS Accepted,
    SUM(CASE WHEN [Action] = @Denied THEN 1 ELSE 0 END) AS Denied,
    SUM(CASE WHEN [Action] = @Modified THEN 1 ELSE 0 END) AS Modified,
    MIN([Timestamp]) AS Oldest,
    MAX([Timestamp]) AS Newest
FROM {TableName}{where}";

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(sql, connection);
        AddParameters(command, parameters);
        command.Parameters.AddWithValue("@Accepted", nameof(ConsentAction.Accepted));
        command.Parameters.AddWithValue("@Denied", nameof(ConsentAction.Denied));
        command.Parameters.AddWithValue("@Modified", nameof(ConsentAction.Modified));

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return new ConsentAuditStatistics();

        return new ConsentAuditStatistics
        {
            TotalEntries = reader.GetInt32(0),
            AcceptedCount = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
            DeniedCount = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
            ModifiedCount = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
            OldestEntry = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
            NewestEntry = reader.IsDBNull(5) ? null : reader.GetDateTime(5)
        };
    }

    // Builds a parameterized WHERE clause; column names are fixed literals, all user values are parameters.
    private static string BuildWhereClause(
        DateTime? fromDate,
        DateTime? toDate,
        string? visitorId,
        string? action,
        out List<(string Name, object Value)> parameters)
    {
        parameters = [];
        var conditions = new List<string>();

        if (fromDate.HasValue)
        {
            conditions.Add("[Timestamp] >= @FromDate");
            parameters.Add(("@FromDate", fromDate.Value));
        }

        if (toDate.HasValue)
        {
            conditions.Add("[Timestamp] <= @ToDate");
            parameters.Add(("@ToDate", toDate.Value));
        }

        if (!string.IsNullOrWhiteSpace(visitorId))
        {
            conditions.Add("[VisitorId] = @VisitorId");
            parameters.Add(("@VisitorId", visitorId));
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            conditions.Add("[Action] = @FilterAction");
            parameters.Add(("@FilterAction", action));
        }

        return conditions.Count == 0 ? string.Empty : " WHERE " + string.Join(" AND ", conditions);
    }

    private static void AddParameters(SqlCommand command, List<(string Name, object Value)> parameters)
    {
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value);
    }

    private static ConsentAuditItem MapItem(SqlDataReader reader)
    {
        return new ConsentAuditItem
        {
            Id = reader.GetInt64(0).ToString(),
            Timestamp = reader.GetDateTime(1),
            Action = reader.GetString(2),
            CategoriesSelected = reader.GetString(3),
            VisitorId = reader.IsDBNull(4) ? null : reader.GetString(4),
            IsAuthenticated = reader.GetBoolean(5),
            PolicyVersion = reader.IsDBNull(6) ? null : reader.GetString(6),
            IpAddress = reader.IsDBNull(7) ? null : reader.GetString(7),
            UserAgent = reader.IsDBNull(8) ? null : reader.GetString(8)
        };
    }
}
