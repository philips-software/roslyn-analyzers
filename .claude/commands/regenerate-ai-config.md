Regenerate `.github/copilot-instructions.md` from `CLAUDE.md` by running this bash command from the repo root:

```bash
{ echo "# Philips Roslyn Analyzers — AI Coding Instructions"; echo ""; echo "> AUTO-GENERATED from CLAUDE.md. Do not edit directly — update CLAUDE.md instead."; tail -n +2 CLAUDE.md; } > .github/copilot-instructions.md
```

Then stage the result with `git add .github/copilot-instructions.md`.
