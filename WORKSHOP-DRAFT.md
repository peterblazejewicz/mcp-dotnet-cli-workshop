# MCP + .NET CLI Workshop Draft

**Target Audience**: Developers/Engineers  
**Duration**: Full day (6-7 hours)  
**Prerequisites**: .NET 9 SDK, LM Studio or similar local LLM, basic understanding of CLI and APIs

---

## Workshop Goals

By the end of this workshop, participants will:
1. Understand how to programmatically execute and parse CLI output
2. Build a Semantic Kernel agent with auto-function calling
3. Optimize system prompts for reliable tool invocation
4. Create and test an MCP server exposing CLI tools to LLM clients
5. Integrate their MCP server with production LLM tools (Claude, Warp, LM Studio)

---

## Module 1: CLI Integration Fundamentals (90 min)

### Theory (20 min)
- Process execution in .NET: `Process.Start`, stdout/stderr streams
- Parsing structured vs unstructured CLI output
- Error handling and exit codes
- Why wrap CLI tools for LLM consumption?

### Exercise 1: Basic CLI Wrapper (40 min)
**Checkpoint: `3391aa4` (Initial commit)**

**Task**: Implement `DotNetCliService` to execute `dotnet --list-sdks` and `dotnet --list-runtimes`

Requirements:
- Create `IDotNetCliService` interface
- Implement async methods that spawn dotnet processes
- Parse output into structured `SdkInfo` and `RuntimeInfo` records
- Handle process errors and return meaningful exceptions
- Write xUnit tests with real dotnet CLI execution

**Key Files**:
- `src/DotNetCliMcp.Core/Services/DotNetCliService.cs`
- `src/DotNetCliMcp.Core/Contracts/SdkInfo.cs`
- `tests/DotNetCliMcp.Core.Tests/Services/DotNetCliServiceTests.cs`

### Exercise 1b: Advanced CLI Parsing (30 min)
**Checkpoint: `d2f90e2` (feat: logging bootstrap, core contracts)**

**Task**: Add `dotnet --info` parsing with regex patterns

Requirements:
- Parse multi-section output (Host, .NET SDKs, .NET runtimes)
- Use `[GeneratedRegex]` for performance
- Extract versions, architectures, and paths
- Create `DotNetInfo` contract with nested structures

---

## Module 2: Semantic Kernel Agent (120 min)

### Theory (25 min)
- Semantic Kernel architecture: Kernel, Plugins, Functions
- Function calling vs prompt engineering
- OpenAI-compatible API endpoints (LM Studio, Ollama, local inference)
- Auto-invocation patterns with `ToolCallBehavior`

### Exercise 2: Build SK Plugin (45 min)
**Checkpoint: `3391aa4` (Initial commit)**

**Task**: Create `DotNetCliPlugin` with `[KernelFunction]` methods

Requirements:
- Wrap `IDotNetCliService` methods as kernel functions
- Add `[Description]` attributes for LLM context
- Serialize results to JSON strings (LLM-friendly format)
- Handle exceptions and return error JSON
- Test plugin registration and invocation

**Key Files**:
- `src/DotNetCliMcp.Core/Plugins/DotNetCliPlugin.cs`
- `src/DotNetCliMcp.App/Program.cs`

### Exercise 3: Chat Loop with Auto-Invocation (50 min)
**Checkpoint: `3391aa4` (Initial commit)**

**Task**: Build interactive console app with SK chat completion

Requirements:
- Configure `IChatCompletionService` with LM Studio endpoint
- Add plugin to kernel with `kernel.Plugins.AddFromObject()`
- Implement chat loop with `ToolCallBehavior.AutoInvokeKernelFunctions`
- Accumulate conversation history
- Log function invocations and responses

**Test Queries**:
- "List all installed .NET SDKs"
- "Do I have .NET 8.0.413 installed?"
- "What's my latest SDK version?"

---

## Module 3: System Prompt Optimization (75 min)

### Theory (20 min)
- Why local LLMs fail at tool calling (compared to GPT-4/Claude)
- Common errors: invalid tool names, duplicate calls, reasoning leakage
- Temperature, max_tokens, and stop sequences for deterministic behavior
- Prompt engineering for tool selection

### Exercise 4: Enhanced System Prompts (35 min)
**Checkpoint: `c20b72d` (feat: improve LLM tool calling)**

**Task**: Implement robust system prompt with tool calling rules

Requirements:
- Add explicit tool naming conventions (prefix, underscore format)
- Include valid/invalid examples in prompt
- Configure execution settings: low temperature (0.1), reduced tokens
- Add stop sequences to prevent `<think>` tag leakage
- Strip reasoning tags from responses

**Key Metrics**:
- First-call success rate
- Zero invalid tool names
- No duplicate invocations

### Exercise 5: Error Recovery (20 min)

**Task**: Handle tool calling failures gracefully

Requirements:
- Detect parse errors and retry with corrective prompt
- Log failed invocations for analysis
- Provide fallback responses when tools unavailable

---

## Module 4: MCP Server Implementation (120 min)

### Theory (30 min)
- MCP Protocol overview: JSON-RPC over stdio
- stdio transport: stdin=requests, stdout=responses, stderr=logs
- MCP lifecycle: initialize → tools/list → tools/call
- Why MCP? Reusability across LLM clients (Claude, Warp, LM Studio, VS Code)

