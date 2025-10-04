# Tool Calling Prompt Improvements

## Problem Analysis

The error you encountered is **avoidable** and stems from several issues in how the system prompt instructs the model on tool calling format:

```
[ERROR] Failed to parse tool call: Expected one of "{", "[END_TOOL_REQUEST]", 
but got "or anything, just " at index 15.
```

### Root Causes

1. **Ambiguous Tool Call Format**: The current system prompt doesn't explicitly specify the **exact format** for tool calls
2. **Missing Format Examples**: No concrete examples of valid tool call requests/responses
3. **Reasoning Leakage**: Model includes `<think>` blocks in responses despite instructions not to
4. **Invalid Tool Name**: Model generated `tool_name()` - a completely invalid function
5. **Inconsistent Parsing Expectations**: LM Studio expects a specific format that the model doesn't know about

## Current System Prompt Analysis

**Location**: `src/DotNetCliMcp.App/Program.cs` (lines 93-111)

```csharp
history.AddSystemMessage(@"You are a helpful assistant that can answer questions about .NET SDK and runtime installations.
You have access to tools that can query the local .NET environment.
Use these tools to provide accurate, up-to-date information about installed .NET versions.

Available tools:
- DotNetCli_get_dotnet_info: Get comprehensive .NET environment information
- DotNetCli_list_installed_sdks: List all installed .NET SDKs
- DotNetCli_list_installed_runtimes: List all installed .NET runtimes
- DotNetCli_check_sdk_version: Check if a specific SDK version is installed (requires version parameter)
- DotNetCli_get_latest_sdk: Get the latest installed SDK version

IMPORTANT: Use the exact function names above with the DotNetCli_ prefix and underscore separator.

When answering questions:
1. Use the appropriate tool to gather information
2. Provide clear, concise answers based on the tool results
3. Include relevant version numbers and paths when applicable
4. If a user asks about compatibility or requirements, explain clearly
5. Do not include internal reasoning or <think> tags in your response");
```

### What's Missing

1. **No tool call format specification** - What structure should tool calls have?
2. **No parameter format examples** - How should function arguments be formatted?
3. **No complete workflow example** - What does a successful interaction look like?
4. **No error recovery guidance** - What if a tool call fails?
5. **Vague reasoning suppression** - "Do not include <think> tags" isn't enforced by format

## Recommended Improvements

### Option 1: Enhanced System Prompt with Format Specification

```csharp
history.AddSystemMessage(@"You are a helpful assistant that answers questions about .NET SDK and runtime installations.
You have access to tools that query the local .NET environment.

## Available Tools

You MUST use these exact function names with proper JSON formatting:

1. **DotNetCli_get_dotnet_info**
   - Description: Get comprehensive .NET environment information
   - Parameters: none
   - Returns: JSON with sdk_version, runtime_version, os_version, architecture

2. **DotNetCli_list_installed_sdks**
   - Description: List all installed .NET SDKs
   - Parameters: none
   - Returns: JSON with count and list of {version, path}

3. **DotNetCli_list_installed_runtimes**
   - Description: List all installed .NET runtimes
   - Parameters: none
   - Returns: JSON with count and list of {name, version, path}

4. **DotNetCli_check_sdk_version**
   - Description: Check if a specific SDK version is installed
   - Parameters: {""version"": ""string""} (e.g., {""version"": ""9.0.302""})
   - Returns: JSON with is_installed, closest_matches

5. **DotNetCli_get_latest_sdk**
   - Description: Get the latest installed SDK version
   - Parameters: none
   - Returns: JSON with latest_version, path, total_sdks_installed

## Tool Calling Rules

1. **ALWAYS use the exact function names above** with DotNetCli_ prefix
2. **For functions without parameters**, call with empty object: {}
3. **For functions with parameters**, use proper JSON format
4. **Call ONE tool at a time** - never make duplicate calls
5. **Wait for tool results** before responding to the user
6. **DO NOT** include <think>, <reasoning>, or any XML tags in your response
7. **DO NOT** make up function names like 'tool_name' or similar

## Response Format

When you receive a user question:
1. Determine which tool to call
2. Make the tool call with proper format
3. Wait for the result
4. Formulate a natural language answer based ONLY on the tool result
5. Do not expose internal reasoning to the user

## Examples

### Example 1: Simple query
User: ""What version of .NET SDK do I have installed?""
Tool Call: DotNetCli_get_dotnet_info with arguments {}
Wait for result, then respond naturally.

### Example 2: Checking specific version
User: ""Do I have .NET 8.0.202 SDK?""
Tool Call: DotNetCli_check_sdk_version with arguments {""version"": ""8.0.202""}
Wait for result, then respond naturally.

### Example 3: Listing all SDKs
User: ""List all my .NET SDKs""
Tool Call: DotNetCli_list_installed_sdks with arguments {}
Wait for result, then respond naturally.

## Important Notes

- Your responses should be conversational and helpful
- Include version numbers and paths when relevant
- Explain compatibility issues clearly when asked
- If a tool returns an error, explain it to the user clearly
- Keep responses concise but complete");
```

