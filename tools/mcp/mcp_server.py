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

# Request/Response models
class ListFilesRequest(BaseModel):
    path: str = "."
    filters: Optional[str] = None

class GetFileRequest(BaseModel):
    path: str
    lines: Optional[str] = None

class SearchSymbolsRequest(BaseModel):
    query: str

class RunTestsRequest(BaseModel):
    target: Optional[str] = None

# -------------------------
# Manifest
# -------------------------
@app.get("/manifest")
def manifest():
    """Return MCP server manifest describing available endpoints."""
    return {
        "name": "roslyn-analyzers-mcp",
        "version": "1.0.0",
        "description": "MCP server for Roslyn Analyzers development tasks",
        "endpoints": {
            "list_files": {
                "method": "POST",
                "params": ["path", "filters"], 
                "returns": ["files"],
                "description": "List files in directory with optional filters"
            },
            "get_file": {
                "method": "POST",
                "params": ["path", "lines"], 
                "returns": ["content"],
                "description": "Get file content with optional line range"
            },
            "search_symbols": {
                "method": "POST",
                "params": ["query"], 
                "returns": ["matches"],
                "description": "Search for symbols (classes, methods, etc.) in codebase"
            },
            "build_strict": {
                "method": "POST",
                "params": [], 
                "returns": ["status", "errors", "logs"],
                "description": "Build solution with warnings as errors"
            },
            "run_tests": {
                "method": "POST",
                "params": ["target"], 
                "returns": ["results"],
                "description": "Run tests with optional target project"
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
@app.post("/list_files")
def list_files(request: ListFilesRequest):
    """List files in the specified directory with optional filters."""
    try:
        path = validate_path(request.path)
        
        if not path.exists():
            raise HTTPException(status_code=404, detail=f"Path not found: {path}")
        
        matches = []
        
        if path.is_file():
            matches.append(str(path.relative_to(BASE_DIR)))
        else:
            for root, dirs, files in os.walk(path):
                # Skip .git and other common directories
                dirs[:] = [d for d in dirs if not d.startswith('.') and d not in ['bin', 'obj', 'packages', 'node_modules']]
                
                for file in files:
                    file_path = Path(root) / file
                    
                    # Apply filters if specified
                    if request.filters:
                        if not any(file.endswith(ext.strip()) for ext in request.filters.split(',')):
                            continue
                    
                    # Skip binary and generated files
                    if file.endswith(('.dll', '.exe', '.pdb', '.nupkg', '.snupkg')):
                        continue
                        
                    relative_path = file_path.relative_to(BASE_DIR)
                    matches.append(str(relative_path))
        
        return {"files": sorted(matches)}
    
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error listing files: {str(e)}")

@app.post("/get_file")
def get_file(request: GetFileRequest):
    """Get content of specified file with optional line range."""
    try:
        file_path = validate_path(request.path)
        
        if not file_path.exists():
            raise HTTPException(status_code=404, detail=f"File not found: {file_path}")
            
        if not file_path.is_file():
            raise HTTPException(status_code=400, detail=f"Path is not a file: {file_path}")
        
        with open(file_path, "r", encoding="utf-8", errors="ignore") as f:
            content = f.readlines()
        
        if request.lines:
            try:
                if ":" in request.lines:
                    start, end = map(int, request.lines.split(":"))
                    content = content[start-1:end]  # Convert to 0-based indexing
                else:
                    line_num = int(request.lines)
                    content = [content[line_num-1]] if line_num <= len(content) else []
            except (ValueError, IndexError) as e:
                raise HTTPException(status_code=400, detail=f"Invalid line specification: {request.lines}")
        
        return {"content": "".join(content)}
    
    except HTTPException:
        raise
    except Exception as e:
        return {"content": f"Error reading file: {str(e)}"}

# -------------------------
# Symbol search
# -------------------------
@app.post("/search_symbols")
def search_symbols(request: SearchSymbolsRequest):
    """Search for symbols (classes, methods, interfaces, etc.) in the codebase."""
    try:
        matches = []
        
        # Search patterns for different symbol types (allow partial matches)
        query_pattern = re.escape(request.query).replace(r'\*', '.*')
        patterns = [
            rf"class\s+\w*{query_pattern}\w*\s*[<:\{{]",  # Classes
            rf"interface\s+\w*{query_pattern}\w*\s*[<:\{{]",  # Interfaces
            rf"enum\s+\w*{query_pattern}\w*\s*[:\{{]",  # Enums
            rf"struct\s+\w*{query_pattern}\w*\s*[<:\{{]",  # Structs
            rf"(public|private|protected|internal)\s+.*\s+\w*{query_pattern}\w*\s*\(",  # Methods
            rf"(public|private|protected|internal)\s+.*\s+\w*{query_pattern}\w*\s*[{{;]",  # Properties/Fields
            # Also search for any line containing the query
            rf".*{query_pattern}.*",  # General match
        ]
        
        # Search in C# files
        for root, dirs, files in os.walk(BASE_DIR):
            # Skip .git and build directories
            dirs[:] = [d for d in dirs if not d.startswith('.') and d not in ['bin', 'obj', 'packages']]
            
            for file in files:
                if file.endswith('.cs'):
                    file_path = Path(root) / file
                    try:
                        with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
                            content = f.read()
                            
                        for i, line in enumerate(content.splitlines(), 1):
                            for pattern in patterns:
                                if re.search(pattern, line, re.IGNORECASE):
                                    relative_path = file_path.relative_to(BASE_DIR)
                                    matches.append(f"{relative_path}:{i}: {line.strip()}")
                                    break
                    except Exception:
                        continue  # Skip files that can't be read
        
        return {"matches": matches}
    
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error searching symbols: {str(e)}")

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
# Run tests
# -------------------------
@app.post("/run_tests")
def run_tests(request: RunTestsRequest):
    """Run tests with optional target project specification."""
    try:
        test_target = request.target or "Philips.CodeAnalysis.Test/Philips.CodeAnalysis.Test.csproj"
        
        test_cmd = [
            "dotnet", "test", test_target,
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