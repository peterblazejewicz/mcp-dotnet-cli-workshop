namespace DotNetCliMcp.App.Infrastructure.Logging;

public static class LoggingBootstrapper
{
    public static SerilogLoggerFactory Initialize(IConfiguration configuration)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        var filePath = configuration["Logging:File:Path"];
        var outputTemplate = configuration["Logging:Console:OutputTemplate"];

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

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(
                theme: theme,
                outputTemplate: outputTemplate)
            .WriteTo.File(filePath, rollingInterval: RollingInterval.Day)
            .Enrich.FromLogContext()
            .CreateLogger();

        return new SerilogLoggerFactory(Log.Logger);
    }
}
