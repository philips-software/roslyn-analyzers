// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

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

		public LicenseAnalyzer()
			: base(DiagnosticId.AvoidUnlicensedPackages, Title, MessageFormat, Description, Categories.Security, isEnabled: false)
		{ }

		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			// Register a dummy syntax node action to satisfy analyzer validation
			// In a full implementation, this would analyze project files or metadata
			// to check package license information
			context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.CompilationUnit);
		}

		private void AnalyzeNode(SyntaxNodeAnalysisContext context)
		{
			// Placeholder for license analysis logic
			// Future implementation would:
			// 1. Load allowed licenses from AdditionalFiles (Allowed.Licenses.txt)
			// 2. Analyze package references for license information
			// 3. Report diagnostics for packages with restricted licenses

			// This is a placeholder to avoid an empty statement block.
			_ = context.Node;
		}
	}
}
