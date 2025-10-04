# Logging Improvements

## Overview
This document describes the improvements made to consolidate logging in the CLI-MCP application. Previously, the application used a mix of `Console.WriteLine()` and structured logging via `logger.LogInformation()`, which created inconsistent output formatting and made it difficult to differentiate between different types of messages.

## Changes Made

### 1. Unified Logging Strategy
- **Removed**: Direct `Console.WriteLine()` calls throughout the application
- **Added**: Consistent use of Serilog's structured logging with `ILogger` interface
- **Result**: All log messages now flow through Serilog, enabling consistent formatting and filtering

### 2. Color-Coded Output
We've implemented ANSI color coding to visually differentiate message types:

| Message Type | Color | Usage |
|--------------|-------|-------|
| **Information** | Cyan (36m) | General application messages, status updates |
| **Warning** | Yellow (33m) | Important notices, validation messages |
| **Error** | Red (31m) | Error messages, exceptions |
| **Fatal** | White on Red (97;91m) | Critical application failures |
| **User Prompt** | Green (32m) | "You:" prompt for user input |
| **Assistant Response** | Magenta (35m) | AI assistant replies |

### 3. Custom ANSI Theme
```csharp
var theme = new AnsiConsoleTheme(new Dictionary<ConsoleThemeStyle, string>
{
    [ConsoleThemeStyle.LevelInformation] = "\x1b[36m",  // Cyan
    [ConsoleThemeStyle.LevelWarning] = "\x1b[33m",      // Yellow
    [ConsoleThemeStyle.LevelError] = "\x1b[31m",        // Red
    [ConsoleThemeStyle.LevelFatal] = "\x1b[97;91m"      // White on Red
});
```

### 4. Structured Logging Benefits
All log messages now use structured logging with semantic parameters:

**Before:**
```csharp
Console.WriteLine("Connected to LM Studio at: " + lmStudioEndpoint);
```

**After:**
```csharp
logger.LogInformation("Connected to LM Studio at: {Endpoint}", lmStudioEndpoint);
```

**Benefits:**
- Parameters are properly typed and indexed
- Better searchability in log files
- Easier to parse and analyze programmatically
- Consistent formatting across all outputs

### 5. Exception Handling
Error messages now use appropriate log levels:
- `LogError()` for error details and technical information
- `LogWarning()` for troubleshooting suggestions and verification steps

Example:
```csharp
catch (HttpRequestException ex)
{
    logger.LogError(ex, "HTTP request failed to LM Studio");
    logger.LogError("Error: Could not connect to LM Studio.");
    logger.LogWarning("Please ensure:");
    logger.LogWarning("  - LM Studio is running");
    logger.LogWarning("  - The endpoint {Endpoint} is accessible", lmStudioEndpoint);
}
```

### 6. Interactive Elements
For interactive prompts that require inline input (like "You: "), we kept minimal Console usage with ANSI color codes:

```csharp
System.Console.Write("\x1b[32mYou: \x1b[0m");  // Green prompt
var userInput = Console.ReadLine();
```

For assistant responses, we use direct Console output with color coding to maintain the chat-like UX:

```csharp
System.Console.WriteLine($"\x1b[35m\nAssistant: {response.Content}\x1b[0m");
```

## Configuration

### Output Template
```csharp
.WriteTo.Console(
    theme: theme,
    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
```

This template provides:
- Timestamp in HH:mm:ss format
- Log level (INF, WRN, ERR, FTL)
- Structured message with JSON formatting for complex objects
- Exception details when present

### Log Files
Structured logs are also written to rotating daily files:
```csharp
.WriteTo.File("logs/dotnet-cli-mcp-.log", rollingInterval: RollingInterval.Day)
```

## Visual Output Example

```
[09:13:26 INF] === DotNet CLI MCP Assistant ===
[09:13:26 INF] Connected to LM Studio at: http://127.0.0.1:1234/v1
[09:13:26 WRN] Note: Make sure LM Studio is running with a model loaded
[09:13:26 INF] Type your questions about .NET SDK/Runtime (or 'exit' to quit)

You: what SDKs are installed?