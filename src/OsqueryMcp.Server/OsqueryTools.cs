using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace OsqueryMcp.Server;

[McpServerToolType]
public sealed class OsqueryTools(OsqueryRunner runner)
{
    [McpServerTool(Name = "osquery_query", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Runs one read-only osquery SQL query and returns the result as structured JSON. Use LIMIT for potentially large tables.")]
    public Task<JsonElement> QueryAsync(
        [Description("A single SELECT, WITH, or PRAGMA statement using osquery tables.")] string sql,
        CancellationToken cancellationToken = default) => runner.QueryAsync(sql, cancellationToken);

    [McpServerTool(Name = "osquery_info", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Returns the local osquery version, platform, and number of registered tables. Call this to verify availability.")]
    public async Task<OsqueryInfo> GetInfoAsync(CancellationToken cancellationToken = default)
    {
        var rows = await runner.QueryAsync(
            "SELECT version, (SELECT platform FROM os_version LIMIT 1) AS platform, " +
            "(SELECT count(*) FROM osquery_registry) AS table_count FROM osquery_info;",
            cancellationToken);
        var row = rows[0];

        return new OsqueryInfo(
            row.GetProperty("version").GetString() ?? "unknown",
            row.GetProperty("platform").GetString() ?? "unknown",
            int.Parse(row.GetProperty("table_count").GetString() ?? "0"));
    }
}

public sealed record OsqueryInfo(string Version, string Platform, int TableCount);