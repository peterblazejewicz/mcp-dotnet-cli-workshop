# Prompt to .NET CLI with MCP

A .NET 9 sample that turns natural language into `dotnet` CLI commands via Semantic Kernel + local LLM, enabling AI-powered interactions with your .NET environment.

> **Workshop Materials**: This project serves as the foundation for a hands-on workshop teaching MCP integration, Semantic Kernel agents, and LLM tool calling. See [WORKSHOP-DRAFT.md](WORKSHOP-DRAFT.md) for the full workshop curriculum.

![Prompt to .NET CLI with MCP](./assets/lm-studio-chat.png)

![MCP Inspector with .NET CLI MCP](./assets/mcp-inspector.png)

## How It Works

> **TL;DR;**: Two modes in one repo: (1) **MCP Server** - exposes .NET CLI tools via stdio transport for MCP clients (Claude, Warp, LM Studio), and (2) **SK Chat App** - standalone Semantic Kernel demo with auto-function calling to query your .NET environment using natural language + local LLM.

- The LLM chooses an MCP function based on your question.
- Semantic Kernel auto-invokes the function and returns structured results.
- The app summarizes the results into a concise answer.

```bash
dotnet run --project src\DotNetCliMcp.App\DotNetCliMcp.App.csproj

[18:41:15 INF] Starting Prompt to .NET CLI with MCP
[18:41:15 INF] Semantic Kernel initialized with 1 plugins
[18:41:15 INF] Available functions: get_dotnet_info, list_installed_sdks, list_installed_runtimes, check_sdk_version, get_latest_sdk, get_effective_sdk
[18:41:15 INF] === Prompt to .NET CLI with MCP ===
[18:41:15 INF] Connected to LM Studio at: http://127.0.0.1:1234/v1
[18:41:15 INF] HTTP timeout: 300s (0=infinite), Connect timeout: 15s
[18:41:15 WRN] Note: Make sure LM Studio is running with a model loaded
[18:41:15 INF] Type your questions about .NET SDK/Runtime (or 'exit' to quit)
[18:41:15 INF] 
You: list all my installed .net runtimes
[18:41:24 INF] Processing user query: list all my installed .net runtimes

Assistant: \[18:41:25 INF] Plugin function list_installed_runtimes invoked
[18:41:25 INF] Executing dotnet --list-runtimes
[18:41:25 INF] Found 9 installed runtimes
You have the following .NET runtimes installed:

- **Microsoft.AspNetCore.App** 8.0.19  
- **Microsoft.AspNetCore.App** 9.0.8  
- **Microsoft.AspNetCore.App** 10.0.0‑preview.7.25380.108  

- **Microsoft.NETCore.App** 8.0.19  
- **Microsoft.NETCore.App** 9.0.8  
- **Microsoft.NETCore.App** 10.0.0‑preview.7.25380.108  

- **Microsoft.WindowsDesktop.App** 8.0.19  
- **Microsoft.WindowsDesktop.App** 9.0.8  
- **Microsoft.WindowsDesktop.App** 10.0.0‑preview.7.25380.108  

All are located under `C:\Program Files\dotnet\shared`.
```

### You can have fun

```console
# Example (Polish)
User: jaki dzisiaj jest dzień tygodnia?
Assistant: Dziś jest niedziela.
```

```mermaid
sequenceDiagram
  participant U as User
  participant A as App (Console)
  participant L as LLM (LM Studio)
  participant K as Semantic Kernel
  participant P as DotNetCliPlugin
  participant D as dotnet CLI

  U->>A: Ask "List all installed .NET SDKs"
  A->>L: Prompt + function schemas
  L->>K: Tool call: DotNetCli_list_installed_sdks
  K->>P: Invoke kernel function
  P->>D: Run "dotnet --list-sdks"
  D-->>P: Raw output
  P-->>K: Parsed JSON result
  K-->>L: Tool result
  L-->>A: Natural language answer
  A-->>U: Versions listed
```

## Features

