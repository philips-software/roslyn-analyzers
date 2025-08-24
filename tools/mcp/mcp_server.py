#!/usr/bin/env python3
"""
MCP Server for Roslyn Analyzers - Common Development Tasks Automation

This server provides endpoints to automate common development tasks such as:
- Dogfooding builds (applying analyzers to themselves)
- Strict building with warnings as errors
- File navigation and symbol search
- Test execution

© 2025 Koninklijke Philips N.V. See License.md in the project root for license information.
"""

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import List, Optional, Dict, Any
import os
import subprocess
import tempfile
import shutil
import json
import re
from pathlib import Path

app = FastAPI(
    title="Roslyn Analyzers MCP Server",
    description="Model Context Protocol server for common Roslyn Analyzers development tasks",
    version="1.0.0"
)

# Base directory for all operations - repository root
BASE_DIR = Path(__file__).parent.parent.parent.absolute()

def validate_path(user_path: str) -> Path:
    """
    Validate and sanitize user-provided paths to prevent path traversal attacks.
    
    Args:
        user_path: User-provided path string
        
    Returns:
        Path object that is guaranteed to be within BASE_DIR
        
    Raises:
        HTTPException: If path is invalid or attempts to traverse outside BASE_DIR
    """
    # Never allow absolute paths from user input
    if Path(user_path).is_absolute():
        raise HTTPException(status_code=400, detail="Absolute paths are not allowed")
    
    # Construct path relative to BASE_DIR
    try:
        # Remove any leading slashes and normalize
        clean_path = user_path.lstrip('/')
        full_path = (BASE_DIR / clean_path).resolve()
        
        # Ensure the resolved path is within BASE_DIR
        if not str(full_path).startswith(str(BASE_DIR.resolve())):
            raise HTTPException(status_code=400, detail="Path traversal outside repository is not allowed")
            
        return full_path
    except (OSError, ValueError) as e:
        raise HTTPException(status_code=400, detail=f"Invalid path: {str(e)}")

# Request/Response models (minimal for focused functionality)
# Removed redundant models - tests always run against Philips.CodeAnalysis.Test

# -------------------------
# Manifest
# -------------------------
@app.get("/manifest")
def manifest():
    """Return MCP server manifest describing available endpoints."""
    return {
        "name": "roslyn-analyzers-mcp", 
        "version": "1.0.0",
        "description": "MCP server for Roslyn Analyzers development tasks - focused on Helper.For methods",
        "endpoints": {
            "search_helpers": {
                "method": "POST",
                "params": [], 
                "returns": ["helpers", "helpers_count", "message"],
                "description": "Search for Helper.For methods and common utilities that developers often miss"
            },
            "build_strict": {
                "method": "POST",
                "params": [], 
                "returns": ["status", "errors", "logs"],
                "description": "Build solution with warnings as errors"
            },
            "run_tests": {
                "method": "POST",
                "params": [], 
                "returns": ["status", "results", "return_code"],
                "description": "Run tests against main test project (security-hardened)"
            },
            "run_dogfood": {
                "method": "POST",
                "params": [], 
                "returns": ["status", "violations"],
                "description": "Run dogfood process - build analyzers and apply to codebase"
            }
        }
    }

