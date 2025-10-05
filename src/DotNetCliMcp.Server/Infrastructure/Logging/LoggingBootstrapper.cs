using Serilog;

namespace Mcp.DotNet.CliWorkshop.Server.Infrastructure.Logging;

/// <summary>
/// Bootstrapper for configuring Serilog logging for the MCP server.
/// </summary>
public static class LoggingBootstrapper
{
    /// <summary>
    /// Initializes Serilog logging with configuration from appsettings.json and environment variables.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    public static void Initialize(IConfiguration configuration)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        var filePath = configuration["Logging:File:Path"];
        var outputTemplate = configuration["Logging:Console:OutputTemplate"];
        var minimumLevel = configuration["Logging:MinimumLevel"];

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new InvalidOperationException("Logging:File:Path configuration is required.");
        }
        if (string.IsNullOrWhiteSpace(outputTemplate))
        {
            throw new InvalidOperationException("Logging:Console:OutputTemplate configuration is required.");
        }

        // Ensure log directory exists
        try
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
        catch
        {
            // Ignore directory creation errors, Serilog will handle file sink failures
        }

        var theme = ConsoleThemeFactory.CreateAnsiTheme();

        // Parse minimum level (default to Information)
        var logLevel = Enum.TryParse<Serilog.Events.LogEventLevel>(minimumLevel, out var level)
            ? level
            : Serilog.Events.LogEventLevel.Information;

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(logLevel)
            .WriteTo.Console(
                theme: theme,
                outputTemplate: outputTemplate,
                standardErrorFromLevel: Serilog.Events.LogEventLevel.Verbose) // Force ALL logs to stderr
            .WriteTo.File(filePath, rollingInterval: RollingInterval.Day)
            .Enrich.FromLogContext()
            .CreateLogger();
    }
}
