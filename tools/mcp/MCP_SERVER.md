# Roslyn Analyzers MCP Server

A focused Model Context Protocol (MCP) server that helps the Copilot Coding Agent navigate common problems, such as finding existing Helper.For methods, strict build rules, dogfooding analyzers on ourselves, and 80% code coverage on modified code.

## Core Problem Solved

The Copilot Coding Agent often overlooks existing Helper.ForXXX methods and creates new utility methods instead of using the comprehensive helper utilities already available in `Philips.CodeAnalysis.Common`. This server provides focused functionality to surface these existing helpers.

## Features

The MCP server provides streamlined endpoints for essential development tasks:

### Helper Discovery (Primary Focus)
- **`/search_helpers`** - Find Helper.For methods and related utilities that developers commonly miss

### Code Coverage Analysis (New!)
- **`/analyze_coverage`** - Analyze code coverage and provide actionable suggestions to reach SonarCloud's 80% requirement

### Build & Test Automation  
- **`/build_strict`** - Build the solution with warnings treated as errors (`-warnaserror`)
- **`/run_tests`** - Execute tests (security-hardened, fixed target)
- **`/run_dogfood`** - Run the complete dogfooding process (build analyzers and apply them to the codebase)

## Coverage Analysis for SonarCloud

The `/analyze_coverage` endpoint specifically addresses SonarCloud's 80% code coverage requirement that often causes the Copilot Coding Agent to fall short. This endpoint:

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

### Information
- **`/manifest`** - Get server manifest with endpoint descriptions
- **`/health`** - Health check endpoint

## Installation

1. **Install Python dependencies:**
   ```bash
   pip install -r tools/mcp/requirements.txt
   ```

2. **Start the server:**
   ```bash
   cd tools/mcp
   python mcp_server.py
   ```

   The server will start on `http://localhost:8000`

## Usage Examples

### Starting the Server
```bash
# From the repository root
cd tools/mcp
python mcp_server.py
```

### Using the Startup Script
```bash
# From the repository root
./tools/mcp/start_mcp_server.sh
```

### Example API Calls

#### Search for Helper Methods (Primary Feature)
```bash
curl -X POST "http://localhost:8000/search_helpers"
```

#### Run Strict Build
```bash
curl -X POST "http://localhost:8000/build_strict"
```

#### Run Tests (Security-Hardened)
```bash
curl -X POST "http://localhost:8000/run_tests"
```

#### Run Dogfood Process
```bash
curl -X POST "http://localhost:8000/run_dogfood"
```

#### Analyze Code Coverage (New!)
```bash
curl -X POST "http://localhost:8000/analyze_coverage"
```

This endpoint helps reach SonarCloud's 80% coverage requirement by:
- Running tests with coverage analysis
- Identifying specific uncovered lines and methods
- Providing actionable suggestions for improving coverage
- Generating test templates for uncovered code sections

## Dogfood Process

The dogfood process (`/run_dogfood`) automates the complete self-analysis workflow:

1. **Build Dogfood Packages**: Creates `.Dogfood` versions of all analyzer packages by setting `PackageId=$(MSBuildProjectName).Dogfood` in Directory.Build.props and building with Release configuration
2. **Add Package Source**: Adds the local `Packages/` directory as a NuGet source for consuming the dogfood packages
3. **Configure Consumption**: Creates Directory.Build.props with package references to all dogfood analyzer packages with proper `PrivateAssets` and `IncludeAssets` settings
4. **Apply Analyzers**: Cleans and builds all projects (Debug configuration) with the dogfood analyzers applied to themselves
5. **Report Violations**: Returns any analyzer warnings/errors found (CS or PH codes)

This process follows the same workflow as `.github/workflows/dogfood.yml` to ensure that the analyzers work correctly and that the codebase follows its own rules.

**Testing the Dogfood Implementation**: Since the main codebase currently has no dogfood violations, you can test the implementation by temporarily introducing a known violation (such as an empty catch block) in a source file, running the dogfood analysis, and verifying that it detects the violation. The implementation successfully detects analyzer codes like PH2097 (empty statement blocks) and PH2098 (empty catch blocks).

## Development

The server is designed to be run from the repository root directory. It automatically:
- Uses the current working directory as the base for all operations
- Skips binary files and build artifacts when listing files
- Provides detailed error messages and logging
- Handles temporary file cleanup automatically

## API Documentation

When the server is running, visit `http://localhost:8000/docs` for interactive API documentation powered by FastAPI's automatic Swagger UI generation.

## Error Handling

The server provides comprehensive error handling:
- File not found errors (404)
- Invalid parameters (400)  
- Build/test failures with detailed logs
- Automatic cleanup of temporary files

All endpoints return structured JSON responses with status indicators and detailed error messages when applicable.
