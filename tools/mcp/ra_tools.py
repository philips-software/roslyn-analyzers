# tools/mcp/ra_tools.py
import os
import re
import shutil
import subprocess
import urllib.request
import urllib.error
import json
from pathlib import Path
from typing import Dict, Any, List

# The host passes BASE_DIR in; keep it global here
BASE_DIR: Path = Path(".")

DEFAULT_TIMEOUT = 900

def _run(cmd: list[str], timeout: int = DEFAULT_TIMEOUT) -> tuple[int, str]:
    if (not isinstance(cmd, list) or not cmd or
        not all(isinstance(x, str) for x in cmd) or
        any(any(c in x for c in ['&','|','$','`','>','<']) for x in cmd)):
        raise ValueError("Unsafe or invalid command list passed to _run")
    p = subprocess.run(cmd, cwd=BASE_DIR, capture_output=True, text=True, shell=False, timeout=timeout)
    return p.returncode, (p.stdout or "") + (p.stderr or "")

def _coverage_exe() -> str:
    """
    Return an absolute path to dotnet-coverage if installed globally; otherwise
    fall back to 'dotnet-coverage' and let the call fail gracefully.
    """
    home = Path.home()
    candidate = home / ".dotnet" / "tools" / ("dotnet-coverage.exe" if os.name == "nt" else "dotnet-coverage")
    return str(candidate) if candidate.exists() else "dotnet-coverage"

def set_base_dir(p: str) -> None:
    global BASE_DIR
    BASE_DIR = Path(p)

def search_helpers() -> Dict[str, Any]:
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
            if any(s in line for s in ("public static","Helper.For","ForAllowedSymbols","ForAdditionalFiles")):
                ctx = content[max(0, i-1): i+3]
                hits.append({"file": rel, "line": i+1, "content": line.strip(), "context": ctx})
    return {"status":"success","helpers_count":len(hits),"helpers":hits[:50]}

def build_strict() -> Dict[str, Any]:
    _run(["dotnet","clean","Philips.CodeAnalysis.sln"])
    rc, out = _run(["dotnet","build","Philips.CodeAnalysis.sln","--configuration","Release","--no-incremental","-warnaserror"])
    errors = [ln.strip() for ln in out.splitlines()
              if "error" in ln.lower() and (" cs" in ln.lower() or " ph" in ln.lower() or "netsdk" in ln.lower() or " mstest" in ln.lower())]
    return {"status":"success" if rc==0 else "failure","return_code":rc,"errors":errors,"logs":out[-8000:]}

def _ensure_restored() -> bool:
    state = BASE_DIR / ".mcp_state"
    state.mkdir(exist_ok=True)
    sentinel = state / "restored.ok"
    if sentinel.exists():
        return False
    _run(["dotnet","--info"], timeout=60)
    _run(["dotnet","restore","Philips.CodeAnalysis.sln"], timeout=600)
    _run(["dotnet","build","Philips.CodeAnalysis.Test/Philips.CodeAnalysis.Test.csproj","--configuration","Release","--no-restore"], timeout=600)
    sentinel.write_text("ok", encoding="utf-8")
    return True

