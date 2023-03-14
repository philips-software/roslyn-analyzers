// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
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
			: base(DiagnosticId.EveryLinqStatementOnSeparateLine, Title, MessageFormat, Description, Categories.Readability, isEnabled: false)
		{ }
	}

	public class EveryLinqStatementOnSeparateLineSyntaxNodeAction : SyntaxNodeAction<QueryExpressionSyntax>
	{
		public override IEnumerable<Diagnostic> Analyze()
		{
			var errors = new List<Diagnostic>();

			FromClauseSyntax from = Node.FromClause;
			if (!EndsWithNewline(from))
			{
				Location fromLocation = from.GetLocation();
				errors.Add(PrepareDiagnostic(fromLocation));
			}

			foreach (QueryClauseSyntax clause in Node.Body.Clauses)
			{
				if (!EndsWithNewline(clause))
				{
					Location clauseLocation = clause.GetLocation();
					errors.Add(PrepareDiagnostic(clauseLocation));
				}
			}
			return errors;
		}

		private static bool EndsWithNewline(QueryClauseSyntax clause)
		{
			return clause.GetLastToken().TrailingTrivia.Any(SyntaxKind.EndOfLineTrivia);
		}
	}
}
