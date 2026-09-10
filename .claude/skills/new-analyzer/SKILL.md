---
name: new-analyzer
description: Create a new Roslyn diagnostic analyzer with tests and documentation. Evaluates whether a code fixer should also be created.
---

# Create a New Analyzer

Follow these steps to create a new Roslyn diagnostic analyzer. Gather requirements first, then generate all files.

## 1. Check for Existing Rules

Before implementing, check whether the rule already exists in an analyzer set already in use across Philips repos:

- **CS / IDE / CA** — built-in SDK rules (`AnalysisLevel=latest-Recommended` is set in `Directory.Build.Common.props`). Search the Microsoft docs for "dotnet code analysis rule categories" by keyword.
- **BannedApiAnalyzers** (`Microsoft.CodeAnalysis.BannedApiAnalyzers`) — already in use across repos. If the rule is "disallow use of specific API X", a `BannedSymbols.txt` entry may suffice instead of a custom analyzer.

If an equivalent rule exists, report it to the user rather than implementing a new one. Only proceed if no suitable rule covers the scenario.

## 2. Gather Requirements

Ask the user (if not already provided):
- What should the analyzer detect? (the rule)
- Which category does it belong to? (Maintainability, Documentation, Naming, Readability, RuntimeFailure, Security, FunctionalProgramming, MsTest)
- Which analyzer project? (MaintainabilityAnalyzers, DuplicateCodeAnalyzer, MoqAnalyzers, MsTestAnalyzers, SecurityAnalyzers)
- Should it be enabled by default? (default: no for new analyzers)
- How strict should the rule be? If the detection has a spectrum (e.g., "any violation" vs "only severe cases"), clarify the threshold before implementing. Make sure the diagnostic message matches the actual strictness.
- Is there a natural inverse rule? (e.g., "avoid X" vs "enforce X") If so, consider creating both as mutually exclusive analyzers with separate IDs. Document the mutual exclusivity in both PH docs.

## 3. Assign a DiagnosticId

Use the `next_diagnosticId` MCP tool to allocate the next available ID. It examines main and all open PRs to avoid conflicts. Add the new entry to the `DiagnosticId` enum with the returned number. The enum member name should be PascalCase describing the rule (e.g., `AvoidThreadSleep = 2020`).

If the MCP tool is unavailable, read `Philips.CodeAnalysis.Common/DiagnosticId.cs`, find the highest numeric ID, and use the next number — but verify it is not already claimed by another in-progress branch before committing.

## 4. Choose the Analyzer Pattern

First check if the target project has its own base class hierarchy. **MsTestAnalyzers** has specialized bases that handle test attribute resolution via the semantic model — use these instead of `SingleDiagnosticAnalyzer`:
- `TestMethodDiagnosticAnalyzer` — for rules that apply to test methods (resolves `[TestMethod]`, `[DataTestMethod]`, `[STATestMethod]` and derived attributes)
- `TestClassDiagnosticAnalyzer` — for rules that apply to test classes
- `TestAttributeDiagnosticAnalyzer` — the common base for both

These require a nested `Implementation` class (see `TestMethodsMustBePublicAnalyzer` for the pattern). The analyzer defines its own `Rule`/`SupportedDiagnostics` rather than inheriting from `SingleDiagnosticAnalyzer`.

For all other projects, there are two patterns. Pick the right one:

**Pattern A — `SingleDiagnosticAnalyzer<TNode, TSyntaxNodeAction>` (preferred for single syntax kind)**
Use when the analyzer targets a single `SyntaxKind`. The generic base auto-maps `TNode` to a `SyntaxKind`. Create a companion `SyntaxNodeAction<T>` subclass in the same file.

**Pattern B — `SingleDiagnosticAnalyzer` with `InitializeCompilation` override**
Use when the analyzer needs to register for multiple `SyntaxKind`s or uses non-SyntaxNode actions (e.g., symbol actions, operation actions). Override `InitializeCompilation` directly.

## 5. Create the Analyzer

Place in the appropriate project and subfolder matching the category:
- `Philips.CodeAnalysis.<Project>/<Category>/<AnalyzerName>Analyzer.cs`

For Pattern A, the file contains both the analyzer and its SyntaxNodeAction:

```csharp
// © <current_year> Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.<Project>.<Category>
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class <Name>Analyzer : SingleDiagnosticAnalyzer<<TNode>, <Name>SyntaxNodeAction>
	{
		private const string Title = @"<short title>";
		public const string MessageFormat = @"<message with optional {0} placeholders>";
		private const string Description = @"<longer description>";

		public <Name>Analyzer()
			: base(DiagnosticId.<EnumMember>, Title, MessageFormat, Description, Categories.<Category>, isEnabled: false)
		{ }
	}

	public class <Name>SyntaxNodeAction : SyntaxNodeAction<<TNode>>
	{
		public override void Analyze()
		{
			// Analysis logic here.
			// Use the search_helpers MCP tool to discover available Helper.For* methods.
			// Call ReportDiagnostic(location) to report violations.
		}
	}
}
```

