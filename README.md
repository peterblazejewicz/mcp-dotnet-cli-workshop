# Prompt to .NET CLI with MCP

A .NET 9 sample that uses PromptBridge (our MCP tool) to turn natural language into `dotnet` CLI commands via Semantic Kernel, enabling LLM-powered interactions with your local .NET environment.

## Features

- 🤖 **Local LLM Integration**: Connects to LM Studio for privacy-focused AI interactions
- 🔧 **DotNet CLI Wrapper**: Query SDK versions, runtimes, and environment details
- 🧩 **MCP Functions**: Semantic Kernel plugin with tool calling support
- 📝 **Structured Logging**: Serilog with console and file output
- ✅ **Comprehensive Testing**: xUnit 3 with NSubstitute mocking

## Prerequisites

- .NET 9.0 SDK or later
- [LM Studio](https://lmstudio.ai/) running locally with a loaded model
- MacOS, Linux, or Windows

## Quick Start

### 0. One-shot collaborator setup (recommended)

If you have PowerShell Core (pwsh) installed, you can bootstrap everything with one command:

```bash
pwsh -File scripts/setup-collaborator.ps1
```

- Adds no global settings; only affects this repo
- Restores local tools, formats code, builds, and runs tests
- Optionally install git hooks:

```bash
pwsh -File scripts/setup-collaborator.ps1 -InstallGitHooks
```

Alternatively, proceed with the manual steps below.

### 1. Clone and Build

```bash
cd mcp-dotnet-cli-workshop
 dotnet restore
 dotnet build
```

### 2. Start LM Studio

1. Download and install LM Studio
2. Load your preferred LLM model
3. Start the local server (default: `http://127.0.0.1:1234`)

### 3. Run the Application

```bash
dotnet run --project src/DotNetCliMcp.App
```

### 4. Interact with the Assistant

```
$ dotnet run --project src/DotNetCliMcp.App

[16:11:36 INF] Starting Prompt to .NET CLI with MCP
[16:11:36 INF] Semantic Kernel initialized with 1 plugins
[16:11:36 INF] Available functions: get_dotnet_info, list_installed_sdks, list_installed_runtimes, check_sdk_version, get_latest_sdk
[16:11:36 INF] === Prompt to .NET CLI with MCP ===
[16:11:36 INF] Connected to LM Studio at: http://127.0.0.1:1234/v1
[16:11:36 WRN] Note: Make sure LM Studio is running with a model loaded
[16:11:36 INF] Type your questions about .NET SDK/Runtime (or 'exit' to quit)
[16:11:36 INF] 
You: What version of .NET do I have installed?
```

## Example Queries

- "What version of .NET do I have installed?"
- "List all installed .NET SDKs"
- "Do I have the latest dotnet runtime?"
- "My project requires .NET 8.0.202 SDK, would there be a problem?"
- "What runtimes are installed on my system?"

## Available MCP Functions

The following functions are exposed to the LLM:

| Function | Description |
|----------|-------------|
| `get_dotnet_info` | Get comprehensive .NET environment information |
| `list_installed_sdks` | List all installed .NET SDKs |
| `list_installed_runtimes` | List all installed runtimes |
| `check_sdk_version` | Check if a specific SDK version is installed |
| `get_latest_sdk` | Get the latest installed SDK version |

## Project Structure

```
mcp-dotnet-cli-workshop/
├── src/
│   ├── DotNetCliMcp.App/          # Console application
│   │   └── Program.cs              # Main entry point with SK setup
│   └── DotNetCliMcp.Core/         # Core library
│       ├── Services/               # CLI execution services
│       │   ├── IDotNetCliService.cs
│       │   └── DotNetCliService.cs
│       └── Plugins/                # SK plugins
│           └── DotNetCliPlugin.cs  # MCP function definitions
├── tests/
│   └── DotNetCliMcp.Core.Tests/   # Unit and integration tests
└── Mcp.DotNet.CliWorkshop.sln     # Solution file
```

## Configuration

### LM Studio Configuration

Edit `Program.cs` to change the LM Studio endpoint:

```csharp
const string lmStudioEndpoint = "http://127.0.0.1:1234";
```

### Logging Configuration

Logs are written to:
- **Console**: Information level and above
- **File**: `logs/mcp-dotnet-cli-workshop-{Date}.log` (daily rolling)

Adjust in `Program.cs`:

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()  // Change minimum level
    .WriteTo.Console()
.WriteTo.File("logs/mcp-dotnet-cli-workshop-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

## Development

### Run Tests

```bash
dotnet test
```

### Format Code

```bash
dotnet format
```

### Build Release

```bash
dotnet build -c Release
```

## Technology Stack

- **.NET 9.0**: Modern C# features (file-scoped namespaces, top-level statements, primary constructors)
- **Semantic Kernel 1.65+**: LLM orchestration and function calling
- **Microsoft.Extensions.AI**: Unified AI abstractions
- **Serilog 4.x**: Structured logging
- **xUnit 3**: Testing framework
- **NSubstitute**: Mocking library

## How It Works

1. **User Input**: You ask a question about .NET SDK/Runtime
2. **LLM Processing**: LM Studio's LLM receives the query and system instructions
3. **Tool Selection**: The LLM decides which MCP function(s) to call
4. **Function Execution**: Semantic Kernel automatically invokes the selected functions
5. **CLI Execution**: Functions execute `dotnet` commands via `Process`
6. **Response Generation**: Results are parsed and returned to the LLM
7. **Final Answer**: LLM synthesizes information into a natural language response

## Troubleshooting

### "Connection refused" error

Ensure LM Studio is running and the server is started:
- Check LM Studio's server status
- Verify the endpoint URL matches (default: `http://127.0.0.1:1234`)

### "dotnet command not found"

Ensure .NET SDK is installed and in your PATH:
```bash
dotnet --version
```

### No response from LLM

- Check that a model is loaded in LM Studio
- Verify the model supports function/tool calling
- Check logs in `logs/` directory for errors

## Contributing

This is a demonstration project. Feel free to fork and extend it with:
- Additional dotnet CLI commands
- Project file analysis
- NuGet package management
- Global tool operations

## License

MIT License - See LICENSE file for details

## Acknowledgments

- Built with [Semantic Kernel](https://github.com/microsoft/semantic-kernel)
- Powered by [LM Studio](https://lmstudio.ai/)
- Inspired by the Model Context Protocol (MCP) pattern
