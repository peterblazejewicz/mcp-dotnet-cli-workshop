namespace Mcp.DotNet.CliWorkshop.Core.Contracts;

/// <summary>
/// Represents an installed .NET SDK.
/// </summary>
public record SdkInfo(
    string Version,
    string Path
);
