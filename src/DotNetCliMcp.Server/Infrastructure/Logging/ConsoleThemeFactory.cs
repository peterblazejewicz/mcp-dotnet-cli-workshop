using Serilog.Sinks.SystemConsole.Themes;

namespace Mcp.DotNet.CliWorkshop.Server.Infrastructure.Logging;

/// <summary>
/// Factory for creating console themes for Serilog output.
/// </summary>
public static class ConsoleThemeFactory
{
    /// <summary>
    /// Creates an ANSI-compatible console theme for colored log output.
    /// </summary>
    /// <returns>A configured console theme.</returns>
    public static AnsiConsoleTheme CreateAnsiTheme()
    {
        return new AnsiConsoleTheme(
            new Dictionary<ConsoleThemeStyle, string>
            {
                [ConsoleThemeStyle.Text] = "\x1b[37m",          // White
                [ConsoleThemeStyle.SecondaryText] = "\x1b[90m", // Gray
                [ConsoleThemeStyle.TertiaryText] = "\x1b[90m",  // Gray
                [ConsoleThemeStyle.Invalid] = "\x1b[33m",       // Yellow
                [ConsoleThemeStyle.Null] = "\x1b[95m",          // Bright magenta
                [ConsoleThemeStyle.Name] = "\x1b[37m",          // White
                [ConsoleThemeStyle.String] = "\x1b[36m",        // Cyan
                [ConsoleThemeStyle.Number] = "\x1b[95m",        // Bright magenta
                [ConsoleThemeStyle.Boolean] = "\x1b[95m",       // Bright magenta
                [ConsoleThemeStyle.Scalar] = "\x1b[95m",        // Bright magenta
                [ConsoleThemeStyle.LevelVerbose] = "\x1b[34m",  // Blue
                [ConsoleThemeStyle.LevelDebug] = "\x1b[90m",    // Gray
                [ConsoleThemeStyle.LevelInformation] = "\x1b[36m", // Cyan
                [ConsoleThemeStyle.LevelWarning] = "\x1b[33m",  // Yellow
                [ConsoleThemeStyle.LevelError] = "\x1b[31m",    // Red
                [ConsoleThemeStyle.LevelFatal] = "\x1b[97;41m"  // White on red background
            }
        );
    }
}
