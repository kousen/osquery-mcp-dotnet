using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OsqueryMcp.Server;

public sealed partial class OsqueryRunner(OsqueryOptions options)
{
    public async Task<JsonElement> QueryAsync(string sql, CancellationToken cancellationToken = default)
    {
        ValidateQuery(sql);

        var startInfo = new ProcessStartInfo
        {
            FileName = options.ExecutablePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("--json");
        startInfo.ArgumentList.Add(sql);

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException("Unable to start osquery.");
            }
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            throw new InvalidOperationException(
                $"Could not start osquery at '{options.ExecutablePath}'. Set OSQUERY_PATH to osqueryi.exe.", exception);
        }

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.QueryTimeout);

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            cancellationToken.ThrowIfCancellationRequested();
            throw new TimeoutException($"The osquery query exceeded {options.QueryTimeout.TotalSeconds:g} seconds.");
        }

        var output = await outputTask;
        var error = await errorTask;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"osquery failed: {error.Trim()}");
        }

        using var document = JsonDocument.Parse(output);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("osquery returned an unexpected JSON value.");
        }

        if (document.RootElement.GetArrayLength() > options.MaximumRows)
        {
            throw new InvalidOperationException(
                $"Query returned more than {options.MaximumRows} rows. Add a LIMIT clause.");
        }

        return document.RootElement.Clone();
    }

    public static void ValidateQuery(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ArgumentException("SQL query cannot be empty.", nameof(sql));
        }

        var normalized = StripCommentsRegex().Replace(sql, " ").Trim();
        if (!ReadOnlyStartRegex().IsMatch(normalized))
        {
            throw new ArgumentException("Only SELECT, WITH, and PRAGMA queries are allowed.", nameof(sql));
        }

        var withoutTrailingTerminator = normalized.TrimEnd().TrimEnd(';').TrimEnd();
        if (withoutTrailingTerminator.Contains(';'))
        {
            throw new ArgumentException("Multiple SQL statements are not allowed.", nameof(sql));
        }

        if (MutationRegex().IsMatch(normalized))
        {
            throw new ArgumentException("Mutating SQL statements are not allowed.", nameof(sql));
        }
    }

    [GeneratedRegex(@"(?s)/\*.*?\*/|--[^\r\n]*")]
    private static partial Regex StripCommentsRegex();

    [GeneratedRegex(@"^(SELECT|WITH|PRAGMA)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ReadOnlyStartRegex();

    [GeneratedRegex(@"\b(ATTACH|DETACH|INSERT|UPDATE|DELETE|CREATE|DROP|ALTER|REPLACE|VACUUM|REINDEX)\b", RegexOptions.IgnoreCase)]
    private static partial Regex MutationRegex();
}