# -------------------------
# File navigation
# -------------------------
# Search Helper.For methods (core functionality for Copilot Coding Agent)
# -------------------------
@app.post("/search_helpers")
def search_helpers():
    """Search for Helper.For methods and related helper utilities that developers commonly miss."""
    try:
        helpers_found = []
        
        # Find all Helper classes and their For methods
        helper_files = [
            "Philips.CodeAnalysis.Common/Helper.cs",
            "Philips.CodeAnalysis.Common/AdditionalFilesHelper.cs",
            "Philips.CodeAnalysis.Common/AttributeHelper.cs",
            "Philips.CodeAnalysis.Common/CodeFixHelper.cs",
            "Philips.CodeAnalysis.Common/DocumentationHelper.cs",
            "Philips.CodeAnalysis.Common/ExtensionsHelper.cs",
            "Philips.CodeAnalysis.Common/LiteralHelper.cs",
            "Philips.CodeAnalysis.Common/ModifiersHelper.cs",
            "Philips.CodeAnalysis.Common/NamespacesHelper.cs",
            "Philips.CodeAnalysis.Common/TestHelper.cs",
            "Philips.CodeAnalysis.Common/TypesHelper.cs",
            "Philips.CodeAnalysis.Common/ConstructorSyntaxHelper.cs",
            "Philips.CodeAnalysis.Common/AssembliesHelper.cs"
        ]
        
        for helper_file in helper_files:
            helper_path = os.path.join(BASE_DIR, helper_file)
            if os.path.exists(helper_path):
                try:
                    with open(helper_path, 'r', encoding='utf-8') as f:
                        content = f.read()
                        lines = content.split('\n')
                        
                        # Find all public methods and properties
                        for i, line in enumerate(lines):
                            # Look for public methods, properties, and Helper.For references
                            if any(pattern in line for pattern in [
                                'public static', 'public class', 'public ', 
                                'ForAllowedSymbols', 'ForAdditionalFiles',
                                'Helper.For'
                            ]):
                                helpers_found.append({
                                    "file": helper_file,
                                    "line": i + 1,
                                    "content": line.strip(),
                                    "context": lines[max(0, i-1):i+3] if i > 0 else lines[i:i+3]
                                })
                
                except Exception as e:
                    continue
        
        return {
            "status": "success",
            "helpers_count": len(helpers_found),
            "helpers": helpers_found[:50],  # Limit results to avoid token overflow
            "message": f"Found {len(helpers_found)} helper methods/properties. Use these instead of creating new utility methods."
        }
    
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error searching helpers: {str(e)}")

