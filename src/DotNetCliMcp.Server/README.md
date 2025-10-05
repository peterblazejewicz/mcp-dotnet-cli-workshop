# .NET CLI MCP Server

MCP server exposing .NET CLI capabilities via stdio transport. Part of the [mcp-dotnet-cli-workshop](https://github.com/peterblazejewicz/mcp-dotnet-cli-workshop).

## Tools

| Tool | CLI Command |
|------|-------------|
| `list_installed_sdks` | `dotnet --list-sdks` |
| `list_installed_runtimes` | `dotnet --list-runtimes` |
| `get_effective_sdk` | `dotnet --version` |
| `get_dotnet_info` | `dotnet --info` |
| `check_sdk_version` | (derived) |
| `get_latest_sdk` | (derived) |

## Usage

Configure in MCP clients (Claude Desktop, LM Studio, Warp, etc.):

```json
{
  "servers": {
    "dotnet-cli": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "path/to/DotNetCliMcp.Server"]
    }
  }
}
```

## Configuration

Use `MCPDOTNETCLI_` prefix for environment variables to avoid conflicts:

```bash
export MCPDOTNETCLI_Logging__MinimumLevel=Debug
export MCPDOTNETCLI_Logging__File__Path=/var/log/mcp-dotnet-cli.log
export MCPDOTNETCLI_ENVIRONMENT=Development
```

See main [README](../../README.md) for details.

## Testing

```bash
# Run locally
dotnet run --project src/DotNetCliMcp.Server

# Test with MCP Inspector
npm install -g @modelcontextprotocol/inspector
mcp-inspector dotnet run --project src/DotNetCliMcp.Server
```

## More

See [main README](../../README.md) for architecture, setup, and full documentation.
