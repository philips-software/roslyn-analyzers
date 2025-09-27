#!/usr/bin/env python3
import sys, importlib
import os
from pathlib import Path
from typing import Dict, Any
from fastmcp import FastMCP

mcp = FastMCP("roslyn-analyzers-dev")
BASE_DIR = Path(__file__).resolve().parents[2]

# Add the MCP directory to path for direct imports
MCP_DIR = Path(__file__).parent
if str(MCP_DIR) not in sys.path:
    sys.path.insert(0, str(MCP_DIR))

# Import ra_tools directly 
import ra_tools

def _mod():
    # Set the base directory and return the ra_tools module
    ra_tools.set_base_dir(str(BASE_DIR))
    return ra_tools

@mcp.tool
def hot_reload() -> Dict[str, Any]:
    """Reload tool implementations from disk without restarting the MCP server."""
    importlib.invalidate_caches()
    global ra_tools
    ra_tools = importlib.reload(ra_tools)
    ra_tools.set_base_dir(str(BASE_DIR))
    return {"status": "ok", "reloaded": "ra_tools", "base_dir": str(BASE_DIR)}

# Thin wrappers: delegate to the current module
@mcp.tool
def search_helpers() -> Dict[str, Any]:
    """Search for Helper.For methods and related helper utilities across Philips.CodeAnalysis.Common."""
    return _mod().search_helpers()

@mcp.tool
def build_strict() -> Dict[str, Any]:
    """dotnet build solution with warnings as errors."""
    return _mod().build_strict()

@mcp.tool
def run_tests() -> Dict[str, Any]:
    """Run tests against main test project."""
    return _mod().run_tests()

@mcp.tool
def run_dogfood() -> Dict[str, Any]:
    """Build analyzers, add dogfood packages, and build all projects to collect analyzer findings."""
    return _mod().run_dogfood()

@mcp.tool
def fix_formatting() -> Dict[str, Any]:
    """Fix code formatting issues using dotnet format. Automatically corrects IDE0055 violations including CRLF line endings and tab indentation."""
    return _mod().fix_formatting()

@mcp.tool
def analyze_coverage() -> Dict[str, Any]:
    """Collect .NET coverage and summarize uncovered lines (if dotnet-coverage is available, otherwise returns guidance)."""
    return _mod().analyze_coverage()

@mcp.tool
def next_diagnosticId() -> Dict[str, Any]:
    """Determine the next available DiagnosticId by examining main branch and all open PRs to avoid conflicts."""
    return _mod().next_diagnosticId()

if __name__ == "__main__":
    mcp.run()
