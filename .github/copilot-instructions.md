# Philips Roslyn Analyzers — AI Coding Instructions

> AUTO-GENERATED from CLAUDE.md. Do not edit directly — update CLAUDE.md instead.

This file provides authoritative guidance to AI coding agents working with code in this repository.

## Overview

Philips Roslyn Analyzers — custom Roslyn diagnostic analyzers for C# shipped as NuGet packages. The analyzers provide real-time compiler feedback and many include automatic code fixers. Open-sourced by Philips in 2020; all rules originate from real code review feedback.

## Build and Test Commands

Run from the repository root. All commands use `--configuration Release`.

```bash
# Build — generates NuGet packages in ./Packages/
dotnet build --configuration Release

# Run all tests (MSTest)
dotnet test --configuration Release --logger "trx;LogFileName=test-results.trx"

# Run a single test class
dotnet test --configuration Release --filter "FullyQualifiedName~AvoidThreadSleepTest"

# Run a single test method
dotnet test --configuration Release --filter "FullyQualifiedName~AvoidThreadSleepTest.BehindAlias"

# Verify code formatting
dotnet format style --verify-no-changes --no-restore --verbosity detailed

# Fix formatting violations
dotnet format style --no-restore
```

## Formatting Rules (zero tolerance — IDE0055 is severity error)

- Line endings: CRLF
- Indentation: tabs, size 4
- Encoding: UTF-8 with BOM for .cs files
- Braces: Allman style (new line before all braces)
- Parameters: camelCase
- See `.editorconfig` for the full set; `TreatWarningsAsErrors` is enabled in `Directory.Build.Common.props`

## Architecture

### Analyzer Class Hierarchy

All analyzers flow through a common base in `Philips.CodeAnalysis.Common`:

```
DiagnosticAnalyzer (Roslyn)
  └─ DiagnosticAnalyzerBase — sealed Initialize(), enables concurrent execution,
  │    creates Helper on CompilationStart, delegates to InitializeCompilation()
  │  └─ SingleDiagnosticAnalyzer — one DiagnosticId + one Rule
  │    └─ SingleDiagnosticAnalyzer<TNode, TSyntaxNodeAction> — auto-registers
  │         SyntaxNodeAction for the SyntaxKind inferred from TNode; handles
  │         generated-code filtering; instantiates TSyntaxNodeAction per node
  └─ SolutionAnalyzer — operates on the full Compilation (opt-in by default)
```

**Most analyzers** inherit `SingleDiagnosticAnalyzer<TNode, TSyntaxNodeAction>` and pair with a `SyntaxNodeAction<T>` subclass that implements `Analyze()`. The generic base auto-maps `TNode` to a `SyntaxKind` — override `GetSyntaxKind()` only if the default mapping doesn't fit.

### SyntaxNodeAction Pattern

The analysis logic lives in a `SyntaxNodeAction<T>` subclass (not in the analyzer). It receives `Context`, `Node`, `Rule`, `Helper`, and calls `ReportDiagnostic(location)`.

### Code Fix Hierarchy

```
CodeFixProvider (Roslyn)
  └─ SingleDiagnosticCodeFixProvider<TSyntax> — one fixable ID, BatchFixer FixAll,
  │    override ApplyFix()
  └─ SolutionCodeFixProvider<TSyntax> — operates across the solution
```

Code fix providers must be annotated with `[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(...)), Shared]`.

### Helper System

`Helper` (inherits `CodeFixHelper`) is created per compilation and provides domain helpers accessed via properties:
- `ForAttributes` — attribute detection
- `ForNamespaces` — using/alias resolution
- `ForTypes`, `ForLiterals`, `ForModifiers`, `ForConstructors`, `ForTests`, `ForAssemblies`, `ForGeneratedCode`
- `ForAdditionalFiles` — reads .editorconfig settings and AdditionalFiles (exceptions lists)
- `ForAllowedSymbols` — allowlist support with wildcards
- `ForDocumentationOf(node)` — XML doc helpers

### DiagnosticId Enum

All diagnostic IDs live in `Philips.CodeAnalysis.Common/DiagnosticId.cs`. IDs use the `PH` prefix (e.g., `PH2160`). The enum's numeric value maps directly to the ID number.

### Project Layout

| Project | Purpose |
|---|---|
| `Common` | Base classes, helpers, DiagnosticId enum |
| `MaintainabilityAnalyzers` | Largest set — subcategories: Maintainability, Documentation, Naming, Readability, RuntimeFailure, Cardinality |
| `DuplicateCodeAnalyzer` | Duplicate code detection (uses Mono.Cecil) |
| `MoqAnalyzers` | Moq framework misuse detection |
| `MsTestAnalyzers` | MSTest framework rules |
| `SecurityAnalyzers` | Security rules (passwords, RSA padding, licensing) |
| `Test` | All unit tests (single project, mirrors analyzer structure) |
| `Benchmark` | BenchmarkDotNet performance tests |
| `AnalyzerPerformance` | Performance analysis tooling |

### Packaging and ILRepack

