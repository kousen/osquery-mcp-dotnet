using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using OsqueryMcp.Server;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddSingleton(new OsqueryOptions
{
    ExecutablePath = Environment.GetEnvironmentVariable("OSQUERY_PATH")
        ?? @"C:\Program Files\osquery\osqueryi.exe"
});
builder.Services.AddSingleton<OsqueryRunner>();
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<OsqueryTools>();

await builder.Build().RunAsync();