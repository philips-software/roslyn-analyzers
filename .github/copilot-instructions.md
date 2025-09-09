# Philips Roslyn Analyzers
The Philips Roslyn Analyzers repository contains customized Roslyn diagnostic analyzers for C# that provide real-time feedback to developers. This is a .NET 8.0 solution with multiple analyzer projects that compile to NuGet packages.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Prerequisites
- .NET 8.0 SDK is required and available
- Solution targets both .NET 8.0 and .NET Standard 2.0

### Core Build and Test Commands
Always run these commands in the repository root directory:

```bash
# Clean build artifacts (quick - ~1 second)
dotnet clean

# Restore dependencies (quick - ~1 second if already restored)
dotnet restore

# Build the entire solution -- takes ~1m 21s. NEVER CANCEL. Set timeout to 3+ minutes.
dotnet build --configuration Release

# Run the full test suite -- takes ~48 seconds, runs 1903 tests. NEVER CANCEL. Set timeout to 2+ minutes.
dotnet test --configuration Release --logger "trx;LogFileName=test-results.trx"

# Validate code formatting -- takes ~23 seconds. NEVER CANCEL. Set timeout to 1+ minutes.
dotnet format style --verify-no-changes --no-restore --verbosity detailed
```

### Package Creation
The build process automatically creates NuGet packages in the `./Packages/` directory:
- `Philips.CodeAnalysis.MaintainabilityAnalyzers.*.nupkg`
- `Philips.CodeAnalysis.DuplicateCodeAnalyzer.*.nupkg`  
- `Philips.CodeAnalysis.MoqAnalyzers.*.nupkg`
- `Philips.CodeAnalysis.MsTestAnalyzers.*.nupkg`
- `Philips.CodeAnalysis.SecurityAnalyzers.*.nupkg`

Each package includes both `.nupkg` and `.snupkg` (symbol) packages.

## Validation

### Complete Validation Workflow
Before submitting any changes, ALWAYS run this complete validation sequence:

```bash
# 1. Clean and build
dotnet clean
dotnet build --configuration Release  # NEVER CANCEL: ~1m 21s

# 2. Run tests  
dotnet test --configuration Release --logger "trx;LogFileName=test-results.trx"  # NEVER CANCEL: ~48s

# 3. Check formatting
dotnet format style --verify-no-changes --no-restore --verbosity detailed  # NEVER CANCEL: ~23s
```

Total validation time: ~2m 32s - NEVER CANCEL these commands.

### Dogfooding Process
This repository uses a "dogfooding" process where the analyzers analyze their own code:
- Build creates analyzer packages with `.Dogfood` suffix
- Analyzers are then applied to the codebase itself
- All analyzer violations must be fixed, not suppressed

### Mandatory CI Requirements
The CI process enforces these checks that will cause build failures:
- All tests must pass (1903 tests)
- Code formatting must be perfect (no deviations from .editorconfig)
- All analyzer warnings must be addressed (no suppressions allowed)
- Dogfood process must complete successfully

## Project Structure

### Key Projects
- **Philips.CodeAnalysis.Common** - Shared utilities and base classes
- **Philips.CodeAnalysis.MaintainabilityAnalyzers** - Code maintainability rules
- **Philips.CodeAnalysis.DuplicateCodeAnalyzer** - Duplicate code detection
- **Philips.CodeAnalysis.MoqAnalyzers** - Moq testing framework rules
- **Philips.CodeAnalysis.MsTestAnalyzers** - MSTest framework rules  
- **Philips.CodeAnalysis.SecurityAnalyzers** - Security-focused rules
- **Philips.CodeAnalysis.Test** - All unit tests (1903 tests)
- **Philips.CodeAnalysis.Benchmark** - Performance benchmarking
- **Philips.CodeAnalysis.AnalyzerPerformance** - Performance analysis tools

### Solution Structure
```
Philips.CodeAnalysis.sln - Main solution file
├── Philips.CodeAnalysis.Common/ - Shared utilities
├── Philips.CodeAnalysis.MaintainabilityAnalyzers/ - Core analyzers  
├── Philips.CodeAnalysis.DuplicateCodeAnalyzer/ - Duplicate detection
├── Philips.CodeAnalysis.MoqAnalyzers/ - Moq-specific rules
├── Philips.CodeAnalysis.MsTestAnalyzers/ - MSTest rules
├── Philips.CodeAnalysis.SecurityAnalyzers/ - Security rules
├── Philips.CodeAnalysis.Test/ - All unit tests
├── Philips.CodeAnalysis.Benchmark/ - Benchmarking
├── Philips.CodeAnalysis.AnalyzerPerformance/ - Performance tools
├── Documentation/ - Rule documentation by category
├── Packages/ - Generated NuGet packages (created during build)
└── .github/workflows/ - CI/CD pipelines
```

