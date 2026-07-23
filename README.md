# Osquery MCP Server for .NET

[![CI](https://github.com/kousen/osquery-mcp-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/kousen/osquery-mcp-dotnet/actions/workflows/ci.yml)

A local [Model Context Protocol](https://modelcontextprotocol.io/) server that exposes [osquery](https://osquery.io/) to MCP clients over stdio. It targets .NET 8 and uses the official [Model Context Protocol C# SDK](https://github.com/modelcontextprotocol/csharp-sdk).

## Tools

- `osquery_info` verifies the local installation and reports its version, platform, and registered table count.
- `osquery_query` runs one read-only `SELECT`, `WITH`, or `PRAGMA` query and returns structured JSON.

The server launches `osqueryi` without a shell, passes SQL through `ProcessStartInfo.ArgumentList`, enforces a 15-second timeout, rejects multiple or mutating statements, and limits results to 1,000 rows.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [osquery](https://osquery.io/downloads/official/5.23.1)

On Windows, the server defaults to `C:\Program Files\osquery\osqueryi.exe`. Set `OSQUERY_PATH` when osquery is installed elsewhere.

## Build and test

```powershell
dotnet restore
dotnet build
dotnet test
```

The integration tests invoke a real osquery installation. To run only deterministic unit tests, as CI does:

```powershell
dotnet test --filter "Category!=Integration"
```

## Run

```powershell
dotnet run --project src/OsqueryMcp.Server
```

To use a nondefault executable:

```powershell
$env:OSQUERY_PATH = "D:\tools\osquery\osqueryi.exe"
dotnet run --project src/OsqueryMcp.Server
```

The process waits for MCP JSON-RPC messages on stdin. Standard output is reserved for the stdio protocol; diagnostics are written to standard error.

## Configure an MCP client

This repository includes [.vscode/mcp.json](.vscode/mcp.json) for VS Code. Build the project once, run **MCP: List Servers**, and start `osquery-csharp`. In Copilot Chat Agent mode, enable `osquery_info` and `osquery_query` under **Configure Tools**.

Equivalent stdio configuration for another client:

```json
{
  "servers": {
    "osquery-csharp": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/absolute/path/to/osquery_mcp/src/OsqueryMcp.Server/OsqueryMcp.Server.csproj",
        "--no-build"
      ],
      "env": {
        "OSQUERY_PATH": "/absolute/path/to/osqueryi"
      }
    }
  }
}
```

Example prompts:

- "Use osquery to show the OS version."
- "List the five processes using the most resident memory."
- "Which listening TCP ports are open on this machine?"

Some managed GitHub Copilot environments restrict custom MCP servers to an enterprise allowlist or private registry. In that case, an administrator must approve this server before it appears in VS Code.

## Security

Treat osquery results as sensitive system information. This server is intended for local stdio use and does not expose an HTTP endpoint. Query validation is defense in depth; review generated SQL before approving tool calls in sensitive environments.

## License

Licensed under the [MIT License](LICENSE).
