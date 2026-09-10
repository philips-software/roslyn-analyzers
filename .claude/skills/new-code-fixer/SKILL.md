---
name: new-code-fixer
description: Create a new code fixer (CodeFixProvider) for an existing Roslyn diagnostic analyzer, with tests.
---

# Create a New Code Fixer

Follow these steps to add a CodeFixProvider to an existing analyzer.

## 1. Identify the Target Analyzer

Determine which analyzer this fixer applies to. Read the analyzer class to understand:
- The `DiagnosticId` it reports
- The `SyntaxNode` type at the diagnostic location
- What the fix should transform

## 2. Create the Code Fixer

Place the fixer in the same project and subfolder as the analyzer:
- `Philips.CodeAnalysis.<Project>/<Category>/<Name>CodeFixProvider.cs`

```csharp
// © <current_year> Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.<Project>.<Category>
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(<Name>CodeFixProvider)), Shared]
	public class <Name>CodeFixProvider : SingleDiagnosticCodeFixProvider<<TSyntax>>
	{
		protected override string Title => "<Human-readable action description>.";

		protected override DiagnosticId DiagnosticId => DiagnosticId.<EnumMember>;

		protected override async Task<Document> ApplyFix(Document document, <TSyntax> node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken);

			// Implement the fix. Common patterns:
			// - Remove a node: rootNode.RemoveNode(node, SyntaxRemoveOptions.KeepDirectives)
			// - Replace a node: rootNode.ReplaceNode(node, newNode)
			// - Insert a node: use SyntaxFactory to construct new syntax

			SyntaxNode newRoot = rootNode; // Replace with actual transformation
			return document.WithSyntaxRoot(newRoot);
		}
	}
}
```

### Choosing the right base class

- **`SingleDiagnosticCodeFixProvider<TSyntax>`** — fixes within a single document. This is the most common case. Uses `WellKnownFixAllProviders.BatchFixer` automatically.
- **`SolutionCodeFixProvider<TSyntax>`** — when the fix needs to modify multiple documents in the solution.

### Using diagnostic properties

If the fixer needs data computed by the analyzer, pass it via `ImmutableDictionary<string, string>` properties on the `Diagnostic.Create` call in the analyzer, then read from the `properties` parameter in `ApplyFix`.

## 3. Update Tests

Change the existing test class to extend `CodeFixVerifier` instead of `DiagnosticVerifier`, and add `GetCodeFixProvider()`:

```csharp
using Microsoft.CodeAnalysis.CodeFixes;

// Change base class from DiagnosticVerifier to CodeFixVerifier
public class <Name>Test : CodeFixVerifier
{
	// Add fix verification tests
	[TestMethod]
	[TestCategory(TestDefinitions.UnitTests)]
	public async Task <DescriptiveFixCase>()
	{
		var givenText = @"
// source code with violation
";
		var fixedText = @"
// expected source after fix is applied
";
		// For SingleDiagnosticAnalyzer subclasses:
		await VerifyDiagnostic(givenText).ConfigureAwait(false);
		// For other hierarchies (e.g., TestMethodDiagnosticAnalyzer):
		// await VerifyDiagnostic(givenText, DiagnosticId.<EnumMember>).ConfigureAwait(false);
		await VerifyFix(givenText, fixedText).ConfigureAwait(false);
	}

	// Existing GetDiagnosticAnalyzer() stays the same

	protected override CodeFixProvider GetCodeFixProvider()
	{
		return new <Name>CodeFixProvider();
	}
}
```

Key test methods:
- `VerifyFix(oldSource, newSource)` — applies the first code action and compares result
- `VerifyFix(oldSource, newSource, codeFixIndex: N)` — when multiple actions exist
- `VerifyFix(oldSource, newSource, shouldAllowNewCompilerDiagnostics: true)` — when the fix intentionally removes code that may leave unused usings
- `VerifyFixAll(oldSource, newSource)` — tests BatchFixer across Document, Project, and Solution scopes

Note: `CodeFixVerifier` automatically includes a `CheckFixAllProvider` test that validates the FixAllProvider is `BatchFixer`.

## 4. Update Documentation

Edit `Documentation/Diagnostics/PH<id>.md`:
- Change `CodeFix` row from `No` to `Yes`
- Add or update the "How to solve" section with the automatic fix description

## 5. Fix Formatting

New files will not have correct CRLF line endings. Use the `fix_formatting` MCP tool to auto-fix all IDE0055 violations (CRLF, tabs, braces). Alternatively:
```bash
dotnet format style --no-restore --include <space-separated list of new file paths>
```

Re-run this after any subsequent edits to those files — the Edit tool writes LF, not CRLF.

## 6. Validate

Use the MCP tools to validate, or run the equivalent commands:
- `build_strict` — builds with warnings as errors
- `run_tests` — runs the full test suite

To run only the relevant tests first:
```bash
dotnet test --configuration Release --filter "FullyQualifiedName~<Name>Test"
```

Then run the full suite (via `run_tests` or `dotnet test --configuration Release`) to check for regressions.

## 7. Dogfooding and CI

Run `run_dogfood` to build the analyzers and apply them to this codebase before pushing. This mirrors the CI dogfooding pipeline. Your new code fixer (and any code you wrote) must pass:

- **Never disable an analyzer** — do not suppress, disable, or lower the severity of any rule in `.editorconfig`, `GlobalSuppressions.cs`, or any other mechanism. If the codebase triggers a diagnostic, fix the code.
- **SonarCloud** must pass — new code must meet the 80% coverage threshold. Use `analyze_coverage` to identify uncovered lines and get test suggestions before pushing.
- If the dogfooding build surfaces violations in your code, fix those violations rather than weakening any rule.
