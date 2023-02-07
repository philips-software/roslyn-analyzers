// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class NoRegionsInMethodAnalyzer : SingleDiagnosticAnalyzer<MethodDeclarationSyntax, NoRegionsInMethodSyntaxNodeAction>

	{
		private static readonly string Title = "No Regions In Methods";
		private static readonly string MessageFormat = "Regions are not allowed to start or end within a method";
		private static readonly string Description = "A #region cannot start or end within a method. Consider refactoring long methods instead.";

		public NoRegionsInMethodAnalyzer()
			: base(DiagnosticId.NoRegionsInMethods, Title, MessageFormat, Description, Categories.Maintainability)
		{ }
	}

	public class NoRegionsInMethodSyntaxNodeAction : SyntaxNodeAction<MethodDeclarationSyntax>
	{
		public override void Analyze()
		{
			// Specifying Span instead of FullSpan correctly excludes trivia before or after the method
			var descendants = Node.DescendantNodes(Node.Span, null, descendIntoTrivia: true).OfType<DirectiveTriviaSyntax>();
			foreach (RegionDirectiveTriviaSyntax regionDirective in descendants.OfType<RegionDirectiveTriviaSyntax>())
			{
				var location = regionDirective.GetLocation();
				ReportDiagnostic(location);
			}

			foreach (EndRegionDirectiveTriviaSyntax endRegionDirective in descendants.OfType<EndRegionDirectiveTriviaSyntax>())
			{
				var location = endRegionDirective.GetLocation();
				ReportDiagnostic(location);
			}
		}
	}
}
