# Roslyn Analyzers MCP Server

A focused Model Context Protocol (MCP) server that helps the Copilot Coding Agent find existing Helper.For methods and related utilities in the Philips Roslyn Analyzers repository.

## Core Problem Solved

The Copilot Coding Agent often overlooks existing Helper.ForXXX methods and creates new utility methods instead of using the comprehensive helper utilities already available in `Philips.CodeAnalysis.Common`. This server provides focused functionality to surface these existing helpers.

## Features

The MCP server provides streamlined endpoints for essential development tasks:

### Helper Discovery (Primary Focus)
- **`/search_helpers`** - Find Helper.For methods and related utilities that developers commonly miss

### Build & Test Automation  
- **`/build_strict`** - Build the solution with warnings treated as errors (`-warnaserror`)
- **`/run_tests`** - Execute tests (security-hardened, fixed target)
- **`/run_dogfood`** - Run the complete dogfooding process (build analyzers and apply them to the codebase)

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

## Dogfood Process

The dogfood process (`/run_dogfood`) automates the complete self-analysis workflow:

1. **Build Packages**: Creates `.Dogfood` versions of all analyzer packages
2. **Configure**: Creates temporary `Directory.Build.props` with dogfood package references
3. **Apply Analyzers**: Builds all projects with the analyzers applied to themselves
4. **Report Violations**: Returns any analyzer warnings/errors found

This process ensures that the analyzers work correctly and that the codebase follows its own rules.

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