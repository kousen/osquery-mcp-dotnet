# Repository instructions

- This is a .NET 8 stdio MCP server built with the official C# SDK: https://github.com/modelcontextprotocol/csharp-sdk
- MCP documentation: https://modelcontextprotocol.io/docs/develop/build-server
- Keep stdout reserved for JSON-RPC. Send diagnostics and logs to stderr.
- Treat all osquery calls as untrusted process input. Use `ProcessStartInfo.ArgumentList`, timeouts, and read-only SQL validation.
- Run `dotnet test` after changes.