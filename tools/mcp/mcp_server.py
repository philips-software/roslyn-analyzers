#!/usr/bin/env python3
import os
import re
import subprocess
import shutil
from pathlib import Path
from typing import List, Dict, Any
from fastmcp import FastMCP

mcp = FastMCP("roslyn-analyzers-dev")
BASE_DIR = Path(__file__).resolve().parents[2]  # repo root (../../)
DEFAULT_TIMEOUT = 900  # 15 minutes for long builds/tests

def _run(cmd: list[str], *, timeout: int = DEFAULT_TIMEOUT) -> tuple[int, str]:
    if (
        not isinstance(cmd, list)
        or not cmd
        or not all(isinstance(x, str) for x in cmd)
        or any(any(c in x for c in ['&', '|', '$', '`', '>', '<']) for x in cmd)
    ):
        raise ValueError("Unsafe or invalid command list passed to _run")
    p = subprocess.run(
        cmd,
        cwd=BASE_DIR,
        capture_output=True,
        text=True,
        shell=False,
        timeout=timeout
    )
    return p.returncode, (p.stdout or "") + (p.stderr or "")

def _coverage_exe() -> str:
    """
    Return an absolute path to dotnet-coverage if installed globally; otherwise
    fall back to 'dotnet-coverage' and let the call fail gracefully.
    """
    home = Path.home()
    candidate = home / ".dotnet" / "tools" / ("dotnet-coverage.exe" if os.name == "nt" else "dotnet-coverage")
    return str(candidate) if candidate.exists() else "dotnet-coverage"
    
@mcp.tool
def search_helpers() -> Dict[str, Any]:
    """Search for Helper.For methods and related helper utilities across Philips.CodeAnalysis.Common."""
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
        "Philips.CodeAnalysis.Common/AssembliesHelper.cs",
    ]
    hits: List[Dict[str, Any]] = []
    for rel in helper_files:
        p = BASE_DIR / rel
        if not p.exists():
            continue
        content = p.read_text(encoding="utf-8", errors="replace").splitlines()
        for i, line in enumerate(content):
            if any(s in line for s in ("public static", "Helper.For", "ForAllowedSymbols", "ForAdditionalFiles")):
                ctx = content[max(0, i-1): i+3]
                hits.append({"file": rel, "line": i+1, "content": line.strip(), "context": ctx})
    return {"status": "success", "helpers_count": len(hits), "helpers": hits[:50]}

@mcp.tool
def build_strict() -> Dict[str, Any]:
    """dotnet build solution with warnings as errors."""
    _run(["dotnet", "clean", "Philips.CodeAnalysis.sln"])
    rc, out = _run([
        "dotnet", "build", "Philips.CodeAnalysis.sln",
        "--configuration", "Release", "--no-incremental", "-warnaserror"
    ])
    errors = [ln.strip() for ln in out.splitlines() if "error" in ln.lower() and (" cs" in ln.lower() or " ph" in ln.lower() or " netsdk" in ln.lower() or " mstest" in ln.lower())]
    return {"status": "success" if rc == 0 else "failure", "return_code": rc, "errors": errors, "logs": out[-8000:]}