# -------------------------
# Build strict
# -------------------------
@app.post("/build_strict")
def build_strict():
    """Build the solution with warnings treated as errors."""
    try:
        # Clean first
        clean_cmd = ["dotnet", "clean", "Philips.CodeAnalysis.sln"]
        subprocess.run(clean_cmd, cwd=BASE_DIR, capture_output=True, text=True, check=True)
        
        # Build with strict settings
        build_cmd = [
            "dotnet", "build", "Philips.CodeAnalysis.sln",
            "--configuration", "Release",
            "--no-incremental",
            "-warnaserror"
        ]
        
        result = subprocess.run(build_cmd, cwd=BASE_DIR, capture_output=True, text=True)
        
        # Parse errors from output
        output = result.stdout + result.stderr
        error_lines = []
        
        for line in output.splitlines():
            line_lower = line.lower()
            if "error" in line_lower and ("cs" in line_lower or "ph" in line_lower):
                error_lines.append(line.strip())
        
        return {
            "status": "success" if result.returncode == 0 else "failure",
            "errors": error_lines,
            "logs": output,
            "return_code": result.returncode
        }
    
    except subprocess.CalledProcessError as e:
        return {
            "status": "failure",
            "errors": [f"Build command failed: {e}"],
            "logs": e.stdout + e.stderr if hasattr(e, 'stdout') else str(e),
            "return_code": e.returncode
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error running strict build: {str(e)}")

# -------------------------
# Run tests (fixed target for security)
# -------------------------
@app.post("/run_tests")
def run_tests():
    """Run tests against the main test project."""
    try:
        test_cmd = [
            "dotnet", "test", "Philips.CodeAnalysis.Test/Philips.CodeAnalysis.Test.csproj",
            "--configuration", "Release",
            "--logger", "trx;LogFileName=test-results.trx",
            "--no-build"
        ]
        
        result = subprocess.run(test_cmd, cwd=BASE_DIR, capture_output=True, text=True)
        
        return {
            "status": "success" if result.returncode == 0 else "failure",
            "results": result.stdout + result.stderr,
            "return_code": result.returncode
        }
    
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error running tests: {str(e)}")

# -------------------------
# Run dogfood
# -------------------------
@app.post("/run_dogfood")
def run_dogfood():
    """Run the dogfood process - build analyzers then apply them to the codebase."""
    try:
        all_violations = []
        temp_props_path = BASE_DIR / "Directory.Build.props"
        backup_props_path = None
        
        # Backup existing Directory.Build.props if it exists
        if temp_props_path.exists():
            backup_props_path = temp_props_path.with_suffix('.props.backup')
            shutil.copy2(temp_props_path, backup_props_path)
        
        try:
            # Step 1: Build dogfood packages
            step1_props_content = """<Project>
  <PropertyGroup>
    <PackageId>$(MSBuildProjectName).Dogfood</PackageId>
  </PropertyGroup>
</Project>
"""
            with open(temp_props_path, "w", encoding="utf-8") as f:
                f.write(step1_props_content)
            
            # Build packages
            build_cmd = ["dotnet", "build", "--configuration", "Release"]
            build_result = subprocess.run(build_cmd, cwd=BASE_DIR, capture_output=True, text=True)
            
            if build_result.returncode != 0:
                return {
                    "status": "failure",
                    "violations": [{"step": "package_build", "error": "Failed to build dogfood packages"}],
                    "logs": build_result.stdout + build_result.stderr
                }
            
            # Step 2: Add local package source
            nuget_cmd = ["dotnet", "nuget", "add", "source", str(BASE_DIR / "Packages")]
            subprocess.run(nuget_cmd, cwd=BASE_DIR, capture_output=True, text=True)
            
            # Step 3: Create dogfood configuration
            step2_props_content = """<Project>
  <PropertyGroup>
    <FileVersion>1.0.0</FileVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Philips.CodeAnalysis.MaintainabilityAnalyzers.Dogfood" Version="1.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Philips.CodeAnalysis.DuplicateCodeAnalyzer.Dogfood" Version="1.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Philips.CodeAnalysis.SecurityAnalyzers.Dogfood" Version="1.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Philips.CodeAnalysis.MsTestAnalyzers.Dogfood" Version="1.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Philips.CodeAnalysis.MoqAnalyzers.Dogfood" Version="1.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
"""
            with open(temp_props_path, "w", encoding="utf-8") as f:
                f.write(step2_props_content)
            
            # Step 4: Build all projects with dogfood analyzers
            projects = [
                "./Philips.CodeAnalysis.Common/Philips.CodeAnalysis.Common.csproj",
                "./Philips.CodeAnalysis.DuplicateCodeAnalyzer/Philips.CodeAnalysis.DuplicateCodeAnalyzer.csproj",
                "./Philips.CodeAnalysis.MaintainabilityAnalyzers/Philips.CodeAnalysis.MaintainabilityAnalyzers.csproj",
                "./Philips.CodeAnalysis.MoqAnalyzers/Philips.CodeAnalysis.MoqAnalyzers.csproj",
                "./Philips.CodeAnalysis.MsTestAnalyzers/Philips.CodeAnalysis.MsTestAnalyzers.csproj",
                "./Philips.CodeAnalysis.SecurityAnalyzers/Philips.CodeAnalysis.SecurityAnalyzers.csproj",
                "./Philips.CodeAnalysis.Test/Philips.CodeAnalysis.Test.csproj",
                "./Philips.CodeAnalysis.Benchmark/Philips.CodeAnalysis.Benchmark.csproj",
                "./Philips.CodeAnalysis.AnalyzerPerformance/Philips.CodeAnalysis.AnalyzerPerformance.csproj",
            ]
            
            for project in projects:
                framework = "netstandard2.0" if not project.endswith(("Test.csproj", "Benchmark.csproj", "AnalyzerPerformance.csproj")) else "net8.0"
                
                dogfood_cmd = [
                    "dotnet", "build", project,
                    "--configuration", "Debug",
                    "--framework", framework,
                    "-consoleloggerparameters:NoSummary",
                    "-verbosity:quiet"
                ]
                
                dogfood_result = subprocess.run(dogfood_cmd, cwd=BASE_DIR, capture_output=True, text=True)
                
                # Parse violations (warnings/errors from analyzers)
                output_lines = dogfood_result.stdout.splitlines() + dogfood_result.stderr.splitlines()
                for line in output_lines:
                    line_lower = line.lower()
                    if any(keyword in line_lower for keyword in ["warning", "error"]) and ("ph" in line_lower or "cs" in line_lower):
                        all_violations.append({
                            "project": project,
                            "violation": line.strip()
                        })
            
            status = "success" if not all_violations else "failure"
            return {
                "status": status,
                "violations": all_violations,
                "violation_count": len(all_violations)
            }
        
        finally:
            # Cleanup: restore original props file
            if temp_props_path.exists():
                temp_props_path.unlink()
            
            if backup_props_path and backup_props_path.exists():
                shutil.move(backup_props_path, temp_props_path)
    
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error running dogfood process: {str(e)}")

# -------------------------
# Health check and info
# -------------------------
@app.get("/")
def root():
    """Root endpoint providing server information."""
    return {
        "name": "Roslyn Analyzers MCP Server",
        "version": "1.0.0",
        "status": "running",
        "endpoints": ["/manifest", "/list_files", "/get_file", "/search_symbols", "/build_strict", "/run_tests", "/run_dogfood"]
    }

@app.get("/health")
def health():
    """Health check endpoint."""
    return {"status": "healthy", "working_directory": str(BASE_DIR)}

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)