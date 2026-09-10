#!/usr/bin/env python3
"""Generate and validate AI-agent configuration derived from CLAUDE.md."""

from __future__ import annotations

import argparse
import difflib
from pathlib import Path
import sys
import tempfile
import tomllib


COPILOT_TITLE = "# Philips Roslyn Analyzers — AI Coding Instructions"
COPILOT_BANNER = "> AUTO-GENERATED from CLAUDE.md. Do not edit directly — update CLAUDE.md instead."
SKILL_GLOB = "*/SKILL.md"
MCP_SERVER = "roslyn-analyzers-dev"
MCP_COMMAND = "python"
MCP_ARGS = ["tools/mcp/mcp_server.py"]


def read_text(path: Path) -> str:
	return path.read_text(encoding="utf-8-sig")


def write_text(path: Path, content: str) -> None:
	if path.is_file() and read_text(path) == content:
		return
	path.parent.mkdir(parents=True, exist_ok=True)
	temporary_path: Path | None = None
	try:
		with tempfile.NamedTemporaryFile(
			"w",
			encoding="utf-8",
			newline="\n",
			dir=path.parent,
			prefix=f"{path.name}.",
			suffix=".tmp",
			delete=False,
		) as output:
			temporary_path = Path(output.name)
			output.write(content)
		temporary_path.replace(path)
	finally:
		if temporary_path is not None:
			temporary_path.unlink(missing_ok=True)


def generate_copilot_instructions(claude_content: str) -> str:
	lines = claude_content.splitlines()
	if not lines:
		raise ValueError("CLAUDE.md is empty")

	body = "\n".join(lines[1:])
	if body:
		body += "\n"
	return f"{COPILOT_TITLE}\n\n{COPILOT_BANNER}\n{body}"


def canonical_frontmatter(skill_path: Path) -> tuple[str, str, str]:
	lines = read_text(skill_path).splitlines()
	if not lines or lines[0] != "---":
		raise ValueError("frontmatter must start with '---'")

	try:
		end = lines.index("---", 1)
	except ValueError as error:
		raise ValueError("frontmatter is missing its closing '---'") from error

	frontmatter = lines[1:end]
	name_lines = [line for line in frontmatter if line.startswith("name:")]
	description_lines = [line for line in frontmatter if line.startswith("description:")]
	if len(name_lines) != 1 or len(description_lines) != 1:
		raise ValueError("frontmatter must contain one single-line name and description")

	name_line = name_lines[0]
	description_line = description_lines[0]
	name = name_line.split(":", 1)[1].strip().strip("\"'")
	description = description_line.split(":", 1)[1].strip()
	if not name or not description or description.startswith((">", "|")):
		raise ValueError("frontmatter name and description must be single-line values")
	if name != skill_path.parent.name:
		raise ValueError(
			f"frontmatter name '{name}' does not match directory '{skill_path.parent.name}'"
		)

	return name, name_line, description_line


def expected_skill_shim(skill_path: Path) -> tuple[str, str]:
	name, name_line, description_line = canonical_frontmatter(skill_path)
	content = (
		"---\n"
		f"{name_line}\n"
		f"{description_line}\n"
		"---\n\n"
		f"Read and follow `../../../.claude/skills/{name}/SKILL.md` as the authoritative workflow.\n"
		f"Resolve all relative paths and supporting resources from `../../../.claude/skills/{name}/`.\n"
	)
	return name, content


def expected_generated_files(root: Path) -> tuple[dict[Path, str], list[str]]:
	errors: list[str] = []
	outputs: dict[Path, str] = {}

	claude_path = root / "CLAUDE.md"
	if not claude_path.is_file():
		errors.append("MISSING: CLAUDE.md")
	else:
		try:
			outputs[root / ".github/copilot-instructions.md"] = generate_copilot_instructions(
				read_text(claude_path)
			)
		except ValueError as error:
			errors.append(f"INVALID: CLAUDE.md: {error}")

	canonical_skills = sorted((root / ".claude/skills").glob(SKILL_GLOB))
	for skill_path in canonical_skills:
		try:
			name, content = expected_skill_shim(skill_path)
		except ValueError as error:
			errors.append(f"INVALID: {skill_path.relative_to(root).as_posix()}: {error}")
			continue
		outputs[root / f".agents/skills/{name}/SKILL.md"] = content

	return outputs, errors


def orphaned_skill_shims(root: Path) -> list[Path]:
	canonical_names = {
		path.parent.name for path in (root / ".claude/skills").glob(SKILL_GLOB)
	}
	return sorted(
		path
		for path in (root / ".agents/skills").glob(SKILL_GLOB)
		if path.parent.name not in canonical_names
	)


