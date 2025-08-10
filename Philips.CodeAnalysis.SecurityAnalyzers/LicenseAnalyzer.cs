// © 2024 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.SecurityAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class LicenseAnalyzer : SingleDiagnosticAnalyzer
	{
		private const string Title = @"Avoid Unlicensed Packages";
		public const string MessageFormat = @"Package '{0}' has unknown or restricted license. Consider adding to Allowed.Licenses.txt if license is acceptable.";
		private const string Description = @"Packages with unknown, commercial, or restricted licenses should be reviewed before use to ensure compliance with project requirements.";

		public const string AllowedLicensesFileName = @"Allowed.Licenses.txt";

		public LicenseAnalyzer()
			: base(DiagnosticId.AvoidUnlicensedPackages, Title, MessageFormat, Description, Categories.Security)
		{ }

		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			// For now, this is a placeholder implementation
			// In a full implementation, this would analyze package references
			// and check their license information against the allowlist
			// The analyzer is disabled by default (isEnabledByDefault: false in base class)
			
			// Register a dummy action to satisfy analyzer validation
			context.RegisterCompilationEndAction(AnalyzeCompilation);
		}

		private void AnalyzeCompilation(CompilationAnalysisContext context)
		{
			// Placeholder for license analysis logic
			// Future implementation would:
			// 1. Load allowed licenses from AdditionalFiles (Allowed.Licenses.txt)
			// 2. Analyze package references for license information
			// 3. Report diagnostics for packages with restricted licenses
		}
	}
}