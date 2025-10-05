namespace DotNetCliMcp.App.Infrastructure.Configuration;

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
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
    }

    /// <summary>
    /// Gets the current environment name.
    /// </summary>
    /// <returns>The environment name from DOTNET_ENVIRONMENT variable, or "Production" if not set.</returns>
    public static string GetEnvironment()
    {
        return Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
    }
}