def validate_mcp_json(root: Path) -> list[str]:
	mcp_path = root / ".mcp.json"
	if not mcp_path.is_file():
		return ["MISSING: .mcp.json"]

	import json
	try:
		config = json.loads(read_text(mcp_path))
	except json.JSONDecodeError as error:
		return [f"INVALID: .mcp.json is not valid JSON: {error}"]

	server = config.get(MCP_SERVER)
	if not isinstance(server, dict):
		return [f"INVALID: .mcp.json must contain a {MCP_SERVER!r} entry"]
	if server.get("command") != MCP_COMMAND:
		return [
			f"INVALID: .mcp.json MCP server command must be {MCP_COMMAND!r}; "
			f"found {server.get('command')!r}"
		]
	if server.get("args") != MCP_ARGS:
		return [
			f"INVALID: .mcp.json MCP server args must be {MCP_ARGS!r}; "
			f"found {server.get('args')!r}"
		]
	return []


def validate_codex_config(root: Path) -> list[str]:
	config_path = root / ".codex/config.toml"
	if not config_path.is_file():
		return ["MISSING: .codex/config.toml"]

	try:
		with config_path.open("rb") as config_file:
			config = tomllib.load(config_file)
	except tomllib.TOMLDecodeError as error:
		return [f"INVALID: .codex/config.toml is not valid TOML: {error}"]

	actual = config.get("project_doc_fallback_filenames")
	if actual != ["CLAUDE.md"]:
		return [
			"INVALID: .codex/config.toml must set "
			f"project_doc_fallback_filenames to exactly ['CLAUDE.md']; found {actual!r}"
		]

	mcp_servers = config.get("mcp_servers")
	mcp_server = mcp_servers.get(MCP_SERVER) if isinstance(mcp_servers, dict) else None
	if not isinstance(mcp_server, dict):
		return [
			"INVALID: .codex/config.toml must register "
			f"[mcp_servers.{MCP_SERVER}]"
		]
	if mcp_server.get("command") != MCP_COMMAND:
		return [
			f"INVALID: Codex MCP server command must be {MCP_COMMAND!r}; "
			f"found {mcp_server.get('command')!r}"
		]
	if mcp_server.get("args") != MCP_ARGS:
		return [
			f"INVALID: Codex MCP server args must be {MCP_ARGS!r}; "
			f"found {mcp_server.get('args')!r}"
		]
	return []


def validate_generated_files(root: Path) -> list[str]:
	expected_files, errors = expected_generated_files(root)
	for orphan in orphaned_skill_shims(root):
		errors.append(
			f"ORPHAN: {orphan.relative_to(root).as_posix()} has no matching canonical skill"
		)
	for path, expected in expected_files.items():
		relative_path = path.relative_to(root).as_posix()
		if not path.is_file():
			errors.append(f"MISSING: {relative_path}")
			continue

		actual = read_text(path)
		if actual != expected:
			diff = "".join(
				difflib.unified_diff(
					actual.splitlines(keepends=True),
					expected.splitlines(keepends=True),
					fromfile=str(relative_path),
					tofile=f"expected/{relative_path}",
				)
			)
			errors.append(
				f"DRIFT: {relative_path} does not match its authoritative Claude source\n{diff}"
			)
	return errors


def validate(root: Path) -> list[str]:
	return validate_mcp_json(root) + validate_codex_config(root) + validate_generated_files(root)


def regenerate(root: Path) -> list[str]:
	expected_files, errors = expected_generated_files(root)
	if errors:
		return errors
	for orphan in orphaned_skill_shims(root):
		try:
			orphan.unlink()
			try:
				orphan.parent.rmdir()
			except OSError:
				# Preserve non-generated supporting files in the adapter directory.
				pass
		except OSError as error:
			return [
				f"ERROR: could not remove {orphan.relative_to(root).as_posix()}: {error}"
			]
	for path, content in sorted(expected_files.items()):
		try:
			write_text(path, content)
		except OSError as error:
			return [f"ERROR: could not write {path.relative_to(root).as_posix()}: {error}"]
	return validate(root)


def main() -> int:
	parser = argparse.ArgumentParser()
	mode = parser.add_mutually_exclusive_group(required=True)
	mode.add_argument("--check", action="store_true", help="validate generated files and adapters")
	mode.add_argument("--write", action="store_true", help="regenerate Copilot instructions and skill shims")
	parser.add_argument(
		"--root",
		type=Path,
		default=Path(__file__).resolve().parents[2],
		help="repository root (defaults to the root containing this script)",
	)
	args = parser.parse_args()

	errors = regenerate(args.root) if args.write else validate(args.root)
	if errors:
		for error in errors:
			print(error, file=sys.stderr)
		if args.check:
			print(
				"Regenerate derived files with: python .github/scripts/ai_config.py --write",
				file=sys.stderr,
			)
		return 1

	print("AI config regenerated and validated." if args.write else "AI config parity check passed.")
	return 0


if __name__ == "__main__":
	raise SystemExit(main())
