#!/usr/bin/env python3
import sys, importlib
from pathlib import Path
from typing import Dict, Any
from fastmcp import FastMCP

mcp = FastMCP("roslyn-analyzers-dev")
BASE_DIR = Path(__file__).resolve().parents[2]

MODULE_NAME = "tools.mcp.ra_tools"

def _mod():
    # Always resolve the current module object
    mod = sys.modules.get(MODULE_NAME)
    if mod is None:
        mod = importlib.import_module(MODULE_NAME)
        mod.set_base_dir(str(BASE_DIR))
    return mod

@mcp.tool
def hot_reload() -> Dict[str, Any]:
    """Reload tool implementations from disk without restarting the MCP server."""
    importlib.invalidate_caches()
    mod = sys.modules.get(MODULE_NAME)
    if mod is None:
        mod = importlib.import_module(MODULE_NAME)
    else:
        mod = importlib.reload(mod)
    mod.set_base_dir(str(BASE_DIR))
    return {"status": "ok", "reloaded": MODULE_NAME, "base_dir": str(BASE_DIR)}

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

if __name__ == "__main__":
    mcp.run()
