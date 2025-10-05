using DotNetCliMcp.App.Infrastructure.Logging;


try
{
    // Build configuration (JSON + environment variables)
    var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables()
        .Build();

    // Initialize logging
    using var loggerFactory = LoggingBootstrapper.Initialize(configuration);
    var logger = loggerFactory.CreateLogger<Program>();
    Log.Information("Starting Prompt to .NET CLI with MCP");

    // Configuration for LM Studio (local OpenAI-compatible endpoint)
    var endpoint = configuration["OpenAI:Endpoint"];
    var modelName = configuration["OpenAI:Model"];
    var apiKey = configuration["OpenAI:ApiKey"];

    // HTTP timeouts (configurable)
    var httpTimeoutSeconds = configuration.GetValue<int?>("OpenAI:HttpTimeoutSeconds") ?? 300;
    var connectTimeoutSeconds = configuration.GetValue<int?>("OpenAI:ConnectTimeoutSeconds") ?? 15;
    var httpTimeout = httpTimeoutSeconds <= 0
        ? Timeout.InfiniteTimeSpan
        : TimeSpan.FromSeconds(httpTimeoutSeconds);

    if (string.IsNullOrWhiteSpace(endpoint))
    {
        throw new InvalidOperationException("OpenAI:Endpoint configuration is required. Set it in appsettings.json or via environment variable OpenAI__Endpoint.");
    }
    if (string.IsNullOrWhiteSpace(modelName))
    {
        throw new InvalidOperationException("OpenAI:Model configuration is required. Set it in appsettings.json or via environment variable OpenAI__Model.");
    }
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        throw new InvalidOperationException("OpenAI:ApiKey configuration is required. Set it in appsettings.json or via environment variable OpenAI__ApiKey.");
    }

    // Create Semantic Kernel with OpenAI-compatible chat completion service
    var kernelBuilder = Kernel.CreateBuilder();

    // Configure OpenAI chat completion pointing to LM Studio
    // The endpoint should include /v1 to match OpenAI API structure
    // Configure resilient HttpClient for LM Studio
    var handler = new SocketsHttpHandler
    {
        ConnectTimeout = TimeSpan.FromSeconds(connectTimeoutSeconds),
        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
        PooledConnectionLifetime = TimeSpan.FromMinutes(10)
    };
    var httpClient = new HttpClient(handler)
    {
        Timeout = httpTimeout
    };

    kernelBuilder.AddOpenAIChatCompletion(
        modelId: modelName,
        apiKey: apiKey,
        endpoint: new Uri(endpoint),
        httpClient: httpClient
    );

    // Register services
    kernelBuilder.Services.AddSingleton(loggerFactory);
    kernelBuilder.Services.AddSingleton<IConfiguration>(configuration);
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

    // Create a chat history with enhanced v2 system prompt for better tool calling and reasoning suppression
    var history = new ChatHistory();
    history.AddSystemMessage(@"You are a .NET SDK assistant that provides information about installed .NET SDKs and runtimes.

## CRITICAL: Output Format Rules

**NEVER include reasoning tags in your output:**
- ❌ DO NOT use <think>, </think>, <reasoning>, or any XML-style tags
- ❌ DO NOT show your internal thought process
- ❌ DO NOT explain your tool selection logic to the user
- ✅ Output ONLY tool calls followed by natural language responses

**Output Structure:**
1. If a tool is needed: Output tool call and STOP immediately
2. After receiving tool result: Output a natural language response ONLY

## Available Tools

You have access to these functions to query .NET SDK information:

1. **DotNetCli_get_effective_sdk** ⭐ PREFERRED for ""which version"" questions
   - Parameters: {""workingDirectory"": ""/path""} (optional)
   - Returns: The SDK version that dotnet will use in the specified/current directory
   - Respects global.json and roll-forward rules
   - **Use when**: User asks ""which version"" (singular), ""what version do I have"", or ""what's the active version""

2. **DotNetCli_list_installed_sdks**
   - Parameters: None (use empty object {})
   - Returns: Complete list of all installed SDK versions with paths
   - **Use when**: User asks for ""versions"" (plural), ""list all"", ""show me all SDKs""

3. **DotNetCli_list_installed_runtimes**
   - Parameters: None (use empty object {})
   - Returns: All installed runtimes with names, versions, and paths
   - **Use when**: User asks about runtimes specifically

4. **DotNetCli_check_sdk_version**
   - Parameters: {""version"": ""X.Y.Z""} (required)
   - Example: {""version"": ""9.0.302""}
   - Returns: Boolean indicating if that specific version is installed
   - **Use when**: User mentions a specific version number to check

5. **DotNetCli_get_latest_sdk**
   - Parameters: None (use empty object {})
   - Returns: The highest version number among installed SDKs
   - **Use when**: User asks ""what's the latest"" or ""newest SDK""

6. **DotNetCli_get_dotnet_info**
   - Parameters: None (use empty object {})
   - Returns: Comprehensive environment info (SDK version, runtime, OS, architecture)
   - **Use when**: User asks for general environment or system information

## Tool Selection Guide

Identify the question type:
- ""which version"" (singular) → DotNetCli_get_effective_sdk
- ""list all"" / ""show all"" / ""versions"" (plural) → DotNetCli_list_installed_sdks
- ""do I have X.Y.Z"" → DotNetCli_check_sdk_version
- ""latest"" / ""newest"" → DotNetCli_get_latest_sdk
- ""runtimes"" → DotNetCli_list_installed_runtimes
- ""environment"" / ""system info"" → DotNetCli_get_dotnet_info

## Tool Call Rules

✅ **DO:**
- Use exact function names from the list above
- Call exactly ONE tool per question
- Use {} when no parameters are required
- Stop immediately after making tool call
- Wait for tool result before responding
- Call the tool again if the same question is repeated

❌ **DO NOT:**
- Include <think> or reasoning tags in ANY output
- Call multiple tools for one question
- Use placeholder names like 'tool_name'
- Add explanatory text before tool calls
- Get stuck deliberating whether to call a tool - just call it
- Respond before receiving the tool result

## Handling Repeated Questions

If the user asks the same question twice:
1. **Do NOT overthink it** - just call the tool again
2. **Do NOT get stuck in reasoning loops**
3. **Trust that the tool will provide current information**

## Response Guidelines

After receiving tool result:
- Start immediately with the answer (no preamble)
- Include relevant version numbers
- List SDKs in a clean format (comma-separated or bulleted)
- Keep it conversational but accurate
- Don't repeat information unnecessarily

## Examples

User: ""Show me all installed SDK versions""
→ Call: DotNetCli_list_installed_sdks with {}
→ Wait for result
→ Respond: ""You have 9 .NET SDK versions installed: 6.0.419, 8.0.120, 8.0.303, 8.0.403, 8.0.404, 9.0.100, 9.0.103, 9.0.203, and 9.0.302.""

User: ""Do I have .NET SDK 9.0.302?""
→ Call: DotNetCli_check_sdk_version with {""version"": ""9.0.302""}
→ Wait for result
→ Respond: ""Yes, .NET SDK 9.0.302 is installed on your system.""

User: ""What .NET version do I have?""
→ Call: DotNetCli_get_effective_sdk with {}
→ Wait for result
→ Respond: ""You're using .NET SDK version 9.0.302 in this directory.""

Remember: Your output should contain EITHER a tool call OR a natural language response. Never both in the same turn. Never reasoning tags.");

    logger.LogInformation("=== Prompt to .NET CLI with MCP ===");
    logger.LogInformation("Connected to LM Studio at: {Endpoint}", endpoint);
    logger.LogInformation("HTTP timeout: {HttpTimeoutSeconds}s (0=infinite), Connect timeout: {ConnectTimeoutSeconds}s", httpTimeoutSeconds, connectTimeoutSeconds);
    logger.LogWarning("Note: Make sure LM Studio is running with a model loaded");
    logger.LogInformation("Type your questions about .NET SDK/Runtime (or 'exit' to quit)");
    logger.LogInformation(string.Empty);

    // Interactive chat loop with optimized settings for tool calling
    // Note: For reasoning models like DeepSeek R1, avoid stop sequences that block reasoning
    // Configure generation settings (configurable via appsettings)
    var temperature = configuration.GetValue<double?>("OpenAI:Temperature") ?? 0.2;
    var maxTokens = configuration.GetValue<int?>("OpenAI:MaxTokens") ?? 1500;

    var settings = new OpenAIPromptExecutionSettings
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
        Temperature = temperature,
        MaxTokens = maxTokens
        // StopSequences removed - they prevent reasoning models from working
    };

    while (true)
    {
        // Use Console.Write for the prompt as it needs to stay on same line
        Console.Write("\x1b[32mYou: \x1b[0m");  // Green color for user prompt
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

            // Stream response with automatic function calling
            // Print label once, then stream tokens in magenta
            var sb = new System.Text.StringBuilder();
            var consoleLock = new object();
            Console.Write("\x1b[35m\nAssistant: ");

            // Inline spinner that appears during idle periods (e.g., while tools run)
            var lastChunkAt = DateTime.UtcNow;
            using var spinnerCts = new CancellationTokenSource();
            var spinnerRunning = false;
            var spinnerTask = Task.Run(async () =>
            {
                var frames = new[] { '|', '/', '-', '\\' };
                var idx = 0;
                while (!spinnerCts.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(125, spinnerCts.Token).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }

                    var idle = (DateTime.UtcNow - lastChunkAt).TotalMilliseconds > 500;
                    if (idle && !spinnerRunning)
                    {
                        spinnerRunning = true;
                        lock (consoleLock) { Console.Write(" "); } // allocate spinner cell
                    }

                    if (spinnerRunning)
                    {
                        var ch = frames[idx++ % frames.Length];
                        lock (consoleLock) { Console.Write($"\b{ch}"); }
                    }
                }
            }, spinnerCts.Token);

            await foreach (var update in chatService.GetStreamingChatMessageContentsAsync(
                history,
                settings,
                kernel
            ).ConfigureAwait(false))
            {
                var piece = update?.Content;
                if (!string.IsNullOrEmpty(piece))
                {
                    // Clear spinner before printing tokens
                    if (spinnerRunning)
                    {
                        spinnerRunning = false;
                        lock (consoleLock) { Console.Write("\b \b"); }
                    }

                    lock (consoleLock) { Console.Write(piece); }
                    sb.Append(piece);
                    lastChunkAt = DateTime.UtcNow;
                }
            }

            // Stop spinner and clean up
            spinnerCts.Cancel();
            try { await spinnerTask.ConfigureAwait(false); } catch { }
            if (spinnerRunning)
            {
                lock (consoleLock) { Console.Write("\b \b"); }
            }

            // Reset color and finalize lines
            Console.WriteLine("\x1b[0m");
            Console.WriteLine();

            var content = sb.ToString();

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

            if (!string.IsNullOrEmpty(content))
            {
                history.AddAssistantMessage(content);
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
            logger.LogWarning("  - LM Studio is running at {Endpoint}", endpoint);
            logger.LogWarning("  - A model is loaded in LM Studio");
            logger.LogWarning("  - The model supports chat completions");
            // Remove the last user message to allow retry
            if (history.Count > 0)
            {
                history.RemoveAt(history.Count - 1);
            }
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Request timed out after {TimeoutSeconds}s (HttpClient)", httpTimeoutSeconds);
            logger.LogError("Error: The LLM request exceeded the configured timeout.");
            logger.LogWarning("You can increase OpenAI:HttpTimeoutSeconds (env: OpenAI__HttpTimeoutSeconds) or set it to 0 for no timeout.");
            logger.LogWarning("LM Studio logs showing 'Client disconnected' at ~{TimeoutSeconds}s likely correspond to this timeout.", httpTimeoutSeconds);
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
            logger.LogWarning("  - The endpoint {Endpoint} is accessible", endpoint);
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
            logger.LogWarning("  - The endpoint {Endpoint} is accessible", endpoint);
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
    await Log.CloseAndFlushAsync().ConfigureAwait(false);
}

return 0;
