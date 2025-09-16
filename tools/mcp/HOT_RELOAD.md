# MCP Hot Reload Feature

## Overview

The MCP (Model Context Protocol) server now supports hot reload functionality, allowing the CoPilot Coding Agent to test tool changes without restarting the MCP server process.

## Architecture

The hot reload feature uses a two-module approach:

1. **Stable Entrypoint** (`tools/mcp/mcp_server.py`): Contains thin wrapper functions that never change
2. **Reloadable Module** (`tools/mcp/ra_tools.py`): Contains the actual tool logic that can be hot-reloaded

## Usage

### For CoPilot Coding Agent

When developing or enhancing MCP tools:

1. **Edit the reloadable module**: Make changes to `tools/mcp/ra_tools.py`
2. **Call hot reload**: Use the `hot_reload` tool through MCP
3. **Test immediately**: Use the updated functionality without server restart

### Available Tools

- `hot_reload` - Reload tool implementations from disk
- `search_helpers` - Search for helper utilities
- `build_strict` - Build solution with warnings as errors
- `run_tests` - Run the test suite
- `run_dogfood` - Run dogfood analysis
- `fix_formatting` - Fix code formatting issues
- `analyze_coverage` - Analyze test coverage

## Example Workflow

```python
# 1. CoPilot edits ra_tools.py to add/modify a function
def new_feature():
    return {"status": "success", "message": "New feature added!"}

# 2. CoPilot calls hot_reload through MCP
result = hot_reload()
# Returns: {"status": "ok", "reloaded": "tools.mcp.ra_tools", "base_dir": "..."}

# 3. CoPilot can immediately use the new/modified functionality
# The changes are picked up without restarting the MCP server
```

## Technical Details

### Hot Reload Mechanism

The `hot_reload()` function:
1. Invalidates Python import caches
2. Reloads the `tools.mcp.ra_tools` module using `importlib.reload()`
3. Re-initializes the base directory setting
4. Returns success status

### Module Delegation

Each tool in `mcp_server.py` is a thin wrapper:

```python
@mcp.tool
def search_helpers() -> Dict[str, Any]:
    """Search for Helper.For methods and related helper utilities."""
    return _mod().search_helpers()
```

The `_mod()` function always returns the current (possibly reloaded) module instance.

## Testing

The implementation has been thoroughly tested:

- ✅ All existing tools work unchanged
- ✅ Hot reload picks up code changes correctly
- ✅ All 2076 tests pass
- ✅ Build succeeds with no errors
- ✅ Code formatting validation passes

## Benefits

1. **No Server Restart**: Changes take effect immediately
2. **Seamless Development**: CoPilot can iteratively develop and test tools
3. **Preserved Connection**: MCP handshake remains intact
4. **Backward Compatibility**: All existing tools work exactly as before