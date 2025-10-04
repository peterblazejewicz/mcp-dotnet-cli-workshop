namespace DotNetCliMcp.App.Infrastructure.Logging;

/// <summary>
/// Factory responsible for creating console themes used by Serilog.
/// </summary>
public static class ConsoleThemeFactory
{
    /// <summary>
    /// Creates the ANSI console theme used by the application.
    /// </summary>
    public static ConsoleTheme CreateAnsiTheme()
    {
        return new AnsiConsoleTheme(new Dictionary<ConsoleThemeStyle, string>
        {
            [ConsoleThemeStyle.Text] = "\e[0m",
            [ConsoleThemeStyle.SecondaryText] = "\e[90m",
            [ConsoleThemeStyle.TertiaryText] = "\e[90m",
            [ConsoleThemeStyle.Invalid] = "\e[33m",
            [ConsoleThemeStyle.Null] = "\e[95m",
            [ConsoleThemeStyle.Name] = "\e[93m",
            [ConsoleThemeStyle.String] = "\e[96m",
            [ConsoleThemeStyle.Number] = "\e[95m",
            [ConsoleThemeStyle.Boolean] = "\e[95m",
            [ConsoleThemeStyle.Scalar] = "\e[95m",
            [ConsoleThemeStyle.LevelVerbose] = "\e[37m",
            [ConsoleThemeStyle.LevelDebug] = "\e[37m",
            [ConsoleThemeStyle.LevelInformation] = "\e[36m",
            [ConsoleThemeStyle.LevelWarning] = "\e[33m",
            [ConsoleThemeStyle.LevelError] = "\e[31m",
            [ConsoleThemeStyle.LevelFatal] = "\e[97;91m"
        });
    }
}
