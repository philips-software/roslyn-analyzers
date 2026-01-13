# .NET 10.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that a .NET 10.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 10.0 upgrade.
3. Upgrade Philips.CodeAnalysis.Common\Philips.CodeAnalysis.Common.csproj
4. Upgrade Philips.CodeAnalysis.DuplicateCodeAnalyzer\Philips.CodeAnalysis.DuplicateCodeAnalyzer.csproj
5. Upgrade Philips.CodeAnalysis.SecurityAnalyzers\Philips.CodeAnalysis.SecurityAnalyzers.csproj
6. Upgrade Philips.CodeAnalysis.MaintainabilityAnalyzers\Philips.CodeAnalysis.MaintainabilityAnalyzers.csproj
7. Upgrade Philips.CodeAnalysis.MoqAnalyzers\Philips.CodeAnalysis.MoqAnalyzers.csproj
8. Upgrade Philips.CodeAnalysis.MsTestAnalyzers\Philips.CodeAnalysis.MsTestAnalyzers.csproj
9. Upgrade Philips.CodeAnalysis.AnalyzerPerformance\Philips.CodeAnalysis.AnalyzerPerformance.csproj
10. Upgrade Philips.CodeAnalysis.Benchmark\Philips.CodeAnalysis.Benchmark.csproj
11. Upgrade Philips.CodeAnalysis.Test\Philips.CodeAnalysis.Test.csproj

## Settings

This section contains settings and data used by execution steps.

### Project upgrade details

This section contains details about each project upgrade and modifications that need to be done in the project.

#### Philips.CodeAnalysis.Common\Philips.CodeAnalysis.Common.csproj modifications

Project properties changes:
  - Target frameworks should be changed from `net8.0;netstandard2.0` to `net8.0;netstandard2.0;net10.0`

#### Philips.CodeAnalysis.DuplicateCodeAnalyzer\Philips.CodeAnalysis.DuplicateCodeAnalyzer.csproj modifications

Project properties changes:
  - Target frameworks should be changed from `net8.0;netstandard2.0` to `net8.0;netstandard2.0;net10.0`

#### Philips.CodeAnalysis.SecurityAnalyzers\Philips.CodeAnalysis.SecurityAnalyzers.csproj modifications

Project properties changes:
  - Target frameworks should be changed from `net8.0;netstandard2.0` to `net8.0;netstandard2.0;net10.0`

#### Philips.CodeAnalysis.MaintainabilityAnalyzers\Philips.CodeAnalysis.MaintainabilityAnalyzers.csproj modifications

Project properties changes:
  - Target frameworks should be changed from `net8.0;netstandard2.0` to `net8.0;netstandard2.0;net10.0`

#### Philips.CodeAnalysis.MoqAnalyzers\Philips.CodeAnalysis.MoqAnalyzers.csproj modifications

Project properties changes:
  - Target frameworks should be changed from `net8.0;netstandard2.0` to `net8.0;netstandard2.0;net10.0`

#### Philips.CodeAnalysis.MsTestAnalyzers\Philips.CodeAnalysis.MsTestAnalyzers.csproj modifications

Project properties changes:
  - Target frameworks should be changed from `net8.0;netstandard2.0` to `net8.0;netstandard2.0;net10.0`

#### Philips.CodeAnalysis.AnalyzerPerformance\Philips.CodeAnalysis.AnalyzerPerformance.csproj modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net10.0`

#### Philips.CodeAnalysis.Benchmark\Philips.CodeAnalysis.Benchmark.csproj modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net10.0`

#### Philips.CodeAnalysis.Test\Philips.CodeAnalysis.Test.csproj modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net10.0`
