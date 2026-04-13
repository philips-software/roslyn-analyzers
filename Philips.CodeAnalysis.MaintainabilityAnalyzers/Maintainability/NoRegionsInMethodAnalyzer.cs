// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
			IEnumerable<DirectiveTriviaSyntax> directives = Node
				.DescendantTrivia(Node.Span, descendIntoTrivia: true)
				.Select(trivia => trivia.GetStructure())
				.OfType<DirectiveTriviaSyntax>();

			foreach (DirectiveTriviaSyntax directive in directives)
			{
				if (directive.IsKind(SyntaxKind.RegionDirectiveTrivia) ||
					directive.IsKind(SyntaxKind.EndRegionDirectiveTrivia))
				{
					ReportDiagnostic(directive.GetLocation());
				}
			}
		}
	}
}