### Important Configuration Files
- **.editorconfig** - Strict formatting rules (311 lines) - all violations cause build failures
- **Directory.Build.Common.props** - Common MSBuild properties
- **Directory.Build.Analyzer.props** - Analyzer-specific build configuration
- **copilot-instructions.md** - Additional development guidelines (separate from this file)

## Development Guidelines

### ⚠️ CRITICAL: Code Formatting Requirements ⚠️
**The #1 cause of CoPilot Coding Agent struggles is formatting violations (IDE0055)**

All code MUST strictly follow these .editorconfig rules:
- **❗ Line endings**: CRLF (Windows-style) - NOT LF
- **❗ Indentation**: Tabs with size 4 - NOT spaces  
- **❗ Encoding**: UTF-8 with BOM for C# files
- **❗ Braces**: New line before all braces
- **❗ Naming**: Parameters must be camelCase

**FORMATTING IS ZERO-TOLERANCE**: Any violation fails the build (IDE0055.severity = error)

**Auto-fix tool available**: Use the MCP server `fix_formatting` tool to auto-correct all formatting issues instead of manual fixes.

### Creating New Analyzers
When creating new analyzers:

1. **Check DiagnosticId.cs** for the next available ID (current highest: 2145)
2. **Set `isEnabledByDefault: false`** initially for testing
3. **Add to appropriate project** (Maintainability, Security, etc.)
4. **Create comprehensive tests** in Philips.CodeAnalysis.Test
5. **Update documentation** in Documentation/ folder
6. **Use current year in copyright** headers: `// © 2025 Koninklijke Philips N.V.`

### Performance Considerations
Analyzers run during compilation and must be performant:
- **String comparisons first** before loading semantic models
- **Cache expensive operations** when possible  
- **Early returns** when no issues exist
- **Avoid regex in hot paths** - use string methods instead

### Pull Request Requirements
- **Title format**: Must follow Conventional Commits (feat:, fix:, docs:, etc.)
- **All tests pass**: 1903 tests must pass
- **Formatting perfect**: Zero formatting violations
- **No suppressions**: Fix underlying issues, don't suppress warnings
- **Documentation**: Update relevant docs in Documentation/ folder

## Common Tasks

### Running Individual Projects
```bash
# Build specific analyzer
dotnet build ./Philips.CodeAnalysis.MaintainabilityAnalyzers/Philips.CodeAnalysis.MaintainabilityAnalyzers.csproj --configuration Release

# Run specific tests
dotnet test ./Philips.CodeAnalysis.Test/Philips.CodeAnalysis.Test.csproj --configuration Release
```

### Working with Packages
```bash
# Check generated packages
ls -la ./Packages/

# Clean packages  
dotnet clean  # Removes packages automatically
```

### Documentation Locations
- **Rule documentation**: `./Documentation/` (by category)
- **Individual analyzer docs**: Each project has a `.md` file
- **CI/CD information**: `./cicd.md`
- **Contributing guidelines**: `./CONTRIBUTING.md`
- **Development tips**: `./copilot-instructions.md` (existing file with additional guidelines)

## Troubleshooting

### Troubleshooting

### ⚠️ Formatting Issues (IDE0055)
**Most common CoPilot failure**: Formatting violations that consume 25% of agent effort.

**SOLUTION**: Use the MCP server `fix_formatting` tool:
1. Automatically corrects CRLF line endings (not LF)
2. Fixes indentation to tabs (not spaces)
3. Applies all .editorconfig rules
4. Ensures CI compliance

**Manual check**: `dotnet format style --verify-no-changes --no-restore --verbosity detailed`

### Build Failures
If builds fail:
1. Check formatting first: `dotnet format style --verify-no-changes --no-restore --verbosity detailed`
2. Run clean build: `dotnet clean && dotnet build --configuration Release`
3. Review analyzer violations - fix the code, don't suppress warnings

### Test Failures  
If tests fail:
1. Run tests with detailed output: `dotnet test --configuration Release --verbosity detailed`
2. Check if new code introduced failures
3. All 1903 tests must pass for CI to succeed

### Performance Issues
If analyzers are slow:
1. Use the performance analysis tools in `Philips.CodeAnalysis.AnalyzerPerformance`
2. Review the performance workflow in `.github/workflows/performance.yml`
3. Follow performance guidelines for analyzer development

## Critical Reminders
- **NEVER CANCEL** build, test, or format commands - they must complete
- **NEVER SUPPRESS** analyzer warnings - fix the underlying code issues  
- **ALWAYS VALIDATE** using the complete workflow before submitting changes
- **TIMEOUT VALUES**: Build (3+ min), Test (2+ min), Format (1+ min)
- **1903 TESTS** must all pass for successful CI
- **ZERO TOLERANCE** for formatting violations