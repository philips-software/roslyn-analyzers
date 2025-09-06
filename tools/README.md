# Tools Directory

This directory contains development tools and utilities for the Roslyn Analyzers repository.

## MCP Server (`/mcp`)

The Model Context Protocol (MCP) server automates common development tasks such as:

- **File navigation and code analysis**
- **Build automation with strict warnings**
- **Test execution and reporting**
- **Dogfood process automation**

### Quick Start

```bash
# From repository root
./tools/mcp/start_mcp_server.sh
```

See [mcp/MCP_SERVER.md](./mcp/MCP_SERVER.md) for complete documentation.

### GitHub Integration

The MCP server can be integrated with GitHub Copilot using the configuration in [mcp/github-mcp-config.json](./mcp/github-mcp-config.json). This allows the Copilot Coding Agent to automatically use the MCP server for development tasks.

## Organization

Tools are organized into subdirectories by functionality:
- `mcp/` - Model Context Protocol server for development automation

This keeps the repository root clean while providing clear organization for development utilities.