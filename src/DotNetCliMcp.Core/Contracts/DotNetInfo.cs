namespace Mcp.DotNet.CliWorkshop.Core.Contracts;

/// <summary>
/// Represents comprehensive .NET environment information.
/// </summary>
public record DotNetInfo(
    string SdkVersion,
    string RuntimeVersion,
    string OsVersion,
    string Architecture,
    string RawOutput
);
