# Roslyn Analyzers MCP Server

A focused Model Context Protocol (MCP) server that helps Claude Code, Codex, and the Copilot Coding Agent navigate common problems, such as finding existing Helper.For methods, strict build rules, dogfooding analyzers on ourselves, and 80% code coverage on modified code.

## Core Problem Solved

Coding agents can overlook existing Helper.ForXXX methods and create new utility methods instead of using the comprehensive helper utilities already available in `Philips.CodeAnalysis.Common`. This server provides focused functionality to surface these existing helpers.

## Features

The MCP server provides streamlined tools for essential development tasks:

### Helper Discovery (Primary Focus)

- **`search_helpers`** - Find Helper.For methods and related utilities that developers commonly miss

### Diagnostic ID Management (New!)

- **`next_diagnosticId`** - Determine the next available DiagnosticId by examining main branch and all open PRs to avoid conflicts

### Code Formatting

- **`fix_formatting`** - Auto-fix code formatting issues using `dotnet format`. Addresses IDE0055 violations including CRLF line endings and tab indentation to reduce CoPilot struggles with formatting

### Code Coverage Analysis

- **`analyze_coverage`** - Analyze code coverage and provide actionable suggestions to reach SonarCloud's 80% requirement

### Build & Test Automation  

- **`build_strict`** - Build the solution with warnings treated as errors (`-warnaserror`)
- **`run_tests`** - Execute tests (security-hardened, fixed target)
- **`run_dogfood`** - Run the complete dogfooding process (build analyzers and apply them to the codebase)

## Diagnostic ID Management

The `next_diagnosticId` tool solves the problem of concurrent Pull Requests trying to claim the same diagnostic ID number. When multiple developers work on new analyzers in parallel, they often pick the same "next" ID, causing conflicts during code review.

**Key Benefits:**
- **Conflict prevention** - Scans both main branch and open PRs to find truly available IDs
- **Automated analysis** - Parses DiagnosticId.cs enum automatically  
- **GitHub API integration** - Uses GitHub API to check open PRs for ID conflicts
- **Graceful fallback** - Works even without GitHub API access by analyzing main branch

**Sample Response:**
```json
{
  "status": "success", 
  "next_diagnostic_id": 2160,
  "main_branch_max": 2159,
  "main_branch_count": 140,
  "pr_conflicts": [
    {
      "pr_number": 123,
      "pr_title": "Add new analyzer PH2160", 
      "new_ids": [2160]
    }
  ],
  "pr_new_ids": [2160],
  "recommendation": "Use DiagnosticId = 2161",
  "note": "This accounts for both main branch and open PRs to avoid conflicts"
}
```

## Code Formatting Assistance

The `fix_formatting` tool specifically addresses the Copilot Coding Agent's struggle with IDE0055 formatting violations. This repository enforces strict formatting rules:

- **Line endings**: CRLF (Windows-style) - not LF
- **Indentation**: Tabs (not spaces) with size 4
- **Braces**: New line before all braces
- **IDE0055 severity**: Error (build fails on violations)

**Key Benefits:**
- **Auto-corrects formatting** - Fixes CRLF, tabs, braces, and other .editorconfig violations
- **Reduces agent struggles** - Prevents 25% of CoPilot effort being spent on formatting
- **Ensures CI compliance** - Matches the exact formatting that CI requires
- **Zero-config operation** - Uses existing .editorconfig rules automatically

**Sample Response:**
```json
{
  "status": "success",
  "return_code": 0,
  "formatted_files": 15,
  "message": "Fixed formatting for 15 files",
  "logs": "..."
}
```

## Coverage Analysis for SonarCloud

The `analyze_coverage` tool specifically addresses SonarCloud's 80% code coverage requirement that often causes the Copilot Coding Agent to fall short. This tool:

**Key Benefits:**
- **Identifies coverage gaps** - Pinpoints exact uncovered lines and methods
- **Provides actionable suggestions** - Offers specific test cases to improve coverage  
- **Generates test templates** - Creates skeleton test methods for uncovered code
- **Prioritizes testing areas** - Focuses on error handling, edge cases, and complex logic

**Sample Response:**
```json
{
  "overall_coverage": 72.5,
  "status": "success", 
  "uncovered_lines": [
    {"file": "Helper.cs", "line": 45, "suggestion": "Add test case that executes line 45"}
  ],
  "suggestions": [
    {"type": "coverage_gap", "message": "Current coverage: 72.5%, Target: 80%, Gap: 7.5%"},
    {"type": "test_strategy", "message": "Focus on testing error handling, edge cases, and exception paths"},
    {"type": "test_template", "message": "Test template: [Test] public void TestHelperLine45() { /* Add test */ }"}
  ]
}
```

## Installation

1. **Install Python dependencies:**
   ```bash
   python -m pip install --requirement tools/mcp/requirements.txt
   ```

2. **Start the stdio server:**
   ```bash
   cd tools/mcp
   python mcp_server.py
   ```

   Claude Code and Codex start this command automatically from their checked-in
   project configuration. Running it manually starts the same stdio transport.

## Usage

After installing the dependency, restart Claude Code or Codex so it loads the
checked-in project MCP configuration. The server exposes the tools listed above
through the client's normal MCP tool interface.

Run the stdio integration test directly to verify the entry point and complete
tool catalog:

```bash
python tools/mcp/test_stdio_server.py
```

The `analyze_coverage` tool helps reach SonarCloud's 80% coverage requirement by:
- Running tests with coverage analysis
- Identifying specific uncovered lines and methods
- Providing actionable suggestions for improving coverage
- Generating test templates for uncovered code sections

## Dogfood Process

The `run_dogfood` tool automates the complete self-analysis workflow:

1. **Build Dogfood Packages**: Creates `.Dogfood` versions of all analyzer packages by setting `PackageId=$(MSBuildProjectName).Dogfood` in Directory.Build.props and building with Release configuration
2. **Add Package Source**: Adds the local `Packages/` directory as a NuGet source for consuming the dogfood packages
3. **Configure Consumption**: Creates Directory.Build.props with package references to all dogfood analyzer packages with proper `PrivateAssets` and `IncludeAssets` settings
4. **Apply Analyzers**: Cleans and builds all projects (Debug configuration) with the dogfood analyzers applied to themselves
5. **Report Violations**: Returns any analyzer warnings/errors found (CS or PH codes)

This process follows the same workflow as `.github/workflows/dogfood.yml` to ensure that the analyzers work correctly and that the codebase follows its own rules.

**Testing the Dogfood Implementation**: Since the main codebase currently has no dogfood violations, you can test the implementation by temporarily introducing a known violation (such as an empty catch block) in a source file, running the dogfood analysis, and verifying that it detects the violation. The implementation successfully detects analyzer codes like PH2097 (empty statement blocks) and PH2098 (empty catch blocks).

## Development

The server can be started from any directory. It automatically:

- Resolves the repository root from the location of `mcp_server.py`
- Skips binary files and build artifacts when listing files
- Provides detailed error messages and logging
- Handles temporary file cleanup automatically

## Error Handling

The server provides comprehensive error handling:

- File-not-found results
- Invalid-parameter results
- Build/test failures with detailed logs
- Automatic cleanup of temporary files

All tools return structured results with status indicators and detailed error messages when applicable.