def run_tests() -> Dict[str, Any]:
    did_restore_now = _ensure_restored()
    cmd = ["dotnet","test","Philips.CodeAnalysis.Test/Philips.CodeAnalysis.Test.csproj",
           "--configuration","Release","--logger","trx;LogFileName=test-results.trx","--no-restore"]
    test_bin = BASE_DIR/"Philips.CodeAnalysis.Test"/"bin"/"Release"
    if test_bin.exists():
        cmd.append("--no-build")
    timeout = 600 if did_restore_now else 180
    rc, out = _run(cmd, timeout=timeout)

    test_results = {"passed":0,"failed":0,"skipped":0,"total":0,"duration":""}
    test_summary = ""
    lines = out.splitlines()
    for line in lines:
        if ("Passed!" in line or "Failed!" in line) and "Total:" in line:
            test_summary = line.strip()
            import re as _re
            for key,pat in [("failed",r"Failed:\s*(\d+)"),("passed",r"Passed:\s*(\d+)"),
                            ("skipped",r"Skipped:\s*(\d+)"),("total",r"Total:\s*(\d+)"),
                            ("duration",r"Duration:\s*([^-]+?)(?:\s*-|$)")]:
                m = _re.search(pat, line)
                if m: test_results[key] = int(m.group(1)) if key!="duration" else m.group(1).strip()
            break
    keep = ("Test run for","Test Execution","Starting test execution","test files matched","Results File:","Passed!","Failed!","Warning:","Error:")
    filtered = "\n".join([ln for ln in lines if any(k in ln for k in keep) or not ln.strip()]).strip()
    return {"status":"success" if rc==0 else "failure","return_code":rc,"test_results":test_results,
            "summary": test_summary or f"{test_results['passed']} passed, {test_results['failed']} failed, {test_results['skipped']} skipped, {test_results['total']} total",
            "logs": filtered[-4000:] if filtered else ""}

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

def _parse_diagnostic_ids_from_content(content: str) -> List[int]:
    """Parse DiagnosticId enum values from C# file content."""
    ids = []
    # Match pattern like "SomeName = 2159," - must be within enum context
    # Look for valid C# identifier followed by = number,
    pattern = r'^\s*[A-Za-z_][A-Za-z0-9_]*\s*=\s*(\d+)\s*,?\s*$'
    
    # Only parse lines that look like enum entries (indented, with identifiers)
    for line in content.splitlines():
        # Skip lines that don't look like enum entries
        stripped = line.strip()
        if not stripped or '//' in stripped or stripped.startswith('namespace') or stripped.startswith('public') or stripped.startswith('{') or stripped.startswith('}'):
            continue
            
        match = re.match(pattern, line)
        if match:
            ids.append(int(match.group(1)))
    return ids

def _get_github_api_token() -> str:
    """Get GitHub API token from environment variables."""
    # Check common environment variable names for GitHub tokens
    for var_name in ['GITHUB_TOKEN', 'GH_TOKEN', 'GITHUB_API_TOKEN']:
        token = os.environ.get(var_name)
        if token:
            return token
    return ""

def _github_api_request(url: str) -> Dict[str, Any]:
    """Make a GitHub API request with authentication if available."""
    token = _get_github_api_token()
    
    try:
        req = urllib.request.Request(url)
        if token:
            req.add_header('Authorization', f'token {token}')
        req.add_header('Accept', 'application/vnd.github.v3+json')
        req.add_header('User-Agent', 'roslyn-analyzers-mcp-tool')
        
        with urllib.request.urlopen(req, timeout=30) as response:
            return json.loads(response.read().decode('utf-8'))
    except urllib.error.HTTPError as e:
        if e.code == 403:
            # Rate limit or authentication issue
            raise Exception(f"GitHub API rate limit or authentication error: {e}")
        elif e.code == 404:
            # Not found
            return {}
        else:
            raise Exception(f"GitHub API error {e.code}: {e}")
    except Exception as e:
        raise Exception(f"Error accessing GitHub API: {e}")

def _get_file_content_from_pr(owner: str, repo: str, pr_number: int, file_path: str) -> str:
    """Get file content from a specific PR."""
    try:
        # Get PR details to find the head SHA
        pr_url = f"https://api.github.com/repos/{owner}/{repo}/pulls/{pr_number}"
        pr_data = _github_api_request(pr_url)
        
        if not pr_data or 'head' not in pr_data:
            return ""
        
        head_sha = pr_data['head']['sha']
        
        # Get file content from the PR's head commit
        file_url = f"https://api.github.com/repos/{owner}/{repo}/contents/{file_path}?ref={head_sha}"
        file_data = _github_api_request(file_url)
        
        if not file_data or 'content' not in file_data:
            return ""
        
        # Decode base64 content
        import base64
        content = base64.b64decode(file_data['content']).decode('utf-8')
        return content
    except Exception:
        # If we can't get the file content, just return empty
        return ""

