# Roslyn Analyzers MCP Server

A Model Context Protocol (MCP) server that automates common development tasks for the Philips Roslyn Analyzers repository.

## Features

The MCP server provides the following endpoints to streamline development:

### File Navigation
- **`/list_files`** - List files in directories with optional filtering
- **`/get_file`** - Get file content with optional line range specification

### Code Analysis
- **`/search_symbols`** - Search for classes, methods, interfaces, and other symbols in the codebase

### Build & Test Automation
- **`/build_strict`** - Build the solution with warnings treated as errors (`-warnaserror`)
- **`/run_tests`** - Execute tests with optional target project specification
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

#### List C# Files
```bash
curl -X POST "http://localhost:8000/list_files" \
  -H "Content-Type: application/json" \
  -d '{"path": ".", "filters": ".cs"}'
```

#### Get File Content
```bash
curl -X POST "http://localhost:8000/get_file" \
  -H "Content-Type: application/json" \
  -d '{"path": "README.md"}'
```

#### Search for Classes
```bash
curl -X POST "http://localhost:8000/search_symbols" \
  -H "Content-Type: application/json" \
  -d '{"query": "DiagnosticAnalyzer"}'
```

#### Run Strict Build
```bash
curl -X POST "http://localhost:8000/build_strict"
```

#### Run Tests
```bash
curl -X POST "http://localhost:8000/run_tests" \
  -H "Content-Type: application/json" \
  -d '{"target": "Philips.CodeAnalysis.Test/Philips.CodeAnalysis.Test.csproj"}'
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