@mcp.tool
def run_tests() -> Dict[str, Any]:
    """Run tests against main test project."""
    
    # Build base command without --no-restore to allow restore + build from clean state
    cmd = [
        "dotnet", "test", "Philips.CodeAnalysis.Test/Philips.CodeAnalysis.Test.csproj",
        "--configuration", "Release",
        "--logger", "trx;LogFileName=test-results.trx"
    ]
    
    # Check if build artifacts exist - if so, skip both restore and build for speed
    test_bin = BASE_DIR / "Philips.CodeAnalysis.Test" / "bin" / "Release"
    if test_bin.exists():
        cmd.extend(["--no-restore", "--no-build"])

    # Use timeout that works within MCP framework limits
    # MCP framework appears to timeout around 60s, so use 45s for safety
    # Tests only take ~40s, but from clean state needs build first
    timeout = 45

    rc, out = _run(cmd, timeout=timeout)
    
    # Parse test results from output
    test_results = {"passed": 0, "failed": 0, "skipped": 0, "total": 0, "duration": ""}
    test_summary = ""
    
    lines = out.splitlines()
    for line in lines:
        # Look for the test results summary line like:
        # "Passed!  - Failed:     0, Passed:  2015, Skipped:     0, Total:  2015, Duration: 40 s"
        # "Failed!  - Failed:     5, Passed:  2010, Skipped:     0, Total:  2015, Duration: 40 s"
        if ("Passed!" in line or "Failed!" in line) and "Total:" in line:
            test_summary = line.strip()
            # Parse the numeric values
            failed_match = re.search(r'Failed:\s*(\d+)', line)
            passed_match = re.search(r'Passed:\s*(\d+)', line)
            skipped_match = re.search(r'Skipped:\s*(\d+)', line)
            total_match = re.search(r'Total:\s*(\d+)', line)
            duration_match = re.search(r'Duration:\s*(\d+\s*[a-zA-Z]+)', line)
            
            if failed_match:
                test_results["failed"] = int(failed_match.group(1))
            if passed_match:
                test_results["passed"] = int(passed_match.group(1))
            if skipped_match:
                test_results["skipped"] = int(skipped_match.group(1))
            if total_match:
                test_results["total"] = int(total_match.group(1))
            if duration_match:
                test_results["duration"] = duration_match.group(1).strip()
            break
    
    # Filter out noise - keep only test-related output
    filtered_lines = []
    for line in lines:
        # Skip .NET welcome messages and similar noise
        if any(noise in line for noise in [
            "Welcome to .NET", "SDK Version:", "development certificate",
            "Write your first app:", "Find out what's new:", "Explore documentation:",
            "Report issues and find source", "Use 'dotnet --help'"
        ]):
            continue
        # Keep test execution related lines
        if any(test_keyword in line for test_keyword in [
            "Test run for", "Test Execution Command Line Tool", "Starting test execution",
            "test files matched", "Results File:", "Passed!", "Failed!", "Warning:", "Error:"
        ]) or line.strip() == "":
            filtered_lines.append(line)
    
    filtered_output = "\n".join(filtered_lines).strip()
    
    return {
        "status": "success" if rc == 0 else "failure",
        "return_code": rc,
        "test_results": test_results,
        "summary": test_summary if test_summary else f"{test_results['passed']} passed, {test_results['failed']} failed, {test_results['skipped']} skipped, {test_results['total']} total",
        "logs": filtered_output[-4000:] if filtered_output else ""
    }

@mcp.tool
def run_dogfood() -> Dict[str, Any]:
    """Build analyzers, add dogfood packages, and build all projects to collect analyzer findings."""
    props = BASE_DIR / "Directory.Build.props"
    backup = None
    violations: List[Dict[str, str]] = []
    try:
        # Step 1: Build the Dogfood packages
        if props.exists():
            backup = props.with_suffix(".props.backup")
            shutil.copy2(props, backup)
        
        # Create Directory.Build.props for dogfood package creation
        props.write_text("""<Project>
  <PropertyGroup>
    <PackageId>$(MSBuildProjectName).Dogfood</PackageId>
  </PropertyGroup>
</Project>
""", encoding="utf-8")
        
        # Build to create .Dogfood packages
        rc, out = _run(["dotnet", "build", "--configuration", "Release"])
        if rc != 0:
            return {"status": "failure", "violation_count": 0, "violations": [], "error": "Failed to build dogfood packages", "build_output": out[-2000:]}
        
        # Step 2: Prepare to eat Dogfood - add local package source and configure package references
        packages_dir = BASE_DIR / "Packages"
        rc, _ = _run(["dotnet", "nuget", "add", "source", str(packages_dir)])
        
        # Remove the dogfood build props and create the consumption props
        props.unlink()
        props.write_text("""<Project>
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
""", encoding="utf-8")

        # Step 3: Eat the Dogfood - build all projects with analyzers applied
        # First clean to ensure compilation happens
        _run(["dotnet", "clean"])
        
        # Build all projects at once to detect violations more efficiently
        rc, out = _run(["dotnet", "build", "--configuration", "Debug", 
                       "-consoleloggerparameters:NoSummary", "-verbosity:normal"])
        
        # Parse output for violations 
        for ln in (out or "").splitlines():
            low = ln.lower()
            # Look for warnings and errors with analyzer codes (CS or PH)
            if ("warning" in low or "error" in low) and (" cs" in low or " ph" in low):
                # Extract project from the line format if possible
                project = "unknown"
                if "[" in ln and "]" in ln:
                    bracket_content = ln[ln.rfind("["):ln.rfind("]")+1]
                    if "/" in bracket_content:
                        potential_project = bracket_content.split("/")[-1].replace("]", "").split("::")[0]
                        if potential_project.endswith(".csproj"):
                            project = potential_project
                violations.append({"project": project, "violation": ln.strip()})
        return {"status": "success" if not violations else "failure", "violation_count": len(violations), "violations": violations}
    finally:
        if props.exists(): props.unlink()
        if backup and backup.exists(): shutil.move(backup, props)

