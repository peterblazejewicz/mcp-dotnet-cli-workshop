# Function Naming Issue and Fix

## Problem Summary

When using the application with LM Studio, the model was generating incorrect function names and making duplicate tool calls, as observed in the LM Studio logs:

### Issues Observed

1. **Incorrect Function Names**
   - First attempt: `get_dotnet_info` ❌
   - Second attempt: `get_dotnet_info` ❌
   - Third attempt: `DotNetCli-get_dotnet_info` ❌ (hyphen instead of underscore)
   - Correct: `DotNetCli_get_dotnet_info` ✅

2. **Duplicate Tool Calls**
   ```json
   "tool_calls": [
     { "function": { "name": "get_dotnet_info" } },
     { "function": { "name": "get_dotnet_info" } }  // duplicate!
   ]
   ```

3. **Exposed Internal Reasoning**
   The model included `<think>` blocks in responses, which should be internal reasoning only.

4. **Error Messages in Logs**
   ```
   [ERROR] Failed to generate a tool call: 
   Failed to parse tool call: Expected one of "{", "[END_TOOL_REQUEST]"
   ```

## Root Cause

The issue stemmed from a **mismatch between function names** in the system prompt and the actual function names registered with Semantic Kernel.

### How Semantic Kernel Names Functions

When you register a plugin with:
```csharp
kernel.Plugins.AddFromObject(dotnetCliPlugin, "DotNetCli");
```

Semantic Kernel automatically **prefixes all function names** with the plugin name:

| Plugin Definition | Actual Function Name |
|-------------------|---------------------|
| `get_dotnet_info` | `DotNetCli_get_dotnet_info` |
| `list_installed_sdks` | `DotNetCli_list_installed_sdks` |
| `list_installed_runtimes` | `DotNetCli_list_installed_runtimes` |
| `check_sdk_version` | `DotNetCli_check_sdk_version` |
| `get_latest_sdk` | `DotNetCli_get_latest_sdk` |

The prefix uses:
- Plugin name: `DotNetCli`
- Separator: `_` (underscore)
- Function name: e.g., `get_dotnet_info`
- Result: `DotNetCli_get_dotnet_info`

### The Mismatch

**System Prompt Said:**
```
Available tools:
- get_dotnet_info: Get comprehensive .NET environment information
- list_installed_sdks: List all installed .NET SDKs
...
```

**But Actual Function Names Were:**
```
- DotNetCli_get_dotnet_info
- DotNetCli_list_installed_sdks
...
```

### Why This Caused Issues

1. **Model Confusion**: The model tried to call `get_dotnet_info` but that function didn't exist
2. **Self-Correction Loops**: The model tried to "fix" itself by:
   - First using `get_dotnet_info` ❌
   - Then trying `get_dotnet_info` again ❌
   - Then using `DotNetCli-get_dotnet_info` (with hyphen) ❌
   - Making duplicate calls in confusion ❌

3. **Wasted Tokens**: Each failed attempt generated extensive internal reasoning (`<think>` blocks)

4. **Eventually Worked**: After multiple attempts, the model eventually figured out the correct format, but only after:
   - 4 rounds of tool call attempts
   - 2815+ tokens of internal reasoning
   - Multiple errors in logs

## The Fix

### Changed System Message

**Before:**
```csharp
history.AddSystemMessage(@"...
Available tools:
- get_dotnet_info: Get comprehensive .NET environment information
- list_installed_sdks: List all installed .NET SDKs
...
");
```

**After:**
```csharp
history.AddSystemMessage(@"...
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
5. Do not include internal reasoning or <think> tags in your response
");
```

### Key Changes

1. ✅ **Correct Function Names**: Added `DotNetCli_` prefix with underscore
2. ✅ **Explicit Instruction**: Added "IMPORTANT" note about exact naming
3. ✅ **Parameter Hints**: Noted which functions require parameters
4. ✅ **Think Block Warning**: Added instruction to avoid exposing internal reasoning

## Expected Behavior After Fix

### First Tool Call Should Work
```
[INFO] Model generated tool calls: [DotNetCli_get_dotnet_info()]
```

### No Duplicate Calls
Single, correct function call instead of multiple attempts.

### No Error Messages
No "Failed to generate a tool call" errors.

### Cleaner Responses
No `<think>` blocks in the assistant's response visible to users.

## Testing

To verify the fix:

1. **Run the application:**
   ```bash
   dotnet run --project src/DotNetCliMcp.App
   ```

2. **Ask a simple question:**
   ```
   You: What is my .NET version?
   ```

3. **Check LM Studio logs** for:
   - ✅ Single tool call with correct name: `DotNetCli_get_dotnet_info`
   - ✅ No error messages
   - ✅ No duplicate tool calls
   - ✅ Clean response without `<think>` tags

## Technical Details

### Why Semantic Kernel Uses Prefixes

Plugin name prefixes serve several purposes:

1. **Namespace Collision Avoidance**: Multiple plugins can have functions with the same name
2. **Plugin Identification**: Makes it clear which plugin a function belongs to
3. **Discoverability**: Easier to find all functions in a plugin
4. **Organization**: Groups related functions together

### Alternative Approaches

If you don't want prefixes, you can:

1. **Use Empty Plugin Name:**
   ```csharp
   kernel.Plugins.AddFromObject(dotnetCliPlugin, "");
   ```
   This would result in: `get_dotnet_info` (no prefix)

2. **Import Functions Individually:**
   ```csharp
   kernel.ImportFunctions(dotnetCliPlugin);
   ```
   Functions would be imported without a namespace.

3. **Use Custom Names:**
   ```csharp
   kernel.Plugins.AddFromObject(dotnetCliPlugin, "cli");
   ```
   Result: `cli_get_dotnet_info`

### Current Approach

We chose to keep the `DotNetCli` prefix because:
- ✅ Clear function ownership
- ✅ Follows Semantic Kernel conventions
- ✅ Prevents name collisions if we add more plugins
- ✅ Makes logs more readable

## Related Files

- **`src/DotNetCliMcp.App/Program.cs`** (line 68-88) - System message with function names
- **`src/DotNetCliMcp.Core/Plugins/DotNetCliPlugin.cs`** - Function definitions

## References

- [Semantic Kernel Plugin Documentation](https://learn.microsoft.com/en-us/semantic-kernel/concepts/plugins/)
- [Function Naming Best Practices](https://learn.microsoft.com/en-us/semantic-kernel/concepts/plugins/using-the-kernelfunction-decorator)
- [LM Studio Function Calling](https://lmstudio.ai/docs/advanced/function-calling)

## Future Improvements

1. **Function Name Validation**: Add startup check to verify system message matches actual function names
2. **Dynamic System Prompt**: Generate system message from registered plugins automatically
3. **Function Calling Tests**: Add integration tests that verify function names are correct
4. **Documentation Generation**: Auto-generate function docs from plugin attributes
