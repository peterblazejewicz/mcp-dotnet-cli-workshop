using DotNetCliMcp.Core.Plugins;
using DotNetCliMcp.Core.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;

// Configure Serilog for structured logging with colored console output
var theme = new AnsiConsoleTheme(new Dictionary<ConsoleThemeStyle, string>
{
    [ConsoleThemeStyle.Text] = "\x1b[0m",
    [ConsoleThemeStyle.SecondaryText] = "\x1b[90m",
    [ConsoleThemeStyle.TertiaryText] = "\x1b[90m",
    [ConsoleThemeStyle.Invalid] = "\x1b[33m",
    [ConsoleThemeStyle.Null] = "\x1b[95m",
    [ConsoleThemeStyle.Name] = "\x1b[93m",
    [ConsoleThemeStyle.String] = "\x1b[96m",
    [ConsoleThemeStyle.Number] = "\x1b[95m",
    [ConsoleThemeStyle.Boolean] = "\x1b[95m",
    [ConsoleThemeStyle.Scalar] = "\x1b[95m",
    [ConsoleThemeStyle.LevelVerbose] = "\x1b[37m",
    [ConsoleThemeStyle.LevelDebug] = "\x1b[37m",
    [ConsoleThemeStyle.LevelInformation] = "\x1b[36m",  // Cyan for info
    [ConsoleThemeStyle.LevelWarning] = "\x1b[33m",      // Yellow for warnings
    [ConsoleThemeStyle.LevelError] = "\x1b[31m",        // Red for errors
    [ConsoleThemeStyle.LevelFatal] = "\x1b[97;91m"      // White on Red for fatal
});

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(
        theme: theme,
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/dotnet-cli-mcp-.log", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("Starting DotNet CLI MCP Application");

    // Create logger factory
    using var loggerFactory = new SerilogLoggerFactory(Log.Logger);
    var logger = loggerFactory.CreateLogger<Program>();

    // Configuration for LM Studio (local OpenAI-compatible endpoint)
    // LM Studio expects the base URL without /v1 path - it will append it automatically
    const string lmStudioEndpoint = "http://127.0.0.1:1234/v1";
    const string modelName = "local-model"; // LM Studio uses this as default

    // Create Semantic Kernel with OpenAI-compatible chat completion service
    var kernelBuilder = Kernel.CreateBuilder();

    // Configure OpenAI chat completion pointing to LM Studio
    // The endpoint should include /v1 to match OpenAI API structure
    kernelBuilder.AddOpenAIChatCompletion(
        modelId: modelName,
        apiKey: "not-needed", // LM Studio doesn't require API key
        endpoint: new Uri(lmStudioEndpoint),
        httpClient: new HttpClient()
    );

    // Register services
    kernelBuilder.Services.AddSingleton(loggerFactory);
    kernelBuilder.Services.AddSingleton<IDotNetCliService>(sp =>
        new DotNetCliService(loggerFactory.CreateLogger<DotNetCliService>()));

    var kernel = kernelBuilder.Build();

    // Create and import the DotNet CLI plugin
    var cliService = new DotNetCliService(loggerFactory.CreateLogger<DotNetCliService>());
    var dotnetCliPlugin = new DotNetCliPlugin(
        cliService,
        loggerFactory.CreateLogger<DotNetCliPlugin>()
    );

    kernel.Plugins.AddFromObject(dotnetCliPlugin, "DotNetCli");

    logger.LogInformation("Semantic Kernel initialized with {PluginCount} plugins", kernel.Plugins.Count);
    logger.LogInformation("Available functions: {Functions}",
        string.Join(", ", kernel.Plugins.SelectMany(p => p.Select(f => f.Name))));

    // Get chat completion service
    var chatService = kernel.GetRequiredService<IChatCompletionService>();

    // Create chat history with improved system prompt for better tool calling
    var history = new ChatHistory();
    history.AddSystemMessage(@"You are a .NET SDK assistant that answers questions about installed .NET SDKs and runtimes.

## Available Tools

You MUST call ONE of these functions to answer user questions:

1. **DotNetCli_get_dotnet_info**
   - No parameters required
   - Returns: SDK version, runtime version, OS, architecture

2. **DotNetCli_list_installed_sdks**
   - No parameters required
   - Returns: List of all installed SDKs with versions and paths

3. **DotNetCli_list_installed_runtimes**
   - No parameters required
   - Returns: List of all installed runtimes with names, versions, paths

4. **DotNetCli_check_sdk_version**
   - REQUIRES parameter: {""version"": ""X.Y.Z""}
   - Example: {""version"": ""9.0.302""}
   - Returns: Whether that specific version is installed

5. **DotNetCli_get_latest_sdk**
   - No parameters required
   - Returns: The latest SDK version installed

## CRITICAL RULES

1. ✅ Use EXACT function names: DotNetCli_<name> (underscore, not hyphen)
2. ✅ Call ONE tool per user question
3. ✅ For no parameters: use empty object {}
4. ✅ For parameters: use proper JSON like {""version"": ""9.0.302""}
5. ❌ DO NOT call 'tool_name' or make up function names
6. ❌ DO NOT call the same function twice
7. ❌ DO NOT include <think>, <reasoning>, or XML tags in responses
8. ❌ DO NOT respond before receiving tool results

## How to Respond

Step 1: Read user question
Step 2: Choose correct tool from list above
Step 3: Call tool with proper format
Step 4: WAIT for result
Step 5: Answer naturally based ONLY on the tool result

## Examples

User: ""What .NET SDK version do I have?""
→ Call: DotNetCli_list_installed_sdks with {}
→ Wait for result
→ Respond: ""You have the following .NET SDK versions installed: [list from result]""

User: ""Do I have .NET 9.0.302 installed?""
→ Call: DotNetCli_check_sdk_version with {""version"": ""9.0.302""}
→ Wait for result
→ Respond: ""Yes, .NET SDK 9.0.302 is installed"" OR ""No, that version is not installed""

User: ""What's my latest SDK?""
→ Call: DotNetCli_get_latest_sdk with {}
→ Wait for result
→ Respond: ""Your latest .NET SDK is version [version from result]""

## Response Style

- Be concise and helpful
- Include version numbers when relevant
- Include paths if helpful
- Explain compatibility issues clearly
- Keep responses conversational but accurate

Remember: ONE tool call, EXACT function name, WAIT for results!");

    logger.LogInformation("=== DotNet CLI MCP Assistant ===");
    logger.LogInformation("Connected to LM Studio at: {Endpoint}", lmStudioEndpoint);
    logger.LogWarning("Note: Make sure LM Studio is running with a model loaded");
    logger.LogInformation("Type your questions about .NET SDK/Runtime (or 'exit' to quit)");
    logger.LogInformation(string.Empty);

    // Interactive chat loop with optimized settings for tool calling
    var settings = new OpenAIPromptExecutionSettings
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
        Temperature = 0.1,  // Lower temperature for more deterministic tool selection
        MaxTokens = 1000,   // Reduced tokens to discourage lengthy reasoning
        StopSequences = ["<think>", "</think>", "<reasoning>", "</reasoning>"]  // Prevent reasoning tag leakage
    };

    while (true)
    {
        // Use Console.Write for the prompt as it needs to stay on same line
        System.Console.Write("\x1b[32mYou: \x1b[0m");  // Green color for user prompt
        var userInput = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(userInput) || userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("User requested exit");
            break;
        }

        history.AddUserMessage(userInput);

        try
        {
            logger.LogInformation("Processing user query: {Query}", userInput);

            // Get response with automatic function calling
            var response = await chatService.GetChatMessageContentAsync(
                history,
                settings,
                kernel
            );

            if (response?.Content != null)
            {
                var content = response.Content;
                
                // Strip out any reasoning tags that may have leaked through
                content = System.Text.RegularExpressions.Regex.Replace(
                    content, 
                    @"<think>.*?</think>", 
                    "", 
                    System.Text.RegularExpressions.RegexOptions.Singleline
                );
                content = System.Text.RegularExpressions.Regex.Replace(
                    content, 
                    @"<reasoning>.*?</reasoning>", 
                    "", 
                    System.Text.RegularExpressions.RegexOptions.Singleline
                );
                
                // Trim any extra whitespace from cleaning
                content = content.Trim();
                
                history.AddAssistantMessage(content);
                // Use custom color for assistant responses (magenta)
                System.Console.WriteLine($"\x1b[35m\nAssistant: {content}\x1b[0m");
                System.Console.WriteLine();
            }
            else
            {
                logger.LogWarning("Received empty or null response from chat service");
                logger.LogWarning("Assistant: I received an empty response. Please try again.");
                // Remove the last user message to allow retry
                if (history.Count > 0)
                {
                    history.RemoveAt(history.Count - 1);
                }
            }
        }
        catch (ArgumentOutOfRangeException ex) when (ex.Message.Contains("index"))
        {
            // This specific error occurs when LM Studio returns a response format
            // that's incompatible with the OpenAI SDK's expected structure
            logger.LogError(ex, "OpenAI SDK compatibility issue with LM Studio response");
            logger.LogError("Error: The response from LM Studio is not compatible with the expected OpenAI format.");
            logger.LogError("Possible causes:");
            logger.LogError("  1. LM Studio is not running or no model is loaded");
            logger.LogError("  2. LM Studio endpoint is incorrect (check if it should be /v1)");
            logger.LogError("  3. The loaded model doesn't support function calling");
            logger.LogError("  4. LM Studio version incompatibility");
            logger.LogWarning("Please verify:");
            logger.LogWarning("  - LM Studio is running at {Endpoint}", lmStudioEndpoint);
            logger.LogWarning("  - A model is loaded in LM Studio");
            logger.LogWarning("  - The model supports chat completions");
            // Remove the last user message to allow retry
            if (history.Count > 0)
            {
                history.RemoveAt(history.Count - 1);
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed to LM Studio");
            logger.LogError("Error: Could not connect to LM Studio.");
            logger.LogWarning("Please ensure:");
            logger.LogWarning("  - LM Studio is running");
            logger.LogWarning("  - The endpoint {Endpoint} is accessible", lmStudioEndpoint);
            logger.LogWarning("  - A model is loaded in LM Studio");
            // Remove the last user message to allow retry
            if (history.Count > 0)
            {
                history.RemoveAt(history.Count - 1);
            }
        }
        catch (HttpOperationException ex)
        {
            logger.LogError(ex, "HTTP operation failed when communicating with LM Studio");
            logger.LogError("Error: Failed to communicate with LM Studio.");
            logger.LogError("Details: {Message}", ex.Message);
            logger.LogWarning("Please ensure:");
            logger.LogWarning("  - LM Studio is running");
            logger.LogWarning("  - The endpoint {Endpoint} is accessible", lmStudioEndpoint);
            logger.LogWarning("  - A model is loaded in LM Studio");
            logger.LogWarning("  - The model is compatible with OpenAI chat completions");
            // Remove the last user message to allow retry
            if (history.Count > 0)
            {
                history.RemoveAt(history.Count - 1);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing chat message");
            logger.LogError("Error: {Message}", ex.Message);
            logger.LogError("Type: {ExceptionType}", ex.GetType().Name);
            if (ex.InnerException != null)
            {
                logger.LogError("Inner: {InnerMessage}", ex.InnerException.Message);
            }
            // Remove the last user message to allow retry
            if (history.Count > 0)
            {
                history.RemoveAt(history.Count - 1);
            }
        }
    }

    logger.LogInformation("\nGoodbye!");
    logger.LogInformation("Application shutting down");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    Log.Fatal("Fatal error: {Message}", ex.Message);
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;
