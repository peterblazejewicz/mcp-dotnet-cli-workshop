namespace Mcp.DotNet.CliWorkshop.Server.Infrastructure.Configuration;

/// <summary>
/// Factory responsible for creating application configuration from multiple sources.
/// </summary>
public static class ConfigurationFactory
{
    /// <summary>
    /// Creates an IConfiguration instance with JSON files and environment variables.
    /// </summary>
    /// <returns>A fully configured IConfiguration instance.</returns>
    public static IConfiguration Create()
    {
        // Use tool-specific environment variable for environment name to avoid global conflicts
        var environment = Environment.GetEnvironmentVariable("MCPDOTNETCLI_ENVIRONMENT")
                       ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                       ?? "Production";

        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            // Add tool-specific environment variables with prefix (highest priority)
            .AddEnvironmentVariables(prefix: "MCPDOTNETCLI_")
            // Fallback to standard environment variables for compatibility
            .AddEnvironmentVariables()
            .Build();
    }

    /// <summary>
    /// Gets the current environment name.
    /// </summary>
    /// <returns>The environment name from MCPDOTNETCLI_ENVIRONMENT or DOTNET_ENVIRONMENT variable, or "Production" if not set.</returns>
    public static string GetEnvironment()
    {
        return Environment.GetEnvironmentVariable("MCPDOTNETCLI_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Production";
    }
}
