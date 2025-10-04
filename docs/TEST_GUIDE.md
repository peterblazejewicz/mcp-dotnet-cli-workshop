# Testing Guide: Improved Tool Calling

## Quick Test

Run the application and ask the same question that previously caused errors:

```bash
dotnet run --project src/DotNetCliMcp.App
```

Then ask:
```
which version of .net sdk I have installed?
```

## What to Look For

### ✅ Expected Behavior (SUCCESS)

**In LM Studio Logs:**
```
[INFO] Model generated tool calls: [DotNetCli_list_installed_sdks()]
```
OR
```
[INFO] Model generated tool calls: [DotNetCli_get_latest_sdk()]
```

**In Application Output:**
```
Assistant: You have multiple .NET SDK versions installed: [clean list of versions]
```

**Key Success Indicators:**
1. ✅ Single tool call (no duplicates)
2. ✅ Correct function name (DotNetCli_list_installed_sdks or DotNetCli_get_latest_sdk)
3. ✅ No `tool_name()` invalid call
4. ✅ No `<think>` tags in response
5. ✅ No parsing errors
6. ✅ Clean, natural language response

### ❌ Previous Behavior (FAILURE)

**In LM Studio Logs:**
```
[INFO] Model generated tool calls: [tool_name(), DotNetCli-list_installed_sdks()]
[ERROR] Failed to parse tool call: Expected one of "{", "[END_TOOL_REQUEST]"
```

**Problems:**
- Invalid function name: `tool_name()`
- Duplicate calls
- Exposed `<think>` blocks
- Parsing errors
- Multiple retry attempts

## Additional Test Cases

### Test 1: Check Specific Version
```
Do I have .NET 9.0.302 installed?
```

**Expected:**
- Tool call: `DotNetCli_check_sdk_version` with `{"version": "9.0.302"}`
- Response: Clear yes/no answer

### Test 2: Get Latest SDK
```
What's my latest SDK?
```

**Expected:**
- Tool call: `DotNetCli_get_latest_sdk` with `{}`
- Response: Specific version number

### Test 3: General Environment Info
```
Tell me about my .NET environment
```

**Expected:**
- Tool call: `DotNetCli_get_dotnet_info` with `{}`
- Response: SDK version, runtime, OS, architecture

## Monitoring Points

### 1. LM Studio Console
Watch for:
- Number of tool call attempts (should be 1)
- Function name correctness
- No parsing errors
- Token usage (should be significantly lower)

### 2. Application Logs
Check `logs/mcp-dotnet-cli-workshop-YYYY-MM-DD.log` for:
```
[INFO] Plugin function list_installed_sdks invoked
```
Should appear once, not multiple times.

### 3. Response Quality
- No XML tags visible
- Natural language
- Concise but complete
- Includes relevant version numbers

## Performance Metrics

### Before (Baseline)
- Tool call attempts: 4+
- Tokens used: 2815+
- Errors: Multiple parsing errors
- Invalid calls: `tool_name()`, duplicates

### After (Expected)
- Tool call attempts: 1
- Tokens used: <500
- Errors: 0
- Invalid calls: 0

## Troubleshooting

### If Still Seeing Errors

1. **Verify LM Studio is running**
   ```bash
   curl http://127.0.0.1:1234/v1/models
   ```

2. **Check model supports function calling**
   - Prefer: Mistral 7B Instruct, Llama 3.1 Instruct, Qwen 2.5 Coder
   - Avoid: Pure reasoning models (DeepSeek R1)

3. **Verify build succeeded**
   ```bash
   dotnet build
   ```

4. **Check configuration in Program.cs**
   - Temperature: 0.1 ✅
   - MaxTokens: 1000 ✅
   - StopSequences: ["<think>", "</think>", ...] ✅

### If Response Contains <think> Tags

The response validation should strip these automatically. Check:
- Regex replacement is working
- Content is being trimmed

## Comparison Log

Document your results:

**Date:** YYYY-MM-DD  
**Model:** [Model name and size]  
**Question:** "which version of .net sdk I have installed?"

**Before Changes:**
- Tool calls: [count]
- Errors: [yes/no]
- Token usage: [approximate]

**After Changes:**
- Tool calls: [count]
- Errors: [yes/no]
- Token usage: [approximate]

**Improvement:** [percentage or qualitative description]

## Success Criteria

The implementation is successful if:

1. ✅ First tool call is correct (no retries)
2. ✅ No `tool_name()` or made-up function names
3. ✅ No duplicate tool calls
4. ✅ No parsing errors in logs
5. ✅ No `<think>` tags in user-visible output
6. ✅ Response is natural and accurate
7. ✅ Token usage reduced by >50%

## Next Steps After Testing

If successful:
- Document your results
- Consider testing with different models
- Try more complex queries

If issues persist:
- Review LM Studio logs in detail
- Check if model needs different prompt format
- Consider switching to a different model
- Report findings in GitHub issues