For Pattern B, override `InitializeCompilation` directly in the analyzer class.

## 6. Evaluate Code Fixer Opportunity

Before proceeding, evaluate whether a code fixer is appropriate. A code fixer IS warranted when:
- The fix is mechanical and deterministic (e.g., remove a node, rename, add a modifier)
- The fix preserves semantics

A code fixer is NOT warranted when:
- The fix requires human judgment about design
- Multiple valid fixes exist with different trade-offs
- The fix would require understanding broader context beyond the syntax tree

If a fixer is warranted, inform the user and ask if they'd like to create it now. If yes, use the `new-code-fixer` skill.

## 7. Create Tests

Place tests in `Philips.CodeAnalysis.Test/<matching subfolder>/` mirroring the analyzer's location.

Test class extends `DiagnosticVerifier` (no fixer) or `CodeFixVerifier` (with fixer):

```csharp
// © <current_year> Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.<Project>.<Category>;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.<TestSubfolder>
{
	[TestClass]
	public class <Name>Test : DiagnosticVerifier
	{
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task <DescriptiveViolationCase>()
		{
			var givenText = @"
// inline source code that triggers the diagnostic
// use {{ for literal braces if using string.Format
";
			// For SingleDiagnosticAnalyzer subclasses:
			await VerifyDiagnostic(givenText).ConfigureAwait(false);
			// For other hierarchies (e.g., TestMethodDiagnosticAnalyzer):
			// await VerifyDiagnostic(givenText, DiagnosticId.<EnumMember>).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task <DescriptivePassingCase>()
		{
			var givenText = @"
// inline source code that should NOT trigger the diagnostic
";
			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new <Name>Analyzer();
		}
	}
}
```

Write tests covering:
- At least one case that triggers the diagnostic
- At least one case that passes cleanly
- Edge cases relevant to the rule (generated code, nested classes, aliases, etc.)

## 8. Create Documentation

Create `Documentation/Diagnostics/PH<id>.md`:

```markdown
# PH<id>: <Title>

| Property | Value  |
|--|--|
| Package | [Philips.CodeAnalysis.<Project>](https://www.nuget.org/packages/Philips.CodeAnalysis.<Project>) |
| Diagnostic ID | PH<id> |
| Category  | [<Category>](../<Category>.md) |
| Analyzer | Link `<Name>Analyzer` to its source file in `Philips.CodeAnalysis.<Project>/<Category>/<Name>Analyzer.cs` |
| CodeFix  | Yes/No |
| Severity | Error |
| Enabled By Default | Yes/No |

## Introduction

<Why this rule exists>

## How to solve

<How to fix violations>

## Example

Code that triggers a diagnostic:
``` cs
<bad example>
```

And the replacement code:
``` cs
<good example>
```

## Configuration

This analyzer does not offer any special configuration. The general ways of [suppressing](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/suppress-warnings) diagnostics apply.
```

## 9. Fix Formatting

New files will not have correct CRLF line endings. Use the `fix_formatting` MCP tool to auto-fix all IDE0055 violations (CRLF, tabs, braces). Alternatively:
```bash
dotnet format style --no-restore --include <space-separated list of new file paths>
```

Re-run this after any subsequent edits to those files — the Edit tool writes LF, not CRLF.

## 10. Validate

Use the MCP tools to validate, or run the equivalent commands:
- `build_strict` — builds with warnings as errors
- `run_tests` — runs the full test suite

To run only the new tests first:
```bash
dotnet test --configuration Release --filter "FullyQualifiedName~<Name>Test"
```

Then run the full suite (via `run_tests` or `dotnet test --configuration Release`) to check for regressions.

## 11. Dogfooding and CI

Run `run_dogfood` to build the analyzers and apply them to this codebase before pushing. This mirrors the CI dogfooding pipeline. Your new analyzer (and any code you wrote) must pass:

- **Never disable an analyzer** — do not suppress, disable, or lower the severity of any rule in `.editorconfig`, `GlobalSuppressions.cs`, or any other mechanism. If the codebase triggers the new analyzer, fix the code.
- **SonarCloud** must pass — new code must meet the 80% coverage threshold. Use `analyze_coverage` to identify uncovered lines and get test suggestions before pushing.
- If the dogfooding build surfaces violations from your new analyzer in existing code, fix those violations rather than weakening the rule or disabling it.

### Completeness checklist

Before reporting done, verify every artifact exists:
- [ ] DiagnosticId enum entry in `Common/DiagnosticId.cs`
- [ ] Analyzer class in the correct project/subfolder
- [ ] Test class in `Philips.CodeAnalysis.Test/<matching subfolder>/`
- [ ] Documentation file `Documentation/Diagnostics/PH<id>.md`
- [ ] If a code fixer was created, documentation table shows `CodeFix | Yes`
