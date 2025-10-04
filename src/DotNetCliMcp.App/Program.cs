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

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
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
    const string lmStudioEndpoint = "http://127.0.0.1:1234";
    const string modelName = "local-model"; // LM Studio uses this as default

    // Create Semantic Kernel with OpenAI-compatible chat completion service
    var kernelBuilder = Kernel.CreateBuilder();

    // Configure OpenAI chat completion pointing to LM Studio
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

    // Create chat history
    var history = new ChatHistory();
    history.AddSystemMessage(@"You are a helpful assistant that can answer questions about .NET SDK and runtime installations.
You have access to tools that can query the local .NET environment.
Use these tools to provide accurate, up-to-date information about installed .NET versions.

Available tools:
- get_dotnet_info: Get comprehensive .NET environment information
- list_installed_sdks: List all installed .NET SDKs
- list_installed_runtimes: List all installed .NET runtimes
- check_sdk_version: Check if a specific SDK version is installed
- get_latest_sdk: Get the latest installed SDK version

When answering questions:
1. Use the appropriate tool to gather information
2. Provide clear, concise answers
3. Include relevant version numbers and paths when applicable
4. If a user asks about compatibility or requirements, explain clearly");

    Console.WriteLine("=== DotNet CLI MCP Assistant ===");
    Console.WriteLine("Connected to LM Studio at: " + lmStudioEndpoint);
    Console.WriteLine("Type your questions about .NET SDK/Runtime (or 'exit' to quit)");
    Console.WriteLine();

    // Interactive chat loop
    var settings = new OpenAIPromptExecutionSettings
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
        Temperature = 0.7,
        MaxTokens = 2000
    };

    while (true)
    {
        Console.Write("You: ");
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

            history.AddAssistantMessage(response.Content ?? "No response");

            Console.WriteLine($"\nAssistant: {response.Content}");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing chat message");
            Console.WriteLine($"\nError: {ex.Message}");
            Console.WriteLine();
        }
    }

    Console.WriteLine("\nGoodbye!");
    logger.LogInformation("Application shutting down");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    Console.WriteLine($"Fatal error: {ex.Message}");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;
