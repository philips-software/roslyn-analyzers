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
Note that in order for Analyzers to be able to process the .editorconfig, it must be included in the project as an `AdditionalFiles`.  E.g.,:
```
  <ItemGroup>
    <AdditionalFiles Include="..\.editorconfig" Link=".editorconfig" />
  </ItemGroup>
```
  
[More information](https://developercommunity.visualstudio.com/content/problem/791119/editorconfig-has-stopped-working.html) is available.  Analyzer [PH2072](../Philips.CodeAnalysis.MaintainabilityAnalyzers/Philips.CodeAnalysis.MaintainabilityAnalyzers.md) confirms that the .editorconfig file is visible to analyzers.

Enabling this rule on a legacy code base can yield many hits - too many to be able to enable the analyzer as an error.  To effectuate a 'stop the bleeding' policy, leave the rule's severity at error, and either use suppressions, or specify a whitelist as follows: add a file to the project named `DuplicateCode.Allowed.txt`.  The file must be specified with the `AdditionalFiles` node in the .csproj file.  Include a single method name per line.  Use the provided Code Fixer to automatically add violations to the whitelist.

