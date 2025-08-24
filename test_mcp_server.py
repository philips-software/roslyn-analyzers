#!/usr/bin/env python3
"""
Test script for the Roslyn Analyzers MCP Server

© 2025 Koninklijke Philips N.V. See License.md in the project root for license information.
"""

import requests
import json
import time
import sys
import subprocess
import signal
import os
from pathlib import Path

BASE_URL = "http://localhost:8000"
SERVER_SCRIPT = Path(__file__).parent / "mcp_server.py"

def start_server():
    """Start the MCP server in the background."""
    env = os.environ.copy()
    env['PYTHONPATH'] = str(Path(__file__).parent)
    
    process = subprocess.Popen(
        [sys.executable, str(SERVER_SCRIPT)],
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        cwd=Path(__file__).parent
    )
    
    # Wait for server to start
    print("🚀 Starting MCP server...")
    for i in range(30):  # Wait up to 30 seconds
        try:
            response = requests.get(f"{BASE_URL}/health", timeout=1)
            if response.status_code == 200:
                print("✅ Server started successfully")
                return process
        except requests.exceptions.RequestException:
            time.sleep(1)
    
    print("❌ Failed to start server")
    process.terminate()
    return None

def test_endpoint(name, method, path, data=None):
    """Test a single endpoint."""
    try:
        print(f"\n🧪 Testing {name}...")
        
        if method.upper() == "GET":
            response = requests.get(f"{BASE_URL}{path}", timeout=10)
        else:
            response = requests.post(f"{BASE_URL}{path}", json=data, timeout=10)
        
        if response.status_code == 200:
            result = response.json()
            print(f"   ✅ {name} - Status: {response.status_code}")
            return True, result
        else:
            print(f"   ❌ {name} - Status: {response.status_code}")
            print(f"   Response: {response.text[:200]}")
            return False, None
            
    except requests.exceptions.Timeout:
        print(f"   ⏰ {name} - Timeout")
        return False, None
    except Exception as e:
        print(f"   ❌ {name} - Error: {str(e)}")
        return False, None

def run_tests():
    """Run all MCP server tests."""
    print("🧪 Testing Roslyn Analyzers MCP Server")
    print("=" * 50)
    
    server_process = start_server()
    if not server_process:
        return False
    
    try:
        tests = [
            ("Health Check", "GET", "/health", None),
            ("Root Info", "GET", "/", None),
            ("Manifest", "GET", "/manifest", None),
            ("List Files", "POST", "/list_files", {"path": ".", "filters": ".md"}),
            ("Get File", "POST", "/get_file", {"path": "README.md", "lines": "1:2"}),
            ("Search Symbols", "POST", "/search_symbols", {"query": "Analyzer"}),
            ("Build Strict", "POST", "/build_strict", None),
        ]
        
        passed = 0
        total = len(tests)
        
        for name, method, path, data in tests:
            success, result = test_endpoint(name, method, path, data)
            if success:
                passed += 1
                
                # Print some sample results
                if name == "List Files" and result:
                    files = result.get("files", [])
                    print(f"   📄 Found {len(files)} files")
                    
                elif name == "Search Symbols" and result:
                    matches = result.get("matches", [])
                    print(f"   🔍 Found {len(matches)} matches")
                    
                elif name == "Build Strict" and result:
                    status = result.get("status", "unknown")
                    errors = len(result.get("errors", []))
                    print(f"   🔨 Build {status}, {errors} errors")
        
        print(f"\n📊 Test Results: {passed}/{total} tests passed")
        
        if passed == total:
            print("🎉 All tests passed!")
            return True
        else:
            print("❌ Some tests failed")
            return False
            
    finally:
        print("\n🛑 Stopping server...")
        server_process.terminate()
        try:
            server_process.wait(timeout=5)
        except subprocess.TimeoutExpired:
            server_process.kill()
            server_process.wait()

if __name__ == "__main__":
    success = run_tests()
    sys.exit(0 if success else 1)