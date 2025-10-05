using Mcp.DotNet.CliWorkshop.Core.Services;
using Mcp.DotNet.CliWorkshop.Server.Infrastructure.Configuration;
using Mcp.DotNet.CliWorkshop.Server.Infrastructure.Logging;
using Mcp.DotNet.CliWorkshop.Server.Tools;
using Serilog;

try
{
    // Build configuration (JSON + environment variables)
    var configuration = ConfigurationFactory.Create();

    // Initialize Serilog logging (configured via appsettings.json)
    LoggingBootstrapper.Initialize(configuration);
    Log.Information("Starting MCP .NET CLI Server");
    Log.Information("Server: {Name} v{Version}",
        configuration["McpServer:Name"],
        configuration["McpServer:Version"]);

    var builder = Host.CreateApplicationBuilder(args);

    // Use Serilog for logging
    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog(Log.Logger);

    // Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
    builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

    // Register configuration
    builder.Services.AddSingleton(configuration);

    // Register our domain services
    builder.Services.AddSingleton<IDotNetCliService, DotNetCliService>();

    // Add the MCP services: the transport to use (stdio) and the tools to register.
    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithTools<DotNetCliMcpTools>();

    Log.Information("MCP server initialized successfully");
    Log.Information("Listening for MCP protocol messages on stdin/stdout...");

    await builder.Build().RunAsync().ConfigureAwait(false);

    Log.Information("MCP server shutting down");
}
catch (Exception ex)
{
    Log.Fatal(ex, "MCP server terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync().ConfigureAwait(false);
}
