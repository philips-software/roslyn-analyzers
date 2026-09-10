#!/usr/bin/env python3
"""Smoke tests for the MCP stdio entry point and agent tool exposure."""

import json
from pathlib import Path
import unittest

from fastmcp import Client


class McpStdioServerTests(unittest.IsolatedAsyncioTestCase):
	async def test_expected_tools_are_available(self) -> None:
		server = Path(__file__).with_name("mcp_server.py")
		async with Client(server, mode="legacy") as client:
			tools = await client.list_tools()

		actual = {tool.name for tool in tools}
		expected = {
			"analyze_coverage",
			"build_strict",
			"fix_formatting",
			"hot_reload",
			"next_diagnosticId",
			"run_dogfood",
			"run_tests",
			"search_helpers",
		}
		self.assertEqual(expected, actual)

		copilot_config = json.loads(
			server.with_name("github-mcp-config.json").read_text(encoding="utf-8")
		)
		copilot_tools = set(
			copilot_config["mcpServers"]["roslyn-analyzers-dev"]["tools"]
		)
		self.assertEqual(expected, copilot_tools)


if __name__ == "__main__":
	unittest.main()
