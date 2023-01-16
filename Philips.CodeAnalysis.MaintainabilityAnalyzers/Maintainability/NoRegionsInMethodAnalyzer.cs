// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class NoRegionsInMethodAnalyzer : DiagnosticAnalyzer

	{
		private static readonly string Title = "No Regions In Methods";
		private static readonly string MessageFormat = "Regions are not allowed to start or end within a method";
		private static readonly string Description = "A #region cannot start or end within a method. Consider refactoring long methods instead.";
		private const string Category = Categories.Maintainability;

		public static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.NoRegionsInMethods), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);


			context.RegisterSyntaxNodeAction(OnMethod, SyntaxKind.MethodDeclaration);
		}

		private static void OnMethod(SyntaxNodeAnalysisContext context)
		{
			BaseMethodDeclarationSyntax node = (BaseMethodDeclarationSyntax)context.Node;

			// Specifying Span instead of FullSpan correctly excludes trivia before or after the method
			var descendants = node.DescendantNodes(node.Span, null, descendIntoTrivia: true);
			foreach (RegionDirectiveTriviaSyntax regionDirective in descendants.OfType<RegionDirectiveTriviaSyntax>())
			{
				var diagnostic = Diagnostic.Create(Rule, regionDirective.GetLocation());
				context.ReportDiagnostic(diagnostic);
			}

			foreach (EndRegionDirectiveTriviaSyntax endRegionDirective in descendants.OfType<EndRegionDirectiveTriviaSyntax>())
			{
				var diagnostic = Diagnostic.Create(Rule, endRegionDirective.GetLocation());
				context.ReportDiagnostic(diagnostic);
			}
		}
	}
}
