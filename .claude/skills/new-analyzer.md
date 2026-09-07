---
name: new-analyzer
description: Create a new Roslyn diagnostic analyzer with tests and documentation. Evaluates whether a code fixer should also be created.
---

# Create a New Analyzer

Follow these steps to create a new Roslyn diagnostic analyzer. Gather requirements first, then generate all files.

## 1. Check for Existing Rules

Before implementing, check whether the rule already exists in an analyzer set already in use across Philips repos:

- **CS / IDE / CA** — built-in SDK rules (`AnalysisLevel=latest-Recommended` is set in `Directory.Build.Common.props`). Search [learn.microsoft.com/dotnet/fundamentals/code-analysis/rule-categories](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/rule-categories) by keyword.
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

## 2. Assign a DiagnosticId

Read `Philips.CodeAnalysis.Common/DiagnosticId.cs` and find the highest numeric ID in the enum. Add the new entry with the next available number. The enum member name should be PascalCase describing the rule (e.g., `AvoidThreadSleep = 2020`).

## 3. Choose the Analyzer Pattern

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

## 4. Create the Analyzer

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
			// Analysis logic here
			// Use Helper.ForAttributes, Helper.ForNamespaces, etc.
			// Call ReportDiagnostic(location) to report violations
		}
	}
}
```

For Pattern B, override `InitializeCompilation` directly in the analyzer class.

## 5. Evaluate Code Fixer Opportunity

Before proceeding, evaluate whether a code fixer is appropriate. A code fixer IS warranted when:
- The fix is mechanical and deterministic (e.g., remove a node, rename, add a modifier)
- The fix preserves semantics

A code fixer is NOT warranted when:
- The fix requires human judgment about design
- Multiple valid fixes exist with different trade-offs
- The fix would require understanding broader context beyond the syntax tree

If a fixer is warranted, inform the user and ask if they'd like to create it now. If yes, use the `new-code-fixer` skill.

## 6. Create Tests

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
			await VerifyDiagnostic(givenText).ConfigureAwait(false);
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

## 7. Create Documentation

Create `Documentation/Diagnostics/PH<id>.md`:

```markdown
# PH<id>: <Title>

| Property | Value  |
|--|--|
| Package | [Philips.CodeAnalysis.<Project>](https://www.nuget.org/packages/Philips.CodeAnalysis.<Project>) |
| Diagnostic ID | PH<id> |
| Category  | [<Category>](../<Category>.md) |
| Analyzer | [<Name>Analyzer](https://github.com/philips-software/roslyn-analyzers/blob/main/Philips.CodeAnalysis.<Project>/<Category>/<Name>Analyzer.cs) |
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

## 8. Fix Formatting

New files will not have correct CRLF line endings. Run `dotnet format` on all new files before building:
```bash
dotnet format style --no-restore --include <space-separated list of new file paths>
```

Re-run this after any subsequent edits to those files — the Edit tool writes LF, not CRLF.

## 9. Validate

Run the full validation:
```bash
dotnet build --configuration Release
dotnet test --configuration Release --filter "FullyQualifiedName~<Name>Test"
dotnet format style --verify-no-changes --no-restore --verbosity detailed
```

Then run the full test suite to check for regressions.

### Completeness checklist

Before reporting done, verify every artifact exists:
- [ ] DiagnosticId enum entry in `Common/DiagnosticId.cs`
- [ ] Analyzer class in the correct project/subfolder
- [ ] Test class in `Philips.CodeAnalysis.Test/<matching subfolder>/`
- [ ] Documentation file `Documentation/Diagnostics/PH<id>.md`
- [ ] If a code fixer was created, documentation table shows `CodeFix | Yes`