### Option 2: JSON Schema Specification

Add explicit JSON schema for tool calling:

```csharp
var toolSchema = @"
## Tool Call Format

All tool calls must use this exact JSON structure:

{
  ""tool_calls"": [
    {
      ""type"": ""function"",
      ""function"": {
        ""name"": ""<exact_function_name>"",
        ""arguments"": ""<json_string_of_parameters>""
      }
    }
  ]
}

### Valid Examples:

No parameters:
{
  ""function"": {
    ""name"": ""DotNetCli_get_dotnet_info"",
    ""arguments"": ""{}""
  }
}

With parameters:
{
  ""function"": {
    ""name"": ""DotNetCli_check_sdk_version"",
    ""arguments"": ""{\""version\"": \""9.0.302\""}""
  }
}

### Invalid Examples (DO NOT DO THIS):

❌ Wrong name: {""name"": ""get_dotnet_info""}
❌ Missing prefix: {""name"": ""list_installed_sdks""}
❌ Invalid name: {""name"": ""tool_name""}
❌ Duplicate calls: Calling the same function twice
❌ Wrong separator: {""name"": ""DotNetCli-get_dotnet_info""}
";

history.AddSystemMessage(baseSystemPrompt + toolSchema);
```

### Option 3: Constrained Output Format

Force the model into a specific response pattern:

```csharp
history.AddSystemMessage(@"You are a .NET SDK assistant with strict output rules.

## Response Protocol

For EVERY user query, follow this EXACT sequence:

Step 1: TOOL SELECTION
Select ONE tool from: DotNetCli_get_dotnet_info, DotNetCli_list_installed_sdks, 
DotNetCli_list_installed_runtimes, DotNetCli_check_sdk_version, DotNetCli_get_latest_sdk

Step 2: TOOL INVOCATION (use function calling mechanism)
Call the selected tool with proper arguments format

Step 3: RESULT PROCESSING (after receiving tool result)
Formulate answer based ONLY on the tool result

## Strict Rules

1. ONE tool call per user query
2. NO duplicate tool calls
3. NO made-up function names
4. NO XML tags (<think>, <reasoning>, etc.) in responses
5. NO explanations before tool results arrive
6. Use EXACT function names: DotNetCli_<function_name>
7. Empty parameters = {}
8. Parameters with values = proper JSON string

## Tool Descriptions

[... rest of tool descriptions ...]

## Critical Format Rules

✅ CORRECT: DotNetCli_get_dotnet_info
✅ CORRECT: DotNetCli_check_sdk_version with {""version"": ""9.0.302""}
❌ WRONG: get_dotnet_info (missing prefix)
❌ WRONG: DotNetCli-get_dotnet_info (wrong separator)
❌ WRONG: tool_name (invalid function)
❌ WRONG: Calling same function twice

Remember: WAIT for tool results, then respond naturally.");
```

## Testing the Improvements

### Test Case 1: Simple Query
```
User: "which version of .net sdk I have installed?"
Expected Tool Call: DotNetCli_list_installed_sdks OR DotNetCli_get_latest_sdk
Should NOT: Call tool_name, make duplicate calls, expose <think> tags
```

### Test Case 2: Specific Version Check
```
User: "Do I have .NET 9.0.302?"
Expected Tool Call: DotNetCli_check_sdk_version {"version": "9.0.302"}
Should NOT: Call without parameters, call wrong function
```

### Test Case 3: Comprehensive Info
```
User: "Tell me about my .NET environment"
Expected Tool Call: DotNetCli_get_dotnet_info
Should NOT: Call multiple tools simultaneously
```

## Implementation Strategy

### Phase 1: Minimal Fix (Quick)
1. Add explicit format examples to system prompt
2. Add "DO NOT call tool_name or make up function names" rule
3. Emphasize ONE tool call per query

### Phase 2: Format Specification (Moderate)
1. Add JSON schema for tool calls
2. Provide valid/invalid examples
3. Add parameter format specifications

### Phase 3: Dynamic Schema Generation (Long-term)
1. Auto-generate system prompt from plugin definitions
2. Validate system prompt matches registered functions at startup
3. Add runtime validation of tool call formats

## Code Changes Required

### File: `src/DotNetCliMcp.App/Program.cs`

Replace lines 93-111 with improved system prompt:

