namespace OsqueryMcp.Server;

public sealed class OsqueryOptions
{
    public required string ExecutablePath { get; init; }

    public TimeSpan QueryTimeout { get; init; } = TimeSpan.FromSeconds(15);

    public int MaximumRows { get; init; } = 1_000;
}