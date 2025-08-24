#!/bin/bash
# © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.
#
# Script to start the Roslyn Analyzers MCP Server

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "🚀 Starting Roslyn Analyzers MCP Server..."

# Check if Python is available
if ! command -v python3 &> /dev/null; then
    echo "❌ Error: Python 3 is required but not installed."
    exit 1
fi

# Check if pip is available
if ! command -v pip &> /dev/null && ! command -v pip3 &> /dev/null; then
    echo "❌ Error: pip is required but not installed."
    exit 1
fi

# Install dependencies if requirements.txt exists and packages aren't installed
if [ -f "requirements.txt" ]; then
    echo "📦 Checking Python dependencies..."
    
    # Check if FastAPI is installed
    if ! python3 -c "import fastapi" 2>/dev/null; then
        echo "📦 Installing Python dependencies..."
        if command -v pip3 &> /dev/null; then
            pip3 install -r requirements.txt
        else
            pip install -r requirements.txt
        fi
    else
        echo "✅ Dependencies already installed"
    fi
fi

# Check if we're in the right directory
if [ ! -f "mcp_server.py" ]; then
    echo "❌ Error: mcp_server.py not found. Please run this script from the repository root."
    exit 1
fi

# Start the server
echo "🌐 Starting server at http://localhost:8000"
echo "📚 API docs available at http://localhost:8000/docs"
echo "🔄 Press Ctrl+C to stop the server"
echo ""

python3 mcp_server.py