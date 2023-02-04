# Background

Duplicate code detection has gained popularity in recent years. Our organization typically uses [copy-paste-detector](https://github.com/pmd/pmd) (CPD) as a post-build step. Some limitations of that tool include:
* Build failures that are not easily detected prior to check-in are frustrating for developers.
* As a multi-language support tool (and primarily Java), changes in C# confuse the tool.  One example is the simplified using statement syntax introduced in C# 8.

This is a good candidate for a Diagnostic Analyzer, because immediate feedback is provided, and it's tailored to C#. The implementation uses the Rabin-Karp string-searching algorithm and a polynomial rolling hash. The syntax tree is provided by Roslyn.

Unlike CPD, the algorithm intentionally ignores duplicates that span methods.

The immediate developer feedback is a double-edged sword.  If the culture of the team does not appreciate and respect the dangers of duplicate code, they could abuse the feedback by making uninteresting edits such that they are no longer detected as duplicates - all prior to check-in.  These situations will likely go unnoticed until a more advanced tool exposes the behavior.

# Getting Started

The default token count is 100.  This setting can be controlled by the .editorconfig file, with an entry such as:
`dotnet_code_quality.PH2071.token_count=50`

Enabling this rule on a legacy code base can yield many hits - too many to be able to enable the analyzer as an error.  To effectuate a 'stop the bleeding' policy, leave the rule's severity at error, and either use suppressions, or specify a whitelist as follows: add a file to the project named `DuplicateCode.Allowed.txt`.  The file must be specified with the `AdditionalFiles` node in the .csproj file.  Include a single method name per line.  Use the provided Code Fixer to automatically add violations to the whitelist.

# White listing

For certain parts of the code, duplication can be allowed. For example for runtime performance reasons, one might repeat code explicitly. For use-cases like these, there is the Ability to provide an "AdditionalFile" to your project. This additional file needs to be named exactly "DuplicateCode.Allowed.txt". The syntax of this file is very similar to that of Microsofts (BannedApiAnalyzer)[https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md].

# Strings

Duplicate string literals are checked by rule PH2136. To prevent duplicate string literals, define an 'private const string' at the top of your class declaration.