```csharp
// Generate tool descriptions dynamically from registered plugins
var toolDescriptions = kernel.Plugins
    .SelectMany(plugin => plugin.Select(func => 
        $"- {func.Name}: {func.Description}" + 
        (func.Metadata.Parameters.Any() 
            ? $" (Parameters: {string.Join(", ", func.Metadata.Parameters.Select(p => $"{p.Name}: {p.ParameterType.Name}"))})"
            : " (No parameters)")))
    .ToList();

var systemPrompt = $@"You are a .NET SDK assistant. You answer questions using these tools:

## Available Tools
{string.Join("\n", toolDescriptions)}

## Critical Rules
1. Use EXACT function names as listed above
2. For functions without parameters, use {{}}
3. For functions with parameters, use proper JSON format
4. Make ONE tool call per question
5. WAIT for tool result before responding
6. NO <think> tags or internal reasoning in responses
7. DO NOT make up function names

## Examples
User: ""What .NET SDK version?"" → Call DotNetCli_get_dotnet_info with {{}}
User: ""Do I have .NET 9.0.302?"" → Call DotNetCli_check_sdk_version with {{""version"": ""9.0.302""}}

Respond naturally after receiving tool results.";

history.AddSystemMessage(systemPrompt);
```

### Add Startup Validation

Add after line 86:

```csharp
// Validate tool names are correctly referenced in system prompt
var registeredFunctions = kernel.Plugins.SelectMany(p => p.Select(f => f.Name)).ToHashSet();
logger.LogInformation("Registered functions: {Functions}", string.Join(", ", registeredFunctions));

// Warn if system prompt might have mismatches
if (!systemPrompt.Contains("DotNetCli_get_dotnet_info"))
{
    logger.LogWarning("System prompt may not correctly reference registered function names!");
}
```

## Expected Improvements

### Before
- ❌ 4 tool call attempts
- ❌ 2815+ tokens wasted on reasoning
- ❌ `tool_name()` invalid call
- ❌ Duplicate calls
- ❌ Exposed `<think>` tags
- ❌ Parse errors

### After
- ✅ 1 correct tool call on first attempt
- ✅ Minimal token usage
- ✅ Correct function name
- ✅ No duplicates
- ✅ Clean responses
- ✅ No parse errors

## Additional Recommendations

### 1. Model Selection
- **Prefer models explicitly trained for tool calling**
- DeepSeek R1 (reasoning model) may not be ideal for constrained tool calling
- Consider: Mistral 7B Instruct, Llama 3.1 Instruct, Qwen 2.5 Coder

### 2. LM Studio Configuration
- Lower temperature (0.1-0.3) for tool calling
- Enable "strict function calling" if available
- Reduce max tokens for tool call phase

### 3. Semantic Kernel Settings

```csharp
var settings = new OpenAIPromptExecutionSettings
{
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
    Temperature = 0.1,  // Lower for more deterministic tool selection
    MaxTokens = 500,     // Reduce for tool calling phase
    StopSequences = ["<think>", "</think>"]  // Prevent reasoning tags
};
```

### 4. Add Response Validation

```csharp
// After receiving response, validate it doesn't contain unwanted patterns
if (response?.Content != null)
{
    var content = response.Content;
    
    // Strip out any reasoning tags that leaked through
    content = System.Text.RegularExpressions.Regex.Replace(
        content, 
        @"<think>.*?</think>", 
        "", 
        System.Text.RegularExpressions.RegexOptions.Singleline
    );
    
    history.AddAssistantMessage(content);
    System.Console.WriteLine($"\x1b[35m\nAssistant: {content}\x1b[0m");
}
```

## Monitoring and Debugging

Add detailed logging for tool calls:

```csharp
// Before GetChatMessageContentAsync
logger.LogDebug("Chat history count: {Count}", history.Count);
logger.LogDebug("Last message: {Message}", history.LastOrDefault()?.Content);

// After receiving response
if (response?.Metadata?.ContainsKey("ToolCalls") == true)
{
    logger.LogInformation("Tool calls made: {ToolCalls}", 
        response.Metadata["ToolCalls"]);
}
```

## Summary

The error is **100% avoidable** with proper prompt engineering:

1. **Explicit format specification** - Tell the model exactly how to format calls
2. **Concrete examples** - Show valid and invalid patterns
3. **Strict rules** - Emphasize one call, exact names, no duplicates
4. **Model selection** - Use instruction-tuned models for tool calling
5. **Runtime validation** - Catch and strip unwanted patterns

The most impactful changes:
1. ✅ Add explicit "DO NOT call tool_name" rule
2. ✅ Add format examples (valid/invalid)
3. ✅ Lower temperature to 0.1-0.3
4. ✅ Add response validation to strip <think> tags

These changes should eliminate the parsing errors and improve first-call success rate significantly.
