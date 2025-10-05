namespace Mcp.DotNet.CliWorkshop.Core.Services;

/// <summary>
/// Service for executing dotnet CLI commands and retrieving .NET SDK/Runtime information.
/// </summary>
public interface IDotNetCliService
{
    /// <summary>
    /// Gets detailed .NET information including SDK version, runtime versions, and environment.
    /// Executes: dotnet --info
    /// </summary>
    Task<DotNetInfo> GetDotNetInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all installed .NET SDKs with their versions and installation paths.
    /// Executes: dotnet --list-sdks
    /// </summary>
    Task<IReadOnlyList<SdkInfo>> GetInstalledSdksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all installed .NET runtimes with their versions and installation paths.
    /// Executes: dotnet --list-runtimes
    /// </summary>
    Task<IReadOnlyList<RuntimeInfo>> GetInstalledRuntimesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the effective .NET SDK version being used in the specified directory.
    /// This respects global.json and roll-forward rules.
    /// Executes: dotnet --version (in the specified directory)
    /// </summary>
    /// <param name="workingDirectory">The directory to check the SDK version in. If null, uses the current directory.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<string> GetEffectiveSdkAsync(string? workingDirectory = null, CancellationToken cancellationToken = default);
}