@mcp.tool
def fix_formatting() -> Dict[str, Any]:
    """Fix code formatting issues using dotnet format. Automatically corrects IDE0055 violations including CRLF line endings and tab indentation."""
    rc, out = _run([
        "dotnet", "format", "style", "Philips.CodeAnalysis.sln",
        "--verbosity", "normal"
    ])
    
    # Count formatted files from output
    formatted_count = 0
    if "Formatted" in out:
        lines = out.splitlines()
        for line in lines:
            if line.strip().startswith("Formatted") and "files." in line:
                # Extract number like "Formatted 15 of 602 files."
                parts = line.strip().split()
                if len(parts) >= 2 and parts[1].isdigit():
                    formatted_count = int(parts[1])
                    break
    
    return {
        "status": "success" if rc == 0 else "failure",
        "return_code": rc,
        "formatted_files": formatted_count,
        "message": f"Fixed formatting for {formatted_count} files" if formatted_count > 0 else "All files already properly formatted",
        "logs": out[-4000:] if out else ""
    }

@mcp.tool
def analyze_coverage() -> Dict[str, Any]:
    """Collect .NET coverage and summarize uncovered lines (if dotnet-coverage is available, otherwise returns guidance)."""
    # Try to install the coverage tool; ignore failures to keep tool resilient
    _run(["dotnet", "tool", "install", "--global", "dotnet-coverage", "--version", "17.9.6"], timeout=300)

    coverage_bin = _coverage_exe()
    rc, out = _run([
        coverage_bin, "collect",
        "dotnet", "test", "Philips.CodeAnalysis.Test/Philips.CodeAnalysis.Test.csproj",
        "--configuration", "Release", "--no-build",
        "--logger", "trx;LogFileName=coverage-test-results.trx",
        "--output-format", "xml", "--output", "coverage.xml"
    ])

    analysis = {"status": "success" if rc == 0 else "failure", "overall_coverage": 0.0, "uncovered_lines": [], "suggestions": []}
    xml_path = BASE_DIR / "coverage.xml"
    if xml_path.exists():
        try:
            import xml.etree.ElementTree as ET
            root = ET.parse(xml_path).getroot()
            for module in root.findall(".//module"):
                name = module.get("name","")
                if any(p in name for p in [
                    "Philips.CodeAnalysis.Common","Philips.CodeAnalysis.MaintainabilityAnalyzers",
                    "Philips.CodeAnalysis.DuplicateCodeAnalyzer","Philips.CodeAnalysis.SecurityAnalyzers",
                    "Philips.CodeAnalysis.MoqAnalyzers","Philips.CodeAnalysis.MsTestAnalyzers"
                ]):
                    lines_covered = int(module.get("lines-covered","0"))
                    lines_valid = int(module.get("lines-valid","1"))
                    if lines_valid > 0:
                        analysis["overall_coverage"] = max(analysis["overall_coverage"], 100.0*lines_covered/lines_valid)
                    for ln in module.findall(".//line[@hits='0']"):
                        analysis["uncovered_lines"].append({
                            "file": ln.get("filename","unknown"),
                            "line": ln.get("number","?")
                        })
        except Exception as e:
            analysis["suggestions"].append({"type": "error", "message": f"Could not parse coverage.xml: {e}"})
    if analysis["overall_coverage"] < 80.0:
        gap = 80.0 - analysis["overall_coverage"]
        analysis["suggestions"].append({"type": "coverage_gap", "message": f"Current {analysis['overall_coverage']:.1f}% (< 80% by {gap:.1f}%)"})
        if analysis["uncovered_lines"]:
            sample = analysis["uncovered_lines"][:3]
            for u in sample:
                analysis["suggestions"].append({"type": "test_template",
                    "message": f"Add unit test exercising {u['file']}:{u['line']}"})
    return analysis

if __name__ == "__main__":
    mcp.run()
