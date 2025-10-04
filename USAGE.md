# Usage Guide

## Getting Started

### 0. Onboard with the collaborator setup (recommended)

If you have PowerShell Core (pwsh):

```bash
# From repo root
pwsh -File scripts/setup-collaborator.ps1
```

- This verifies .NET 9+, restores tools and packages, formats code, builds, and runs tests.
- To add a pre-commit hook that enforces formatting and tests:

```bash
pwsh -File scripts/setup-collaborator.ps1 -InstallGitHooks
```

If you don’t have pwsh yet on macOS: `brew install --cask powershell`. On Linux/Windows, install PowerShell 7+ from Microsoft.

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
5. Verify the OpenAI-compatible API base is `http://127.0.0.1:1234/v1`

### 3. Run the Application

```bash
cd /Users/blazejewicz/develop/mcp-dotnet-cli-workshop

# Build the solution
dotnet build

# Run the application
dotnet run --project src/DotNetCliMcp.App
```

## Example Conversation

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
