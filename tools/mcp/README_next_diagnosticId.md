# next_diagnosticId Tool

## Purpose

The `next_diagnosticId` tool solves the common problem in the Roslyn Analyzers repository where multiple Pull Requests try to claim the same "next" diagnostic ID, causing merge conflicts and manual coordination overhead.

## Problem Solved

When developers work on new analyzers in parallel:

1. **PR A** looks at main branch, sees max ID is 2159, uses 2160
2. **PR B** also looks at main branch, sees max ID is 2159, also uses 2160
3. **Conflict occurs** when both PRs try to merge

Without this tool, maintainers have to:
- Manually review each PR for diagnostic ID conflicts
- Check all other open PRs to see what IDs are being used
- Request changes to resolve conflicts
- Coordinate between PR authors

## Solution

The `next_diagnosticId` tool:

1. ✅ **Examines main branch** - Parses `DiagnosticId.cs` to find current maximum ID
2. ✅ **Scans all open PRs** - Uses GitHub API to get all open Pull Requests  
3. ✅ **Analyzes each PR** - Gets `DiagnosticId.cs` from each PR's head commit
4. ✅ **Finds new IDs** - Identifies diagnostic IDs being added in each PR
5. ✅ **Calculates true next** - Returns max(main_branch_ids + all_pr_ids) + 1

## Usage

### Via MCP Server

```bash
# Start the MCP server
cd tools/mcp
python3 mcp_server.py

# Call the tool
curl -X POST "http://localhost:8000/next_diagnosticId"
```

### Sample Response

```json
{
  "status": "success",
  "next_id": 2161,
  "diagnostic_id_string": "PH2161",
  "max_main_id": 2159,
  "max_used_id": 2160,
  "total_open_prs": 3,
  "prs_with_new_ids": 1,
  "pr_details": [
    {
      "pr_number": 1234,
      "pr_title": "Add new analyzer for method naming",
      "new_ids": [2160]
    }
  ],
  "all_used_ids_count": 140
}
```

### What This Means

- **Current state**: Main branch has IDs up to 2159
- **Conflict detected**: PR #1234 is already using 2160
- **Safe to use**: 2161 (PH2161) is the next available ID
- **No conflicts**: Using 2161 will not conflict with any existing PR

## For AI Coding Agents

When implementing new analyzers:

1. **Don't guess** the next diagnostic ID by looking at main branch
2. **Call this tool** to get the true next available ID
3. **Use the returned ID** to avoid conflicts with open PRs

### Example Workflow

```bash
# Get next available diagnostic ID
NEXT_ID=$(curl -s -X POST "http://localhost:8000/next_diagnosticId" | jq -r '.next_id')
DIAGNOSTIC_STRING=$(curl -s -X POST "http://localhost:8000/next_diagnosticId" | jq -r '.diagnostic_id_string')

echo "Use diagnostic ID: $NEXT_ID"
echo "Diagnostic string: $DIAGNOSTIC_STRING"
```

## Authentication

The tool works without authentication but is subject to GitHub's API rate limits:
- **Unauthenticated**: 60 requests per hour per IP
- **With GITHUB_TOKEN**: 5,000 requests per hour

To use with authentication:
```bash
export GITHUB_TOKEN=your_github_token
python3 mcp_server.py
```

## Error Handling

The tool gracefully handles:
- ✅ **GitHub API rate limits** - Falls back to main branch analysis
- ✅ **Network issues** - Returns main branch + 1 if GitHub is unreachable  
- ✅ **Invalid PR content** - Skips PRs that can't be parsed
- ✅ **Missing files** - Ignores PRs that don't modify DiagnosticId.cs

## Testing

```bash
# Run unit tests
cd tools/mcp
python3 test_next_diagnostic_id.py

# Run conflict scenario tests  
python3 test_conflict_scenarios.py

# Run manual integration test
python3 manual_test_next_diagnostic_id.py
```

## Current State

As of the latest update:
- **Main branch max ID**: 2159 (AvoidUnnecessaryAttributeParentheses)
- **Next available ID**: 2160 (PH2160)
- **Total diagnostic IDs**: 139

The tool is ready for production use and will prevent diagnostic ID conflicts between parallel development efforts.