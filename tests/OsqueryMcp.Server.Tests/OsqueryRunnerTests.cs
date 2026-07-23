using System.Text.Json;
using ModelContextProtocol.Client;
using OsqueryMcp.Server;
using Xunit;

namespace OsqueryMcp.Server.Tests;

public sealed class OsqueryRunnerTests
{
    private static readonly OsqueryOptions Options = new()
    {
        ExecutablePath = Environment.GetEnvironmentVariable("OSQUERY_PATH")
            ?? @"C:\Program Files\osquery\osqueryi.exe"
    };

    [Theory]
    [InlineData("SELECT * FROM os_version;")]
    [InlineData("-- explanation\nSELECT name FROM processes LIMIT 1")]
    [InlineData("WITH versions AS (SELECT version FROM os_version) SELECT * FROM versions")]
    [InlineData("PRAGMA table_info(processes)")]
    public void ValidateQuery_AllowsReadOnlySql(string sql) => OsqueryRunner.ValidateQuery(sql);

    [Theory]
    [InlineData("")]
    [InlineData("DELETE FROM processes")]
    [InlineData("SELECT * FROM os_version; SELECT * FROM processes")]
    [InlineData("WITH x AS (DELETE FROM processes) SELECT * FROM x")]
    public void ValidateQuery_RejectsUnsafeSql(string sql) =>
        Assert.ThrowsAny<ArgumentException>(() => OsqueryRunner.ValidateQuery(sql));

    [Fact]
    [Trait("Category", "Integration")]
    public async Task QueryAsync_ReturnsOsVersionAsJson()
    {
        var runner = new OsqueryRunner(Options);

        JsonElement result = await runner.QueryAsync("SELECT name, platform FROM os_version;");

        Assert.Equal(JsonValueKind.Array, result.ValueKind);
        Assert.Equal("windows", result[0].GetProperty("platform").GetString());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetInfoAsync_ReturnsInstalledOsqueryInformation()
    {
        var tools = new OsqueryTools(new OsqueryRunner(Options));

        OsqueryInfo info = await tools.GetInfoAsync();

        Assert.False(string.IsNullOrWhiteSpace(info.Version));
        Assert.Equal("windows", info.Platform);
        Assert.True(info.TableCount > 0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task McpServer_ListsAndInvokesOsqueryToolsOverStdio()
    {
        var serverAssembly = Path.GetFullPath(
            "../../../../../src/OsqueryMcp.Server/bin/Debug/net8.0/OsqueryMcp.Server.dll",
            AppContext.BaseDirectory);
        await using var client = await McpClient.CreateAsync(new StdioClientTransport(new()
        {
            Name = "Osquery MCP integration test",
            Command = "dotnet",
            Arguments = [serverAssembly]
        }));

        var tools = await client.ListToolsAsync();
        var result = await client.CallToolAsync("osquery_info");

        Assert.Contains(tools, tool => tool.Name == "osquery_query");
        Assert.Contains(tools, tool => tool.Name == "osquery_info");
        Assert.NotEqual(true, result.IsError);
    }
}