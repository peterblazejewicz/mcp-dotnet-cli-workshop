# Migration Plan: Semantic Kernel -> Microsoft Agent Framework (C#/.NET)

Status: Draft (to be revisited)
Date: 2025-10-06
Scope: Local-first workshop app only (no cloud dependencies)

Objectives
- Replace Semantic Kernel (SK) with Microsoft Agent Framework (AF) while keeping the workshop experience intact.
- Preserve local LM Studio compatibility (OpenAI-compatible endpoint) and streaming console UX.
- Keep existing tool names and JSON shapes so prompts and workshop content remain valid.
- Minimize churn: small, reviewable changes without over-engineering.

Current State (summary)
- SK used for: Kernel + AddOpenAIChatCompletion, IChatCompletionService, ChatHistory, ToolCallBehavior.AutoInvokeKernelFunctions.
- Tools exposed via SK plugin class (DotNetCliPlugin) with [KernelFunction] attributes returning JSON strings.
- Central package mgmt includes Microsoft.SemanticKernel and Microsoft.Extensions.AI.
- Tests reference SK types (Kernel, ChatHistory) and SK-specific exceptions.

Target Architecture
- Use Microsoft.Extensions.AI IChatClient targeting LM Studio (OpenAI-compatible).
- Create an AIAgent from the ChatClient (Microsoft.Agents.AI) with the existing system prompt.
- Register DotNetCliService-backed functions as AF tools directly on the agent (keep names):
  - get_dotnet_info
  - list_installed_sdks
  - list_installed_runtimes
  - check_sdk_version
  - get_latest_sdk
  - get_effective_sdk
- Use agent-managed conversation (agent.GetNewThread()) and streaming run APIs to keep the spinner/streaming UX.

Step-by-Step Migration
1) Dependencies
- Remove Microsoft.SemanticKernel.
- Add Microsoft.Agents.AI.
- Keep Microsoft.Extensions.AI for provider abstractions.

2) Agent creation and chat loop (Program.cs)
- Replace Kernel.CreateBuilder + AddOpenAIChatCompletion with construction of an IChatClient for LM Studio using the existing HttpClient/timeouts.
- Create agent = chatClient.CreateAIAgent(instructions: existing system prompt, tools: registered tool delegates).
- Replace SK’s ChatHistory/IChatCompletionService calls with agent-managed thread and RunStreaming APIs.
- Preserve the spinner and token-by-token console output; keep the reasoning-tag scrubber.

3) Tools (migrate SK plugin to AF tools)
- Convert [KernelFunction]-decorated methods to plain delegates registered as AF tools.
- Keep tool names and JSON result shapes to avoid prompt and content drift.
- Continue using DotNetCliService for actual work.

4) Error handling and tests
- Remove SK-specific HttpOperationException expectations; assert provider exceptions (e.g., HttpRequestException) instead.
- Replace ChatHistory-only tests with minimal thread/message-based checks or direct agent invocation tests.

5) Build and validate locally
- dotnet build; dotnet test.
- Manual validation against LM Studio:
  - Verify streaming output and spinner behavior.
  - Ask sample questions to trigger each tool.
  - Confirm JSON fields and phrasing remain consistent with workshop prompts.

6) Documentation updates (minimal)
- README.md: replace SK references with “Microsoft Agent Framework” and brief architecture note.
- Keep this temporary file (MIGRATION-PLAN-SK.md) until migration completes, then remove or move to archive/.

SK -> AF Mapping (quick reference)
- SK Kernel -> AF AIAgent (created from IChatClient provider extensions).
- IChatCompletionService -> IChatClient.
- ChatHistory -> Agent-managed thread (agent.GetNewThread()).
- OpenAIPromptExecutionSettings/ToolCallBehavior -> Agent tool registration (no plugin wrapper required).
- [KernelFunction]-based plugin -> Direct tool registration (delegates).

Risks / Open Questions
- Provider wiring for IChatClient against LM Studio: confirm the exact provider package used for Microsoft.Extensions.AI to hit OpenAI-compatible endpoints.
- Hosted thread semantics: not applicable for LM Studio; ensure thread model remains in-memory.
- Exception surface: verify final exception types from the provider to update tests accurately.

References
- Agent Framework repo: https://github.com/microsoft/agent-framework
- Migration guide (C#): https://learn.microsoft.com/en-us/agent-framework/migration-guide/from-semantic-kernel/?pivots=programming-language-csharp
- Migration samples (.NET): https://github.com/microsoft/agent-framework/tree/main/dotnet/samples/SemanticKernelMigration
- Announcement blog: https://devblogs.microsoft.com/foundry/introducing-microsoft-agent-framework-the-open-source-engine-for-agentic-ai-apps/
