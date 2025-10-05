using System.ComponentModel;
using System.Text.Json;
using Mcp.DotNet.CliWorkshop.Core.Services;
using ModelContextProtocol.Server;

namespace Mcp.DotNet.CliWorkshop.Server.Tools;

/// <summary>
/// MCP tools that expose .NET CLI capabilities via Model Context Protocol.
/// Adapts DotNetCliService to MCP server tool interface.
/// </summary>
public class DotNetCliMcpTools(
    IDotNetCliService cliService,
    ILogger<DotNetCliMcpTools> logger)
{
    [McpServerTool]
    [Description("Lists all installed .NET SDKs with their versions and installation paths. Executes: dotnet --list-sdks")]
    public async Task<string> ListInstalledSdksAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("MCP tool list_installed_sdks invoked");
            var sdks = await cliService.GetInstalledSdksAsync(cancellationToken).ConfigureAwait(false);

            var result = new
            {
                count = sdks.Count,
                sdks = sdks.Select(s => new { version = s.Version, path = s.Path }).ToList()
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing list_installed_sdks");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool]
    [Description("Lists all installed .NET runtimes with their versions and installation paths. Executes: dotnet --list-runtimes")]
    public async Task<string> ListInstalledRuntimesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("MCP tool list_installed_runtimes invoked");
            var runtimes = await cliService.GetInstalledRuntimesAsync(cancellationToken).ConfigureAwait(false);

            var result = new
            {
                count = runtimes.Count,
                runtimes = runtimes.Select(r => new { name = r.Name, version = r.Version, path = r.Path }).ToList()
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing list_installed_runtimes");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool]
    [Description("Gets the effective .NET SDK version being used in the specified directory. This respects global.json and roll-forward rules. Executes: dotnet --version (in the specified directory)")]
    public async Task<string> GetEffectiveSdkAsync(
        [Description("The directory to check. If not provided, uses the current working directory.")]
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("MCP tool get_effective_sdk invoked with workingDirectory: {Directory}",
                workingDirectory ?? "current");
            var effectiveVersion = await cliService.GetEffectiveSdkAsync(workingDirectory, cancellationToken).ConfigureAwait(false);

            var result = new
            {
                effective_version = effectiveVersion,
                working_directory = workingDirectory ?? Environment.CurrentDirectory,
                note = "This is the SDK version that dotnet will use in this directory, respecting global.json if present"
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing get_effective_sdk");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool]
    [Description("Gets detailed .NET information including SDK version, runtime versions, and environment. Executes: dotnet --info")]
    public async Task<string> GetDotNetInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("MCP tool get_dotnet_info invoked");
            var info = await cliService.GetDotNetInfoAsync(cancellationToken).ConfigureAwait(false);

            var result = new
            {
                sdk_version = info.SdkVersion,
                runtime_version = info.RuntimeVersion,
                os_version = info.OsVersion,
                architecture = info.Architecture
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing get_dotnet_info");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool]
    [Description("Checks if a specific .NET SDK version is installed on the system")]
    public async Task<string> CheckSdkVersionAsync(
        [Description("The SDK version to check (e.g., '8.0.202' or '9.0.302')")]
        string version,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("MCP tool check_sdk_version invoked with version: {Version}", version);
            var sdks = await cliService.GetInstalledSdksAsync(cancellationToken).ConfigureAwait(false);
            var isInstalled = sdks.Any(s => s.Version == version);

            var result = new
            {
                requested_version = version,
                is_installed = isInstalled,
                closest_matches = sdks
                    .Where(s => s.Version.StartsWith(version.Split('.')[0]))
                    .Select(s => new { version = s.Version, path = s.Path })
                    .ToList()
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing check_sdk_version");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool]
    [Description("Gets the latest installed .NET SDK version")]
    public async Task<string> GetLatestSdkAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("MCP tool get_latest_sdk invoked");
            var sdks = await cliService.GetInstalledSdksAsync(cancellationToken).ConfigureAwait(false);

            if (sdks.Count == 0)
            {
                return JsonSerializer.Serialize(new { error = "No SDKs installed" });
            }

            var latestSdk = sdks.OrderByDescending(s => s.Version).First();

            var result = new
            {
                latest_version = latestSdk.Version,
                path = latestSdk.Path,
                total_sdks_installed = sdks.Count
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing get_latest_sdk");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
