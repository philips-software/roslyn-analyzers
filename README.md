# Introduction 
Roslyn Diagnostic Analyzers are customized compiler errors providing real-time feedback to C# developers.  Each Analyzer optionally includes an automatic Code Fixer.  Microsoft (and other organizations) offer many analyzers, but the market is nascent.

We have a policy whereby Code Reviewers ask themselves if a review comment could be automated.  If so, and if a Diagnostic Analyzer is the right tool for the scenario in question, and an Analyzer does not readily exist in the market already, an Issue is created to track the need.  This project is the result.  It was open-sourced in 2020.

Consult the following for details on the available rules:

* [Philips.CodeAnalysis.DuplicateCodeAnalyzer]: .\Philips.CodeAnalysis.DuplicateCodeAnalyzer\Philips.CodeAnalysis.DuplicateCodeAnalyzer.md

* [Philips.CodeAnalysis.MaintainabilityAnalyzers]: .\Philips.CodeAnalysis.MaintainabilityAnalyzers\Philips.CodeAnalysis.MaintainabilityAnalyzers.md

* [Philips.CodeAnalysis.MoqAnalyzers]: .\Philips.CodeAnalysis.MoqAnalyzers\Philips.CodeAnalysis.MoqAnalyzers.md

* [Philips.CodeAnalysis.MsTestAnalyzers]: .\Philips.CodeAnalysis.MsTestAnalyzers\Philips.CodeAnalysis.MsTestAnalyzers.md

  

# Getting Started

Add the rules using Visual Studio's Package Manager, locating these packages on nuget.org.  Rules are generally enabled by default.  Use the .editorconfig file to enable/disable each of them and set their severity level.

Enabling a new rule on a legacy codebase can be daunting.  Some rules (e.g., Avoid Duplicate Code, Avoid Static Classes) support configuration and whitelisting - again via the .editorconfig.




