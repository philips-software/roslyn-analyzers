// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class EveryLinqStatementOnSeparateLineAnalyzer : SingleDiagnosticAnalyzer<QueryExpressionSyntax, EveryLinqStatementOnSeparateLineSyntaxNodeAction>
	{
		private const string Title = @"Put every linq statement on a separate line";
		private const string MessageFormat = Title;
		private const string Description = Title;

		public EveryLinqStatementOnSeparateLineAnalyzer()
			: base(DiagnosticId.EveryLinqStatementOnSeparateLine, Title, MessageFormat, Description, Categories.Readability)
		{ }
	}

	public class EveryLinqStatementOnSeparateLineSyntaxNodeAction : SyntaxNodeAction<QueryExpressionSyntax>
	{
		public override void Analyze()
		{
			FromClauseSyntax from = Node.FromClause;
			if (!EndsWithNewline(from))
			{
				Location fromLocation = from.GetLocation();
				ReportDiagnostic(fromLocation);
			}

			foreach (QueryClauseSyntax clause in Node.Body.Clauses)
			{
				if (!EndsWithNewline(clause))
				{
					Location clauseLocation = clause.GetLocation();
					ReportDiagnostic(clauseLocation);
				}
			}
		}

		private static bool EndsWithNewline(QueryClauseSyntax clause)
		{
			return clause.GetLastToken().TrailingTrivia.Any(SyntaxKind.EndOfLineTrivia);
		}
	}
}
