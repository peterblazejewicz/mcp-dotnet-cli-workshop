# Prompt to .NET CLI with MCP - Copilot Instructions

## Project Overview

This is a .NET 9 application that wraps the `dotnet` CLI tool and exposes its capabilities through Microsoft Semantic Kernel as MCP (Model Context Protocol) compatible functions. In this sample we call the MCP tool PromptBridge. The application connects to a local LM Studio instance to enable LLM-powered interactions with .NET SDK/Runtime information.

## Architecture

### Core Components

1. **DotNetCliMcp.Core** (Class Library)
   - `Services/IDotNetCliService`: Interface for dotnet CLI operations
   - `Services/DotNetCliService`: Implementation that executes dotnet commands via Process
   - `Plugins/DotNetCliPlugin`: Semantic Kernel plugin that exposes CLI functions to LLMs

2. **DotNetCliMcp.App** (Console Application)
   - Interactive chat application using Semantic Kernel
- Configured to use local LM Studio at `http://127.0.0.1:1234/v1`
   - Implements automatic function calling via `ToolCallBehavior.AutoInvokeKernelFunctions`

3. **DotNetCliMcp.Core.Tests** (Test Project)
   - xUnit 3 tests
   - Uses NSubstitute for mocking
   - Integration tests that run against actual dotnet CLI

## Technology Stack

- **.NET 9.0**: Latest .NET SDK with modern C# features
- **Microsoft.SemanticKernel**: For LLM orchestration and function calling
- **Microsoft.Extensions.AI**: Unified AI abstractions
- **Serilog**: Structured logging to console and file
- **xUnit 3**: Testing framework
- **NSubstitute**: Mocking library

## Key Features

### MCP Functions Available to LLM

1. `get_dotnet_info` - Get comprehensive .NET environment information
2. `list_installed_sdks` - List all installed .NET SDKs
3. `list_installed_runtimes` - List all installed runtimes
4. `check_sdk_version` - Check if a specific SDK version is installed
5. `get_latest_sdk` - Get the latest installed SDK version

### Example User Queries

- "What version of .NET do I have installed?"
- "Do I have the latest dotnet runtime?"
- "My project requires .NET 8.0.202 SDK, would there be a problem?"
- "List all installed SDKs"

## Coding Conventions

- Use **file-scoped namespaces** (C# 10+)
- Use **primary constructors** where appropriate (C# 12+)
- Use **top-level statements** in Program.cs (C# 9+)
- Prefer **record types** for DTOs and immutable data
- Use **compiled regex** with `[GeneratedRegex]` for performance
- Follow **async/await** patterns consistently
- Use **ILogger** for all logging (backed by Serilog)

## Patterns & Practices

### Service Pattern
- Services implement interfaces for testability
- Constructor injection for dependencies
- Use `ILogger<T>` for structured logging

### Plugin Pattern
- SK plugins are classes with `[KernelFunction]` decorated methods
- Functions return JSON strings for LLM consumption
- Include comprehensive descriptions using `[Description]` attribute
- Handle exceptions gracefully and return error JSON

### Error Handling
- Log errors using structured logging
- Return user-friendly error messages
- Preserve stack traces in logs but not in user-facing responses

## Build & Run

```bash
# Build solution
dotnet build

# Run tests
dotnet test

# Run application (requires LM Studio running)
dotnet run --project src/DotNetCliMcp.App

# Format code
dotnet format
```

## Configuration

### LM Studio Setup
- Endpoint: `http://127.0.0.1:1234/v1`
- Uses OpenAI-compatible API
- No API key required (local deployment)
- Model selection happens in LM Studio UI

### Logging Configuration
- Console: Information level
- File: `logs/mcp-dotnet-cli-workshop-{Date}.log` with daily rolling
- Structured logging with Serilog

## Testing Strategy

- **Unit tests**: Test service logic with mocked dependencies
- **Integration tests**: Test actual dotnet CLI execution
- **Coverage targets**: Aim for 80%+ code coverage
- Run tests before commits

## Future Enhancements

Potential areas for expansion:
- Add support for `dotnet new` templates listing
- Add project file analysis (read .csproj)
- Support for NuGet package queries
- Global tool management
- Solution file (.sln) operations
- Migration assistant (suggest SDK upgrades)

## Notes

- This is a local-first application - no cloud dependencies
- All LLM interactions stay on the local machine
- The application can work offline if LM Studio is available
- MCP functions are synchronous wrappers around async operations

## Style Guidance

- Prefer diagrams over prose; use Mermaid for architecture and sequence flows.
- Keep README.md as the single source of truth. Do not create USAGE.md or a docs/ directory.
- Do not re-document Microsoft features; link to official Microsoft docs instead.
- When adding or updating features, update diagrams and provide minimal bullet points rather than long paragraphs.
- Place testing guidance at the end of README under "Next Steps / Testing" and keep it concise.
- Avoid duplicating content already covered in README.
- Keep examples minimal and task-focused when proposing changes in PRs.
