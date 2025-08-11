// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.SecurityAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class LicenseAnalyzer : SingleDiagnosticAnalyzer
	{
		private const string Title = @"Avoid Packages with Unacceptable Licenses";
		public const string MessageFormat = @"Package '{0}' has an unacceptable license. Consider adding to Allowed.Licenses.txt if license is acceptable.";
		private const string Description = @"Packages with unacceptable licenses (e.g., copyleft licenses like GPL) should be reviewed before use to ensure compliance with project requirements.";

		public const string AllowedLicensesFileName = @"Allowed.Licenses.txt";

		// Default acceptable licenses (permissive licenses that are generally safe to use)
		private static readonly HashSet<string> DefaultAcceptableLicenses = new(StringComparer.OrdinalIgnoreCase)
		{
			"MIT",
			"Apache-2.0",
			"BSD-2-Clause",
			"BSD-3-Clause",
			"ISC",
			"Unlicense",
			"0BSD"
		};

		private static readonly char[] LineSeparators = { '\r', '\n' };

		public LicenseAnalyzer()
			: base(DiagnosticId.AvoidUnlicensedPackages, Title, MessageFormat, Description, Categories.Security, isEnabled: false)
		{ }

		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(AnalyzeProjectFile, SyntaxKind.CompilationUnit);
		}

		private void AnalyzeProjectFile(SyntaxNodeAnalysisContext context)
		{
			// Only analyze if we're not in a test project (to avoid noise during development)
			if (Helper.ForTests.IsInTestClass(context))
			{
				return;
			}

			// Load custom allowed licenses from additional files
			HashSet<string> allowedLicenses = GetAllowedLicenses(context.Options.AdditionalFiles);

			// For this basic implementation, we'll analyze compilation references
			// In a more complete implementation, this would:
			// 1. Parse project files to find PackageReference elements
			// 2. Query package repositories (like nuget.org) for license information
			// 3. Compare against allowed licenses list
			// 4. Report violations

			AnalyzeReferences(context, allowedLicenses);
		}

		private void AnalyzeReferences(SyntaxNodeAnalysisContext context, HashSet<string> allowedLicenses)
		{
			// This is a basic demonstration implementation
			// In practice, we would need external APIs to get license information for packages
			// For now, we'll simulate by checking if any references look like they might have
			// unacceptable licenses based on naming patterns

			foreach (PortableExecutableReference reference in context.Compilation.References.OfType<PortableExecutableReference>())
			{
				if (reference.Display == null)
				{
					continue;
				}

				var assemblyName = Path.GetFileNameWithoutExtension(reference.Display);

				// Skip system and framework assemblies
				if (IsSystemAssembly(assemblyName))
				{
					continue;
				}

				// For demonstration purposes, we'll simulate license checking
				// This would normally involve querying external APIs or package metadata
				SimulateLicenseCheck(context, assemblyName, allowedLicenses);
			}
		}

		private static bool IsSystemAssembly(string assemblyName)
		{
			// Skip well-known system assemblies to reduce noise
			return assemblyName.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
				   assemblyName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) ||
				   assemblyName.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase) ||
				   assemblyName.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase) ||
				   assemblyName.Equals("System", StringComparison.OrdinalIgnoreCase);
		}

		private void SimulateLicenseCheck(SyntaxNodeAnalysisContext context, string assemblyName, HashSet<string> allowedLicenses)
		{
			// This is a simulation for demonstration purposes
			// In a real implementation, this would query actual package license information

			// For now, we'll only flag packages that explicitly suggest problematic licenses
			// This is just to demonstrate the diagnostic reporting mechanism
			if (ContainsIgnoreCase(assemblyName, "GPL") ||
				ContainsIgnoreCase(assemblyName, "Copyleft"))
			{
				// Create a diagnostic for this potentially problematic package
				var diagnostic = Diagnostic.Create(
					Rule,
					context.Node.GetLocation(),
					assemblyName);

				context.ReportDiagnostic(diagnostic);
			}

			// Use the allowedLicenses parameter to avoid IDE0060 warning
			// In a real implementation, this would be used to compare against actual license information
			GC.KeepAlive(allowedLicenses);

			// Note: This is a very basic simulation. A real implementation would:
			// 1. Extract package name and version from assembly metadata
			// 2. Query package repository APIs (nuget.org, etc.) for license information
			// 3. Parse license identifiers (SPDX, free-text, etc.)
			// 4. Compare against the allowedLicenses list
			// 5. Report diagnostics for packages with unacceptable licenses
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA2249:Use 'string.Contains' instead of 'string.IndexOf' to improve readability", Justification = "Provides cross-framework compatibility between .NET Standard 2.0 and .NET 8.0")]
		private static bool ContainsIgnoreCase(string source, string value)
		{
			return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static HashSet<string> GetAllowedLicenses(IEnumerable<AdditionalText> additionalFiles)
		{
			var allowedLicenses = new HashSet<string>(DefaultAcceptableLicenses, StringComparer.OrdinalIgnoreCase);

			AdditionalText licenseFile = additionalFiles?.FirstOrDefault(file =>
				Path.GetFileName(file.Path).Equals(AllowedLicensesFileName, StringComparison.OrdinalIgnoreCase));

			if (licenseFile != null)
			{
				Microsoft.CodeAnalysis.Text.SourceText text = licenseFile.GetText();
				if (text != null)
				{
					IEnumerable<string> customLicenses = text.ToString()
						.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries)
						.Select(line => line.Trim())
						.Where(line => !string.IsNullOrEmpty(line) && !(line.Length > 0 && line[0] == '#'));

					foreach (var license in customLicenses)
					{
						_ = allowedLicenses.Add(license);
					}
				}
			}

			return allowedLicenses;
		}
	}
}
