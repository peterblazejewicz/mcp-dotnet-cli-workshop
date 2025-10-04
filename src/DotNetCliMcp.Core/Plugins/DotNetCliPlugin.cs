using System.ComponentModel;
using System.Text.Json;
using DotNetCliMcp.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace DotNetCliMcp.Core.Plugins;

/// <summary>
/// Semantic Kernel plugin that exposes .NET CLI capabilities as MCP-compatible functions.
/// These functions can be called by LLMs to answer questions about the .NET environment.
/// </summary>
public class DotNetCliPlugin
{
    private readonly IDotNetCliService _cliService;
    private readonly ILogger<DotNetCliPlugin> _logger;

    public DotNetCliPlugin(IDotNetCliService cliService, ILogger<DotNetCliPlugin> logger)
    {
        _cliService = cliService;
        _logger = logger;
    }

    [KernelFunction("get_dotnet_info")]
    [Description("Gets comprehensive information about the installed .NET SDK and runtime environment, including version, OS, and architecture details")]
    public async Task<string> GetDotNetInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Plugin function get_dotnet_info invoked");
            var info = await _cliService.GetDotNetInfoAsync(cancellationToken);

            var result = new
            {
                sdk_version = info.SdkVersion,
                runtime_version = info.RuntimeVersion,
                os_version = info.OsVersion,
                architecture = info.Architecture,
                raw_output = info.RawOutput
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing get_dotnet_info");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [KernelFunction("list_installed_sdks")]
    [Description("Lists all installed .NET SDKs with their version numbers and installation paths")]
    public async Task<string> ListInstalledSdksAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Plugin function list_installed_sdks invoked");
            var sdks = await _cliService.GetInstalledSdksAsync(cancellationToken);

            var result = new
            {
                count = sdks.Count,
                sdks = sdks.Select(s => new { version = s.Version, path = s.Path }).ToList()
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing list_installed_sdks");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [KernelFunction("list_installed_runtimes")]
    [Description("Lists all installed .NET runtimes (e.g., Microsoft.NETCore.App, Microsoft.AspNetCore.App) with their version numbers and installation paths")]
    public async Task<string> ListInstalledRuntimesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Plugin function list_installed_runtimes invoked");
            var runtimes = await _cliService.GetInstalledRuntimesAsync(cancellationToken);

            var result = new
            {
                count = runtimes.Count,
                runtimes = runtimes.Select(r => new { name = r.Name, version = r.Version, path = r.Path }).ToList()
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing list_installed_runtimes");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [KernelFunction("check_sdk_version")]
    [Description("Checks if a specific .NET SDK version is installed on the system")]
    public async Task<string> CheckSdkVersionAsync(
        [Description("The SDK version to check (e.g., '8.0.202' or '9.0.302')")] string version,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Plugin function check_sdk_version invoked with version: {Version}", version);
            var sdks = await _cliService.GetInstalledSdksAsync(cancellationToken);
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
            _logger.LogError(ex, "Error executing check_sdk_version");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [KernelFunction("get_latest_sdk")]
    [Description("Gets the latest installed .NET SDK version")]
    public async Task<string> GetLatestSdkAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Plugin function get_latest_sdk invoked");
            var sdks = await _cliService.GetInstalledSdksAsync(cancellationToken);

            if (sdks.Count == 0)
            {
                return JsonSerializer.Serialize(new { error = "No SDKs installed" });
            }

            // Sort by version (simple string comparison should work for most cases)
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
            _logger.LogError(ex, "Error executing get_latest_sdk");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
