# Introduction
Roslyn Diagnostic Analyzers are customized compiler errors providing real-time feedback to C# developers. Many Analyzers include an automatic Code Fixer. While Microsoft (and other organizations) offer many analyzers, the market is nascent. Moreover, many tools offer a rich set of rules, but lack the shift-left integration that Roslyn achieves.

We have a policy whereby Code Reviewers ask themselves if a review comment can be automated.  If so, and if a Diagnostic Analyzer is the right tool for the scenario in question, and if an Analyzer does not readily exist in the market already, an issue is created to track the need. That is, all analyzers herein are based on real-world code review feedback. This project is the result. It was open-sourced in 2020.

## Rules Documentation
Consult the following for details on the available rules:

* [Philips.CodeAnalysis.DuplicateCodeAnalyzer](./Philips.CodeAnalysis.DuplicateCodeAnalyzer/Philips.CodeAnalysis.DuplicateCodeAnalyzer.md)

* [Philips.CodeAnalysis.MaintainabilityAnalyzers](./Philips.CodeAnalysis.MaintainabilityAnalyzers/Philips.CodeAnalysis.MaintainabilityAnalyzers.md)

* [Philips.CodeAnalysis.MoqAnalyzers](./Philips.CodeAnalysis.MoqAnalyzers/Philips.CodeAnalysis.MoqAnalyzers.md)

* [Philips.CodeAnalysis.MsTestAnalyzers](./Philips.CodeAnalysis.MsTestAnalyzers/Philips.CodeAnalysis.MsTestAnalyzers.md) ⚠️ **[Microsoft now has official MSTest analyzers](./Documentation/MsTest.md)**

* [Philips.CodeAnalysis.SecurityAnalyzers](./Philips.CodeAnalysis.SecurityAnalyzers/Philips.CodeAnalysis.SecurityAnalyzers.md)


## Getting Started

Add the rules using Visual Studio's Package Manager, locating these packages on nuget.org.  Rules are generally enabled by default.  Use the .editorconfig file to enable or disable each of them and set their severity level as desired.
Some rules (e.g., Avoid Duplicate Code, Avoid Static Classes) support configuration and whitelisting - again via the .editorconfig.

## Debugging

Published packages include .snupkg and SourceLink. This allows symbol loading, stack traces with line numbers on exceptions, and "Go to Definition" support. However, breakpoints and step-through debugging are flaky on published Release builds (especially with Analyzers in general), due to optimizations and inlining. 

For symbols and stack traces, launch a second instance of Visual Studio, attach the debugger to your primary development instance, and Load Symbols via Debug -> Windows -> Modules, and right-clicking on the Analyzer.

## Visual Studio 2019/2022 Support

These packages reference Microsoft.CodeAnalysis version 3.6, which shipped with Visual Studio 2019 16.6.

## MCP Server for Development

A Model Context Protocol (MCP) server is available to automate common development tasks such as dogfooding builds, strict building, file navigation, and test execution. See [tools/mcp/MCP_SERVER.md](./tools/mcp/MCP_SERVER.md) for details.

Quick start:
```bash
./tools/mcp/start_mcp_server.sh
```

## CI/CD
[Learn more](./cicd.md) about the CI/CD pipeline.
