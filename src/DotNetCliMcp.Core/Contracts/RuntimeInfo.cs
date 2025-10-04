namespace Mcp.DotNet.CliWorkshop.Core.Contracts;

/// <summary>
/// Represents an installed .NET runtime.
/// </summary>
public record RuntimeInfo(
    string Name,
    string Version,
    string Path
);
