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

## Additional Guidelines

* **Test coverage**: Include comprehensive tests for new analyzers and bug fixes
* **Error messages**: Provide clear, actionable error messages that help developers understand and fix issues
* **Rule IDs**: Follow the existing numbering scheme for new analyzer rules
* **Backwards compatibility**: Ensure changes don't break existing analyzer behavior unless intentionally fixing bugs

Following these guidelines will help ensure your contributions integrate smoothly with the existing codebase and maintain the high quality standards of the project.
