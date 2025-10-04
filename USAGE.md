# Usage Guide

## Getting Started

### 1. Ensure Prerequisites

```bash
# Verify .NET SDK is installed
dotnet --version
# Should show 9.0.302 or similar

# Check installed SDKs
dotnet --list-sdks

# Check installed runtimes
dotnet --list-runtimes
```

### 2. Start LM Studio

Before running the application:

1. Open LM Studio
2. Go to the "Local Server" tab
3. Load a model (recommended: models with function calling support like:
   - Mistral 7B Instruct
   - Llama 3.1 8B Instruct
   - Qwen 2.5 7B Instruct)
4. Click "Start Server"
5. Verify the server is running at `http://127.0.0.1:1234`

### 3. Run the Application

```bash
cd /Users/blazejewicz/develop/cli-mcp

# Build the solution
dotnet build

# Run the application
dotnet run --project src/DotNetCliMcp.App
```

## Example Conversation

```
=== DotNet CLI MCP Assistant ===
Connected to LM Studio at: http://127.0.0.1:1234
Type your questions about .NET SDK/Runtime (or 'exit' to quit)

You: What version of .NET do I have installed?