### Exercise 6: Build MCP Server (60 min)
**Checkpoint: `18ec737` (feat: add MCP Server with stdio transport)**

**Task**: Create `DotNetCliMcp.Server` console app with MCP.NET SDK

Requirements:
- Create `DotNetCliMcpTools` class implementing MCP tool interface
- Register 6 tools: `list_installed_sdks`, `list_installed_runtimes`, `get_effective_sdk`, `get_dotnet_info`, `check_sdk_version`, `get_latest_sdk`
- Configure Serilog to output ONLY to stderr (critical)
- Use `MCPDOTNETCLI_` prefix for environment variables
- Test with `mcp-inspector` CLI tool

**Key Files**:
- `src/DotNetCliMcp.Server/Program.cs`
- `src/DotNetCliMcp.Server/Tools/DotNetCliMcpTools.cs`
- `src/DotNetCliMcp.Server/appsettings.json`

### Exercise 7: MCP Client Integration (30 min)

**Task**: Configure MCP server in Claude Desktop / Warp / LM Studio

Requirements:
- Add server configuration to client's MCP settings
- Verify tool discovery via client UI
- Execute queries: "What .NET SDKs do I have?" via LLM client
- Monitor stderr logs for debugging
- Validate stdout contains only JSON-RPC messages

**Configuration Example** (Claude Desktop / Warp):
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

---

## Module 5: Testing and Production Readiness (45 min)

### Exercise 8: MCP Inspector (20 min)

**Task**: Test MCP server with `mcp-inspector` CLI

```bash
npx @modelcontextprotocol/inspector dotnet run --project src/DotNetCliMcp.Server
```

Requirements:
- Verify all 6 tools appear in inspector
- Execute each tool and validate responses
- Check stderr logs for errors
- Confirm stdout has no log contamination

### Exercise 9: Integration Testing (25 min)

**Task**: Write integration tests for MCP tools

Requirements:
- Test tool registration and discovery
- Validate JSON schema responses
- Test error scenarios (invalid SDK version, missing dotnet CLI)
- Verify logging configuration (stderr only)

---

## Bonus Topics (if time permits)

### Advanced: global.json Support
- Implement `get_effective_sdk` with `global.json` resolution
- Traverse directory tree to find nearest `global.json`
- Parse JSON and return pinned SDK version
- Compare with installed SDKs

### Advanced: Async Streaming
- Stream CLI output in real-time for long-running commands
- Implement cancellation tokens for user interruption
- Progress indicators for tool execution

### Production Considerations
- Configuration validation and error messages
- Health checks for MCP server availability
- Logging rotation and retention policies
- Deployment as Windows Service / systemd unit

---

## Workshop Delivery Notes

### Environment Setup (Pre-workshop)
Send participants setup checklist:
- .NET 9 SDK installed
- Git clone workshop repo
- LM Studio installed with model loaded (recommend: llama-3.2-3b-instruct)
- VS Code or Rider with C# Dev Kit
- MCP Inspector: `npm install -g @modelcontextprotocol/inspector`

### Pacing
- 45 min coding, 10 min break (repeat)
- Each module ends with working demo
- Commit checkpoints allow participants to catch up

### Assessment
- No formal tests, but participants should have:
  - Working SK agent that queries .NET environment
  - MCP server integrated with at least one LLM client
  - Understanding of when to use SK vs MCP

### Resources
- Commit history as learning path: `git log --oneline --reverse`
- Each checkpoint is a stable milestone
- README.md has architecture diagrams

---

## Post-Workshop Follow-up

### Challenge Projects
1. **Extend to other CLIs**: Wrap `kubectl`, `az`, `gh` with same pattern
2. **Multi-tool orchestration**: Agent that combines `dotnet`, `git`, and `npm` tools
3. **Custom MCP client**: Build minimal MCP client in Python/Node.js
4. **Production deployment**: Package as Docker container, deploy to dev team

### Learning Resources
- [MCP Specification](https://spec.modelcontextprotocol.io/)
- [Semantic Kernel Docs](https://learn.microsoft.com/en-us/semantic-kernel/)
- [Anthropic MCP Introduction](https://www.anthropic.com/news/model-context-protocol)
- [OpenAI Function Calling](https://platform.openai.com/docs/guides/function-calling)

### Office Hours
- Provide Teams/Slack/Discord channel for post-workshop questions
- Share participant projects and learnings
- Iterate on workshop content based on feedback

---

## Appendix: Commit Checkpoint Map

| Checkpoint | Commit | Description | Files |
|------------|--------|-------------|-------|
| M1-Ex1 | `3391aa4` | Basic CLI wrapper + SK plugin | DotNetCliService, Plugin, Tests |
| M1-Ex1b | `d2f90e2` | Advanced parsing + contracts | DotNetInfo, Regex patterns |
| M2-Ex2 | `3391aa4` | SK plugin with functions | DotNetCliPlugin |
| M2-Ex3 | `3391aa4` | Chat loop with auto-invoke | Program.cs (App) |
| M3-Ex4 | `c20b72d` | Enhanced system prompts | Program.cs (improved) |
| M4-Ex6 | `18ec737` | MCP server implementation | Server project |
| M4-Ex7 | `18ec737` | Client integration | Server README |

Use `git checkout <commit>` to jump to any checkpoint during workshop.

---

## License

This workshop and all associated code are released under the MIT License - See [LICENSE](LICENSE) for details.
