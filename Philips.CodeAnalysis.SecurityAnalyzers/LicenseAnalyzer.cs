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

			// For now, we'll implement a basic version that checks for package references
			// In a more complete implementation, this would:
			// 1. Parse project files to find PackageReference elements
			// 2. Query package repositories (like nuget.org) for license information
			// 3. Compare against allowed licenses list
			// 4. Report violations

			// Load custom allowed licenses from additional files
			HashSet<string> allowedLicenses = GetAllowedLicenses(context.Options.AdditionalFiles);

			// This is a placeholder implementation - in a real scenario we would:
			// - Access the project file context to get package references
			// - Query external APIs for license information
			// - Cache results for performance
			// For now, we just register that the analyzer is active but don't report any diagnostics

			// Example of how a violation would be reported:
			// var diagnostic = Diagnostic.Create(Rule, location, packageName);
			// context.ReportDiagnostic(diagnostic);

			// Avoid IDE0059 warning by explicitly using the variable
			GC.KeepAlive(allowedLicenses);
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