Each analyzer project targets `net8.0;netstandard2.0`. The `Directory.Build.Analyzer.props` configures ILRepack to merge `Common.dll` (and Mono.Cecil if present) into each analyzer DLL for the `netstandard2.0` target, so each NuGet package is self-contained. Packages output to `./Packages/`.

### Categories

Defined in `Common/Categories.cs`: Documentation, Maintainability, Naming, Readability, RuntimeFailure, Security, FunctionalProgramming, MsTest.

## Test Conventions

- Test framework: MSTest. All tests are in `Philips.CodeAnalysis.Test`.
- Test directory structure mirrors the analyzer project structure (e.g., `Test/Maintainability/Maintainability/`, `Test/Moq/`).
- Tests extend `DiagnosticVerifier` (analyzer-only) or `CodeFixVerifier` (analyzer + fixer).
- Key test methods: `VerifyDiagnostic(source)`, `VerifySuccessfulCompilation(source)`, `VerifyFix(oldSource, newSource)`.
- Use `VerifyDiagnostic(source, DiagnosticId.XXX)` when the analyzer doesn't extend `SingleDiagnosticAnalyzer` (e.g., `TestMethodDiagnosticAnalyzer` subclasses).
- Override `GetDiagnosticAnalyzer()` and optionally `GetCodeFixProvider()`.
- Every test method needs `[TestCategory(TestDefinitions.UnitTests)]`.
- Source code under test is provided as inline string literals with `{{` for brace escaping in `string.Format` patterns.
- `CodeFixVerifier` subclasses automatically get a `CheckFixAllProvider` test.
- Copyright header: `// © <year> Koninklijke Philips N.V. See License.md in the project root for license information.`

## Creating a New Analyzer

1. Add the next ID to the `DiagnosticId` enum in `Common/DiagnosticId.cs`.
2. Create the analyzer class inheriting `SingleDiagnosticAnalyzer<TNode, TSyntaxNodeAction>` and a companion `SyntaxNodeAction<T>` class — typically in the same file. Annotate with `[DiagnosticAnalyzer(LanguageNames.CSharp)]`.
3. Set `isEnabled: false` initially in the constructor for safe rollout.
4. Optionally create a `SingleDiagnosticCodeFixProvider<TSyntax>` subclass.
5. Write tests extending `DiagnosticVerifier` or `CodeFixVerifier` in the matching test subfolder.
6. Add documentation in `Documentation/Diagnostics/PH<id>.md`.

## Analyzer Performance

Analyzers run during every compilation and must be fast:
- Do string/syntax comparisons before accessing the semantic model — semantic operations are expensive
- Return early when the node clearly doesn't match
- Avoid regex in hot paths; use `string.Contains` / `string.StartsWith` instead
- Cache results of expensive operations within a compilation via `CompilationStartAnalysisContext`

## Maintaining AI Agent Config

When CLAUDE.md or a canonical skill changes, regenerate the Copilot instructions and skill wrappers before committing — the `aiconfigparity` CI check will fail if generated content, adapter metadata, or runtime configuration drifts:

```bash
python .github/scripts/ai_config.py --write
```

## MCP Tools

The `roslyn-analyzers-dev` MCP server (registered for Claude Code in `.claude/settings.json` and for Codex in `.codex/config.toml`) provides tools that automate common development tasks. Install its Python dependency with `python -m pip install --requirement tools/mcp/requirements.txt`. Use these tools instead of running the equivalent commands manually:

| Tool | Purpose |
|---|---|
| `next_diagnosticId` | Allocate the next DiagnosticId — scans main and all open PRs to avoid conflicts |
| `search_helpers` | Find `Helper.For*` methods and utilities in `Philips.CodeAnalysis.Common` |
| `build_strict` | Build with warnings as errors |
| `run_tests` | Run the test suite |
| `run_dogfood` | Build dogfood packages and apply analyzers to the codebase itself |
| `fix_formatting` | Auto-fix IDE0055 formatting violations (CRLF, tabs, braces) |
| `analyze_coverage` | Identify uncovered lines and suggest tests to reach 80% coverage |

## CI / Dogfooding

- CI runs build, test, and format checks. SonarCloud enforces 80% code coverage — new code must meet this threshold or the PR will fail.
- The dogfooding workflow is an actual CI pipeline (not just a local step): it builds the analyzers with a `.Dogfood` suffix and applies them to the codebase itself. All analyzer violations must be fixed, not suppressed. Use the `run_dogfood` MCP tool to run this locally before pushing.
- **Never disable an analyzer** — do not suppress, disable, or lower the severity of any analyzer rule in `.editorconfig`, `GlobalSuppressions.cs`, or any other mechanism. If the codebase triggers a new analyzer, fix the code.
- PR titles must follow Conventional Commits (e.g., `feat:`, `fix:`, `docs:`). Use `feat:` only for user-facing analyzer changes — it triggers a minor version bump on the NuGet packages.

## DiagnosticId Allocation

When multiple agents or branches are in flight simultaneously, they may independently pick the same next `DiagnosticId`. Use the `next_diagnosticId` MCP tool to allocate IDs — it examines main and all open PRs to find a conflict-free ID. If the MCP server is unavailable, verify the chosen ID is not already claimed by another in-progress branch before committing.
