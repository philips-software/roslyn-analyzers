# Copilot Instructions

This document outlines known pitfalls and requirements when contributing to the Philips Roslyn Analyzers project using GitHub Copilot or other AI-powered development tools.

## Code Formatting Requirements

All code must strictly follow the formatting rules defined in the [.editorconfig](./.editorconfig) file. Key requirements include:

* **Indentation**: Use tabs with size 4
* **Line endings**: CRLF (Windows-style)
* **Encoding**: UTF-8 with BOM for C# files
* **Braces**: New line before all braces (`csharp_new_line_before_open_brace = all`)
* **Spacing**: Specific spacing rules around operators, keywords, and punctuation
* **Naming**: Parameters must be camelCase

Ensure your code follows these rules before submitting. The build will fail if formatting requirements are not met.

## Dogfooding Requirements

The build must pass with all analyzers enabled. This is verified by the [dogfood workflow](./.github/workflows/dogfood.yml), which:

1. Builds the analyzers as NuGet packages
2. Adds them as package references to the project itself
3. Rebuilds the entire codebase with all analyzers active

**Important**: If the dogfood build fails, your changes will be rejected. The analyzers must be able to analyze their own codebase without issues.

## Fixing Analyzer Findings

When analyzer warnings or errors are reported:

* **Do NOT** disable rules using `#pragma warning disable` or `.editorconfig` suppressions
* **Do NOT** add `[SuppressMessage]` attributes to avoid warnings
* **DO** fix the underlying code issues that triggered the findings
* **DO** improve the code quality to address the analyzer's concerns

The goal is to maintain high code quality, not to silence the tools that help achieve it.

## Pull Request Title Requirements

All pull request titles must follow [Conventional Commits](https://www.conventionalcommits.org/) format to indicate the type of release. Examples:

* `feat: Add new analyzer for detecting unused variables`
* `fix: Correct false positive in PH2147 analyzer`
* `docs: Update analyzer documentation`
* `refactor: Simplify analyzer registration logic`
* `test: Add coverage for edge case scenarios`
* `chore: Miscellaneous cleanup not changing behavior`
* `ci: Change to pipeline`

**Invalid example**: `"Fix PH2147: Create new analyzer to avoid variables named exactly "_""`
**Valid example**: `"fix: Create new analyzer PH2147 to avoid variables named exactly "_""`

## Performance Considerations

Roslyn analyzers run during compilation and must be performant. Follow these guidelines:

* **String comparisons first**: Always perform inexpensive string comparisons of identifiers before loading the semantic model
* **Cache expensive operations**: Store results of costly computations when possible
* **Early returns**: Exit analysis methods as soon as you determine no issues exist
* **Avoid regex in hot paths**: Use string methods over regular expressions for simple patterns

Example:
```csharp
// Good: Check name first
if (variableDeclarator.Identifier.ValueText != "_")
    return;

var semanticModel = context.SemanticModel; // Only load if needed

// Bad: Load semantic model for every variable
var semanticModel = context.SemanticModel;
var symbol = semanticModel.GetDeclaredSymbol(variableDeclarator);
if (symbol.Name != "_")
    return;
```

## Documentation Requirements

All analyzers must have corresponding documentation files in the [Documentation](./Documentation/) folder:

* Create or update relevant documentation files (e.g., `Maintainability.md`)
* Include rule descriptions, examples of violations, and correct usage
* Follow the existing documentation format and style
* Link to documentation from the main README.md if adding new analyzer categories

Refer to existing documentation files for format and style guidelines.

## Creating New Analyzers

When creating a new analyzer, follow this workflow to ensure proper integration and testing:

### Diagnostic ID Assignment
* **Choose the next available diagnostic ID**: Review the [DiagnosticId.cs](./Philips.CodeAnalysis.Common/DiagnosticId.cs) file to find the highest existing ID and increment by 1
* **Check open PRs manually**: Consider other open pull requests to avoid multiple PRs claiming the same diagnostic ID (Note: AI tools cannot automatically check open PRs, so manual verification is required)
* **Current highest ID**: As of this writing, the highest ID is `2145`, so new analyzers should start from `2146`

### Initial Configuration
* **Set "Enabled By Default" to No**: Always set `isEnabledByDefault: false` in the `DiagnosticDescriptor` constructor
* **Rationale**: This allows manual testing in production environments before enabling the analyzer by default
* **Copyright header**: When creating new files, always use the current year in the copyright header: `// © YYYY Koninklijke Philips N.V. See License.md in the project root for license information.`

Example:
```csharp
public static readonly DiagnosticDescriptor Rule = new(
    DiagnosticId.YourNewRule.ToId(),
    Title, MessageFormat, Category,
    DiagnosticSeverity.Warning, isEnabledByDefault: false, description: Description);
```

### CodeFixer Workflow
* **Initial submission**: A CodeFixer need not be created initially with the analyzer
* **Subsequent development**: If the analyzer is tested successfully, a subsequent issue will be filed to create a CodeFixer
* **Final enablement**: Only after both the analyzer and CodeFixer are tested successfully may "Enabled by Default" be set to `true` in another issue

This phased approach ensures thorough testing and validation before full deployment.

## Additional Guidelines

* **Test coverage**: Include comprehensive tests for new analyzers and bug fixes
* **Error messages**: Provide clear, actionable error messages that help developers understand and fix issues
* **Rule IDs**: Follow the existing numbering scheme for new analyzer rules
* **Backwards compatibility**: Ensure changes don't break existing analyzer behavior unless intentionally fixing bugs

Following these guidelines will help ensure your contributions integrate smoothely with the existing codebase and maintain the high quality standards of the project.
