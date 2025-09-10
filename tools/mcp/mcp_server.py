#!/usr/bin/env python3
import os
import re
import subprocess
import shutil
import requests
import base64
from pathlib import Path
from typing import List, Dict, Any, Optional, Set
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

def _parse_diagnostic_ids_from_csharp(content: str) -> Set[int]:
    """Parse diagnostic ID numbers from C# enum content."""
    diagnostic_ids = set()
    
    # Look for enum member assignments like "SomeName = 2159,"
    pattern = r'\s*\w+\s*=\s*(\d+)\s*[,}]'
    matches = re.findall(pattern, content)
    
    for match in matches:
        try:
            diagnostic_id = int(match)
            # Only include IDs in the expected range (2000+)
            if diagnostic_id >= 2000:
                diagnostic_ids.add(diagnostic_id)
        except ValueError:
            continue
    
    return diagnostic_ids

def _get_github_api_url(endpoint: str) -> str:
    """Build GitHub API URL for the roslyn-analyzers repository."""
    return f"https://api.github.com/repos/philips-software/roslyn-analyzers{endpoint}"

def _make_github_request(url: str, headers: Optional[Dict[str, str]] = None) -> Optional[Dict]:
    """Make a GitHub API request with error handling."""
    try:
        if headers is None:
            headers = {}
        
        # Add User-Agent header as required by GitHub API
        headers['User-Agent'] = 'roslyn-analyzers-mcp-server'
        
        # Add GitHub token if available from environment
        github_token = os.environ.get('GITHUB_TOKEN')
        if github_token:
            headers['Authorization'] = f'token {github_token}'
        
        response = requests.get(url, headers=headers, timeout=30)
        response.raise_for_status()
        return response.json()
    except Exception as e:
        # Return None on error, let calling code handle it
        return None

def _get_open_prs() -> List[Dict]:
    """Get list of open pull requests."""
    url = _get_github_api_url("/pulls?state=open&per_page=100")
    result = _make_github_request(url)
    return result if result else []

def _get_file_content_from_pr(pr_head_sha: str, file_path: str) -> Optional[str]:
    """Get file content from a specific PR commit."""
    url = _get_github_api_url(f"/contents/{file_path}?ref={pr_head_sha}")
    result = _make_github_request(url)
    
    if result and 'content' in result:
        try:
            # GitHub API returns base64-encoded content
            content = base64.b64decode(result['content']).decode('utf-8')
            return content
        except Exception:
            return None
    return None
    
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

@mcp.tool
def next_diagnosticId() -> Dict[str, Any]:
    """Get the next available DiagnosticId by checking main branch and all open PRs."""
    try:
        # Step 1: Get diagnostic IDs from main branch
        main_diagnostic_file = BASE_DIR / "Philips.CodeAnalysis.Common" / "DiagnosticId.cs"
        if not main_diagnostic_file.exists():
            return {
                "status": "failure",
                "error": "DiagnosticId.cs file not found in main branch",
                "next_id": None
            }
        
        main_content = main_diagnostic_file.read_text(encoding='utf-8')
        main_ids = _parse_diagnostic_ids_from_csharp(main_content)
        
        if not main_ids:
            return {
                "status": "failure", 
                "error": "No diagnostic IDs found in main branch",
                "next_id": None
            }
        
        max_main_id = max(main_ids)
        
        # Step 2: Get open PRs and check for new diagnostic IDs
        open_prs = _get_open_prs()
        pr_ids = set()
        pr_details = []
        
        for pr in open_prs:
            pr_number = pr.get('number', 'unknown')
            pr_title = pr.get('title', 'Unknown PR')
            pr_head_sha = pr.get('head', {}).get('sha')
            
            if not pr_head_sha:
                continue
            
            # Get DiagnosticId.cs from this PR
            pr_content = _get_file_content_from_pr(pr_head_sha, "Philips.CodeAnalysis.Common/DiagnosticId.cs")
            
            if pr_content:
                pr_diagnostic_ids = _parse_diagnostic_ids_from_csharp(pr_content)
                
                # Find IDs that are new in this PR (not in main)
                new_ids_in_pr = pr_diagnostic_ids - main_ids
                
                if new_ids_in_pr:
                    pr_ids.update(new_ids_in_pr)
                    pr_details.append({
                        "pr_number": pr_number,
                        "pr_title": pr_title,
                        "new_ids": sorted(list(new_ids_in_pr))
                    })
        
        # Step 3: Calculate the next available ID
        all_used_ids = main_ids.union(pr_ids)
        max_used_id = max(all_used_ids)
        next_id = max_used_id + 1
        
        return {
            "status": "success",
            "next_id": next_id,
            "max_main_id": max_main_id,
            "max_used_id": max_used_id,
            "total_open_prs": len(open_prs),
            "prs_with_new_ids": len(pr_details),
            "pr_details": pr_details,
            "diagnostic_id_string": f"PH{next_id}",
            "all_used_ids_count": len(all_used_ids)
        }
        
    except Exception as e:
        return {
            "status": "failure",
            "error": f"Unexpected error: {str(e)}",
            "next_id": None
        }

if __name__ == "__main__":
    mcp.run()
