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
        
        # Set timeout based on endpoint type
        timeout = 10  # Default timeout
        if name in ["Build Strict", "Run Dogfood"]:
            timeout = 180  # 3 minutes for build operations
        elif name == "Run Tests":
            timeout = 60   # 1 minute for tests
        elif name == "Analyze Coverage":
            timeout = 180  # 3 minutes for coverage analysis
        
        if method.upper() == "GET":
            response = requests.get(f"{BASE_URL}{path}", timeout=timeout)
        else:
            response = requests.post(f"{BASE_URL}{path}", json=data, timeout=timeout)
        
        if response.status_code == 200:
            result = response.json()
            print(f"   ✅ {name} - Status: {response.status_code}")
            return True, result
        else:
            print(f"   ❌ {name} - Status: {response.status_code}")
            print(f"   Response: {response.text[:200]}")
            return False, None
            
    except requests.exceptions.Timeout:
        print(f"   ⏰ {name} - Timeout (this may be expected for long operations)")
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
            ("Search Helpers", "POST", "/search_helpers", None),
            ("Build Strict", "POST", "/build_strict", None),
            ("Run Tests", "POST", "/run_tests", None),
            ("Run Dogfood", "POST", "/run_dogfood", None),
            ("Analyze Coverage", "POST", "/analyze_coverage", None),
        ]
        
        passed = 0
        total = len(tests)
        
        for name, method, path, data in tests:
            success, result = test_endpoint(name, method, path, data)
            if success:
                passed += 1
                
                # Print some sample results
                if name == "Search Helpers" and result:
                    helpers = result.get("helpers", [])
                    count = result.get("helpers_count", 0)
                    print(f"   🔍 Found {count} helper methods")
                    
                elif name == "Build Strict" and result:
                    status = result.get("status", "unknown")
                    errors = len(result.get("errors", []))
                    print(f"   🔨 Build {status}, {errors} errors")
                
                elif name == "Run Tests" and result:
                    status = result.get("status", "unknown")
                    print(f"   🧪 Tests {status}")
                
                elif name == "Run Dogfood" and result:
                    status = result.get("status", "unknown")
                    violations = len(result.get("violations", []))
                    print(f"   🐕 Dogfood {status}, {violations} violations")
                
                elif name == "Analyze Coverage" and result:
                    status = result.get("status", "unknown")
                    coverage = result.get("overall_coverage", 0)
                    suggestions = len(result.get("suggestions", []))
                    print(f"   📊 Coverage {status}, {coverage:.1f}% coverage, {suggestions} suggestions")
        
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