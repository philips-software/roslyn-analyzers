// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	/// <summary>
	/// Note that a CodeFixer isn't necessary. At the time of this writing, VS offers a refactoring but seemingly not an analyzer.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidUnnecessaryWhereAnalyzer : SingleDiagnosticAnalyzer<InvocationExpressionSyntax, AvoidUnnecessaryWhereSyntaxNodeAction>
	{
		private const string Title = @"Avoid unnecessary 'Where'";
		public const string MessageFormat = @"Move predicate from 'Where' to '{0}'";
		private const string Description = @"Invoking Where is unnecessary";

		public AvoidUnnecessaryWhereAnalyzer()
			: base(DiagnosticId.AvoidUnnecessaryWhere, Title, MessageFormat, Description, Categories.Maintainability, isEnabled: false)
		{ }
	}
	public class AvoidUnnecessaryWhereSyntaxNodeAction : SyntaxNodeAction<InvocationExpressionSyntax>
	{
		public override void Analyze()
		{
			// Is this a call to Count(), Any(), etc, w/o a predicate?
			if (Node.ArgumentList.Arguments.Count != 0)
			{
				return;
			}
			if (Node.Expression is not MemberAccessExpressionSyntax expressionOfInterest)
			{
				return;
			}
			if (expressionOfInterest.Name.Identifier.Text is not @"Count"
					and not @"Any"
					and not @"Single"
					and not @"SingleOrDefault"
					and not @"Last"
					and not @"LastOrDefault"
					and not @"First"
					and not @"FirstOrDefault"
			)
			{
				return;
			}

			// Is it from a Where clause?
			if (expressionOfInterest.Expression is not InvocationExpressionSyntax whereInvocationExpression)
			{
				return;
			}
			if (whereInvocationExpression.Expression is not MemberAccessExpressionSyntax whereExpression)
			{
				return;
			}
			if (whereExpression.Name.Identifier.Text is not @"Where")
			{
				return;
			}

			// It's practicially guaranteed we found something, but let's confirm it's System.Linq.Where
			var whereSymbol = Context.SemanticModel.GetSymbolInfo(whereExpression.Name).Symbol as IMethodSymbol;
			var strWhereSymbol = whereSymbol?.ToString();
			if (strWhereSymbol != null && strWhereSymbol.StartsWith(@"System.Collections.Generic.IEnumerable"))
			{
				Location location = whereExpression.Name.Identifier.GetLocation();
				ReportDiagnostic(location, expressionOfInterest.Name.Identifier.Text);
			}
		}
	}
}
