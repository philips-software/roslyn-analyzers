#!/usr/bin/env python3
"""Regression tests for AI-agent configuration parity."""

from pathlib import Path
import tempfile
import unittest

import ai_config


class AiConfigTests(unittest.TestCase):
	def setUp(self) -> None:
		self.temp_directory = tempfile.TemporaryDirectory()
		self.root = Path(self.temp_directory.name)
		(self.root / ".claude/skills/demo").mkdir(parents=True)
		(self.root / ".codex").mkdir()
		(self.root / "CLAUDE.md").write_text(
			"# CLAUDE.md\n\nAuthoritative instructions.\n", encoding="utf-8"
		)
		(self.root / ".claude/skills/demo/SKILL.md").write_text(
			"---\nname: demo\ndescription: Demonstrate parity checks.\n---\n\nCanonical workflow.\n",
			encoding="utf-8",
		)
		(self.root / ".codex/config.toml").write_text(
			'project_doc_fallback_filenames = ["CLAUDE.md"]\n', encoding="utf-8"
		)
		self.assertEqual([], ai_config.regenerate(self.root))

	def tearDown(self) -> None:
		self.temp_directory.cleanup()

	def test_valid_configuration_passes(self) -> None:
		self.assertEqual([], ai_config.validate(self.root))

	def test_commented_codex_value_does_not_mask_wrong_effective_value(self) -> None:
		(self.root / ".codex/config.toml").write_text(
			'# project_doc_fallback_filenames = ["CLAUDE.md"]\n'
			'project_doc_fallback_filenames = ["AGENTS.md"]\n',
			encoding="utf-8",
		)
		errors = ai_config.validate(self.root)
		self.assertTrue(any("found ['AGENTS.md']" in error for error in errors), errors)

	def test_invalid_toml_fails(self) -> None:
		(self.root / ".codex/config.toml").write_text(
			'project_doc_fallback_filenames = ["CLAUDE.md"\n', encoding="utf-8"
		)
		errors = ai_config.validate(self.root)
		self.assertTrue(any("not valid TOML" in error for error in errors), errors)

	def test_canonical_path_in_comment_does_not_mask_wrong_shim(self) -> None:
		(self.root / ".agents/skills/demo/SKILL.md").write_text(
			"---\nname: demo\ndescription: Demonstrate parity checks.\n---\n\n"
			"# ../../../.claude/skills/demo/SKILL.md\n"
			"Read and follow `../../../WRONG.md`.\n",
			encoding="utf-8",
		)
		errors = ai_config.validate(self.root)
		self.assertTrue(any("DRIFT: .agents/skills/demo/SKILL.md" in error for error in errors), errors)

	def test_shim_frontmatter_drift_fails(self) -> None:
		shim = self.root / ".agents/skills/demo/SKILL.md"
		shim.write_text(
			shim.read_text(encoding="utf-8").replace(
				"description: Demonstrate parity checks.", "description: Different trigger."
			),
			encoding="utf-8",
		)
		errors = ai_config.validate(self.root)
		self.assertTrue(any("DRIFT: .agents/skills/demo/SKILL.md" in error for error in errors), errors)

	def test_copilot_drift_fails(self) -> None:
		(self.root / ".github/copilot-instructions.md").write_text(
			"stale instructions\n", encoding="utf-8"
		)
		errors = ai_config.validate(self.root)
		self.assertTrue(
			any("DRIFT: .github/copilot-instructions.md" in error for error in errors), errors
		)


if __name__ == "__main__":
	unittest.main()