- **Local LLM Integration**: Connects to LM Studio for privacy-focused AI interactions
- **MCP Server**: Exposes .NET CLI capabilities via stdio transport for MCP clients
- **DotNet CLI Wrapper**: Query SDK versions, runtimes, and environment details
- **MCP Functions**: Semantic Kernel plugin with tool calling support
- **Configuration Providers**: Uses appsettings.json and environment variables for flexible configuration
- **Enhanced System Prompts**: Optimized prompts for better tool calling and reasoning suppression
- **Structured Logging**: Serilog with console and file output
- **Comprehensive Testing**: xUnit 3 with NSubstitute mocking

## Architecture

### Two Modes: App vs Server

```mermaid
flowchart TB
  subgraph app["DotNetCliMcp.App (Standalone Demo)"]
    user[Developer] -->|Natural language| console[Console App]
    console --> sk[Semantic Kernel]
    sk <-->|OpenAI API| lms[LM Studio]
    sk --> plugin[DotNetCliPlugin]
  end
  
  subgraph server["DotNetCliMcp.Server (MCP Server)"]
    client[MCP Client<br/>Claude/Warp/etc] <-->|stdio<br/>JSON-RPC| mcpserver[MCP Server]
    mcpserver --> tools[DotNetCliMcpTools]
  end
  
  plugin --> service[DotNetCliService]
  tools --> service
  service -->|Process.Start| cli[dotnet CLI]
  
  style app fill:#e1f5ff
  style server fill:#fff4e1
```

### MCP Server Flow (stdio transport)

```mermaid
sequenceDiagram
  participant C as MCP Client<br/>(Claude/Warp)
  participant S as MCP Server<br/>(stdio)
  participant T as DotNetCliMcpTools
  participant SVC as DotNetCliService
  participant CLI as dotnet CLI

  Note over C,S: stdin/stdout = JSON-RPC<br/>stderr = logs
  
  C->>+S: initialize (via stdin)
  S-->>-C: capabilities (via stdout)
  
  C->>+S: tools/list
  S-->>-C: 6 MCP tools
  
  C->>+S: tools/call: list_installed_sdks
  S->>+T: Execute tool
  T->>+SVC: ListInstalledSdksAsync()
  SVC->>+CLI: dotnet --list-sdks
  CLI-->>-SVC: Raw output
  SVC-->>-T: List<SdkInfo>
  T-->>-S: JSON result
  S-->>-C: Tool response (via stdout)
  
  Note over S: All logs → stderr
```

## Prerequisites

