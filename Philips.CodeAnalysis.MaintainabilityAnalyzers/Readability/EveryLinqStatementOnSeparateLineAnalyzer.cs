// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class EveryLinqStatementOnSeparateLineAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Put every linq statement on a separate line";
		private const string MessageFormat = @"Put every linq statement on a separate line";
		private const string Description = @"Put every linq statement on a separate line";
		private const string Category = Categories.Readability;

		public static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.EveryLinqStatementOnSeparateLine), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.QueryExpression);
		}

		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			QueryExpressionSyntax query = (QueryExpressionSyntax)context.Node;

			FromClauseSyntax from = query.FromClause;
			if (!EndsWithNewline(from))
			{
				context.ReportDiagnostic(Diagnostic.Create(Rule, from.GetLocation()));
			}

			foreach (var clause in query.Body.Clauses)
			{
				if(!EndsWithNewline(clause))
				{
					context.ReportDiagnostic(Diagnostic.Create(Rule, clause.GetLocation()));
				}
			}
		}

		private static bool EndsWithNewline(QueryClauseSyntax clause)
		{
			return clause.GetLastToken().TrailingTrivia.Any(SyntaxKind.EndOfLineTrivia);
		}
	}
}
