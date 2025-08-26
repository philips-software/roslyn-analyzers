#!/usr/bin/env python3
import os
import subprocess
import shutil
import json
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
        or any(any(c in x for c in [';', '&', '|', '$', '`', '>', '<']) for x in cmd)
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
    errors = [ln.strip() for ln in out.splitlines() if "error" in ln.lower() and (" cs" in ln.lower() or " ph" in ln.lower())]
    return {"status": "success" if rc == 0 else "failure", "return_code": rc, "errors": errors, "logs": out}

@mcp.tool
def run_tests() -> Dict[str, Any]:
    """Run tests against main test project (no build)."""
    rc, out = _run([
        "dotnet", "test", "Philips.CodeAnalysis.Test/Philips.CodeAnalysis.Test.csproj",
        "--configuration", "Release", "--logger", "trx;LogFileName=test-results.trx", "--no-build"
    ])
    return {"status": "success" if rc == 0 else "failure", "return_code": rc, "results": out}

@mcp.tool
def run_dogfood() -> Dict[str, Any]:
    """Build analyzers, add dogfood packages, and build all projects to collect analyzer findings."""
    props = BASE_DIR / "Directory.Build.props"
    backup = None
    violations: List[Dict[str, str]] = []
    try:
        if props.exists():
            backup = props.with_suffix(".props.backup")
            shutil.copy2(props, backup)
        props.write_text("""<Project>
  <PropertyGroup><FileVersion>1.0.0</FileVersion></PropertyGroup>
</Project>
""", encoding="utf-8")

        # Build everything (quiet logs), collect warnings/errors marked by CS/PH
        projects = [
            "Philips.CodeAnalysis.Common/Philips.CodeAnalysis.Common.csproj",
            "Philips.CodeAnalysis.DuplicateCodeAnalyzer/Philips.CodeAnalysis.DuplicateCodeAnalyzer.csproj",
            "Philips.CodeAnalysis.MaintainabilityAnalyzers/Philips.CodeAnalysis.MaintainabilityAnalyzers.csproj",
            "Philips.CodeAnalysis.MoqAnalyzers/Philips.CodeAnalysis.MoqAnalyzers.csproj",
            "Philips.CodeAnalysis.MsTestAnalyzers/Philips.CodeAnalysis.MsTestAnalyzers.csproj",
            "Philips.CodeAnalysis.SecurityAnalyzers/Philips.CodeAnalysis.SecurityAnalyzers.csproj",
            "Philips.CodeAnalysis.Test/Philips.CodeAnalysis.Test.csproj",
            "Philips.CodeAnalysis.Benchmark/Philips.CodeAnalysis.Benchmark.csproj",
            "Philips.CodeAnalysis.AnalyzerPerformance/Philips.CodeAnalysis.AnalyzerPerformance.csproj"
        ]
        for proj in projects:
            framework = "net8.0" if proj.endswith(("Test.csproj","Benchmark.csproj","AnalyzerPerformance.csproj")) else "netstandard2.0"
            rc, out = _run(["dotnet", "build", proj, "--configuration", "Debug", "--framework", framework,
                            "-consoleloggerparameters:NoSummary", "-verbosity:quiet"])
            for ln in (out or "").splitlines():
                low = ln.lower()
                if ("warning" in low or "error" in low) and (" cs" in low or " ph" in low):
                    violations.append({"project": proj, "violation": ln.strip()})
        return {"status": "success" if not violations else "failure", "violation_count": len(violations), "violations": violations}
    finally:
        if props.exists(): props.unlink()
        if backup and backup.exists(): shutil.move(backup, props)

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
