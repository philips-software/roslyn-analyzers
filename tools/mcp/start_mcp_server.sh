#!/usr/bin/env bash
set -euo pipefail

# Ensure Python + deps for the MCP stdio server
python3 -m pip install --upgrade pip >/dev/null
python3 -m pip install "fastmcp>=0.4"

# Launch the MCP stdio server (runs until Copilot session ends)
exec python3 ./tools/mcp/mcp_server.py