- .NET 9.0 SDK
- [LM Studio](https://lmstudio.ai/) with a loaded model at `http://127.0.0.1:1234/v1`

## Quick Start

### Run Semantic Kernel Chat App

```bash
# One-shot setup (requires pwsh)
pwsh -File scripts/setup-collaborator.ps1

# Or manually
dotnet build
dotnet run --project src/DotNetCliMcp.App
```

Try prompts:
- List all installed .NET SDKs
- Do I have .NET 8.0.413 installed?
- Jaki jest mój aktualny SDK?

### Run MCP Server

```bash
# Run MCP server (for use with Claude/Warp/LM Studio)
dotnet run --project src/DotNetCliMcp.Server

# Or test with MCP Inspector
npx @modelcontextprotocol/inspector dotnet run --project src/DotNetCliMcp.Server
```

## Available MCP Functions

```mermaid
flowchart LR
  subgraph tools["6 MCP Tools"]
    direction TB
    t1[list_installed_sdks]
    t2[list_installed_runtimes]
    t3[get_effective_sdk*]
    t4[get_dotnet_info]
    t5[check_sdk_version]
    t6[get_latest_sdk]
  end
  
  subgraph cli["dotnet CLI"]
    direction TB
    c1[--list-sdks]
    c2[--list-runtimes]
    c3[--version]
    c4[--info]
    c5[derived]
    c6[derived]
  end
  
  t1 --> c1
  t2 --> c2
  t3 --> c3
  t4 --> c4
  t5 --> c5
  t6 --> c6
  
  style t3 fill:#ffe1e1
```

\* = Respects `global.json` configuration

## Project Structure

```mermaid
flowchart TB
  subgraph app["DotNetCliMcp.App"]
    app_infra[Infrastructure]
    app_prog[Program.cs<br/>SK + Chat Loop]
  end
  
  subgraph server["DotNetCliMcp.Server"]
    srv_infra[Infrastructure]
    srv_tools[Tools<br/>DotNetCliMcpTools]
    srv_prog[Program.cs<br/>MCP Entrypoint]
  end
  
  subgraph core["DotNetCliMcp.Core (Shared)"]
    contracts[Contracts<br/>DTOs]
    services[Services<br/>DotNetCliService]
    plugins[Plugins<br/>DotNetCliPlugin]
  end
  
  subgraph tests["DotNetCliMcp.Core.Tests"]
    integration[Integration Tests]
    unit[Unit Tests]
  end
  
  app --> core
  server --> core
  tests --> core
  
  style core fill:#ffd700
  style app fill:#e1f5ff
  style server fill:#fff4e1
```

## Configuration

### stdio Transport (Critical for MCP)

```mermaid
flowchart LR
  subgraph client["MCP Client Process"]
    c[Claude/Warp/etc]
  end
  
  subgraph server["MCP Server Process"]
    direction TB
    stdin[stdin<br/>JSON-RPC requests]
    stdout[stdout<br/>JSON-RPC responses]
    stderr[stderr<br/>Logs/diagnostics]
  end
  
  c -->|write| stdin
  stdout -->|read| c
  stderr -.->|ignore/log| c
  
  style stdin fill:#d4edda
  style stdout fill:#d4edda
  style stderr fill:#fff3cd
```

⚠️ **Critical**: stdout must contain ONLY JSON-RPC. All logs go to stderr.

### DotNetCliMcp.App (LM Studio Demo)

```bash
export OpenAI__Endpoint="http://127.0.0.1:1234/v1"
export OpenAI__Model="your-model-name"
```

### DotNetCliMcp.Server (MCP Server)

```bash
# Tool-specific prefix prevents conflicts
export MCPDOTNETCLI_Logging__MinimumLevel=Debug
export MCPDOTNETCLI_Logging__File__Path=/var/log/mcp-dotnet-cli.log
export MCPDOTNETCLI_ENVIRONMENT=Development
```

### MCP Client Configuration

```mermaid
flowchart TB
  subgraph clients["MCP Clients"]
    claude["Claude Desktop<br/>~/.config/claude/"]
    warp["Warp Terminal<br/>Settings > MCP"]
    lms["LM Studio<br/>Settings > MCP Servers"]
    vscode["VS Code<br/>.vscode/mcp.json"]
  end
  
  subgraph config["Server Config"]
    direction TB
    json["{<br/>  'type': 'stdio',<br/>  'command': 'dotnet',<br/>  'args': ['run', '--project', 'path']<br/>}"]
  end
  
  clients --> config
  config --> server[DotNetCliMcp.Server]
  
  style server fill:#28a745,color:#fff
```

**Example** (Claude Desktop / Warp / LM Studio):

```json
{
  "servers": {
    "dotnet-cli": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "C:\\path\\to\\DotNetCliMcp.Server"],
      "env": {
        "MCPDOTNETCLI_Logging__MinimumLevel": "Information"
      }
    }
  }
}
```

## Development

```bash
dotnet build
dotnet test    # 20 tests
dotnet format
```

## Stack

```mermaid
flowchart TB
  subgraph core["Core Stack"]
    net[.NET 9.0]
    sk[Semantic Kernel 1.65]
    msai[MS.Extensions.AI 9.9]
  end
  
  subgraph server["MCP Server"]
    mcp[MCP.NET SDK 0.4]
    hosting[Extensions.Hosting 10.0]
  end
  
  subgraph shared["Shared Infrastructure"]
    serilog[Serilog 4.3]
    config[Configuration 9.0]
  end
  
  subgraph testing["Testing"]
    xunit[xUnit 2.9]
    nsub[NSubstitute 5.3]
  end
  
  net --> sk
  net --> mcp
  sk --> msai
  mcp --> hosting
  net --> serilog
  net --> config
  
  style net fill:#512bd4,color:#fff
  style sk fill:#28a745,color:#fff
  style mcp fill:#ff6b6b,color:#fff
```

## License

MIT License - See [LICENSE](LICENSE) for details.