def next_diagnosticId() -> Dict[str, Any]:
    """Determine the next available DiagnosticId by examining main branch and all open PRs."""
    
    try:
        # Step 1: Parse current DiagnosticId enum from main branch (local)
        diagnostic_file = BASE_DIR / "Philips.CodeAnalysis.Common" / "DiagnosticId.cs"
        if not diagnostic_file.exists():
            return {"status": "error", "message": "DiagnosticId.cs file not found"}
        
        main_content = diagnostic_file.read_text(encoding="utf-8", errors="replace")
        main_ids = _parse_diagnostic_ids_from_content(main_content)
        
        if not main_ids:
            return {"status": "error", "message": "No diagnostic IDs found in main branch"}
        
        main_max = max(main_ids)
        
        # Step 2: Scan open PRs for new DiagnosticId values
        pr_ids = []
        pr_conflicts = []
        
        # Determine repository owner and name from git remote
        try:
            rc, git_output = _run(["git", "remote", "get-url", "origin"], timeout=10)
            if rc == 0 and git_output:
                # Parse git URL to get owner/repo (handle both SSH and HTTPS)
                git_url = git_output.strip()
                if "github.com" in git_url:
                    if git_url.startswith("git@"):
                        # SSH format: git@github.com:owner/repo.git
                        parts = git_url.split(":")[-1].replace(".git", "").split("/")
                    else:
                        # HTTPS format: https://github.com/owner/repo.git
                        parts = git_url.split("github.com/")[-1].replace(".git", "").split("/")
                    
                    if len(parts) >= 2:
                        owner, repo = parts[0], parts[1]
                        
                        # Get open PRs
                        prs_url = f"https://api.github.com/repos/{owner}/{repo}/pulls?state=open&per_page=100"
                        prs_data = _github_api_request(prs_url)
                        
                        if isinstance(prs_data, list):
                            for pr in prs_data:
                                pr_number = pr.get('number')
                                pr_title = pr.get('title', '')
                                
                                # Get DiagnosticId.cs content from this PR
                                pr_content = _get_file_content_from_pr(owner, repo, pr_number, "Philips.CodeAnalysis.Common/DiagnosticId.cs")
                                
                                if pr_content:
                                    pr_ids_for_this_pr = _parse_diagnostic_ids_from_content(pr_content)
                                    # Find new IDs not in main branch
                                    new_ids = [id for id in pr_ids_for_this_pr if id not in main_ids]
                                    if new_ids:
                                        pr_ids.extend(new_ids)
                                        pr_conflicts.append({
                                            "pr_number": pr_number,
                                            "pr_title": pr_title,
                                            "new_ids": new_ids
                                        })
        
        except Exception as e:
            # If GitHub API fails, we can still work with main branch
            pr_conflicts.append({"error": f"Could not scan PRs: {str(e)}"})
        
        # Step 3: Calculate next available ID
        all_ids = main_ids + pr_ids
        next_id = max(all_ids) + 1 if all_ids else 2160
        
        # Step 4: Return results
        return {
            "status": "success",
            "next_diagnostic_id": next_id,
            "main_branch_max": main_max,
            "main_branch_count": len(main_ids),
            "pr_conflicts": pr_conflicts,
            "pr_new_ids": pr_ids,
            "recommendation": f"Use DiagnosticId = {next_id}",
            "note": "This accounts for both main branch and open PRs to avoid conflicts"
        }
        
    except Exception as e:
        return {
            "status": "error", 
            "message": f"Error determining next DiagnosticId: {str(e)}",
            "fallback_recommendation": "Check DiagnosticId.cs manually and use the next sequential number"
        }