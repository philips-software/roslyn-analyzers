// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using LanguageExt;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

using static LanguageExt.Prelude;

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
		private static readonly System.Collections.Generic.HashSet<string> ExpressionsOfInterest = new()
		{
			@"Count",
			@"Any",
			@"Single",
			@"SingleOrDefault",
			@"Last",
			@"LastOrDefault",
			@"First",
			@"FirstOrDefault",
		};

		private static readonly string IEnumerableSymbol = @"System.Collections.Generic.IEnumerable";

		public override IEnumerable<Diagnostic> Analyze()
		{
			return Optional(Node)
				.Filter(node => node.ArgumentList.Arguments.Count == 0)
				.Bind<MemberAccessExpressionSyntax>(node => node.Expression as MemberAccessExpressionSyntax)
				.Filter(expression => ExpressionsOfInterest.Contains(expression.Name.Identifier.Text))
				.Bind(AnalyzeExpression);
		}

		private Option<Diagnostic> AnalyzeExpression(MemberAccessExpressionSyntax expression)
		{
			return Optional(expression.Expression as InvocationExpressionSyntax)
				.Bind<MemberAccessExpressionSyntax>(invocation => invocation.Expression as MemberAccessExpressionSyntax)
				.Bind(whereExpression => AnalyzeWhereExpression(expression, whereExpression));
		}

		private Option<Diagnostic> AnalyzeWhereExpression(MemberAccessExpressionSyntax expressionOfInterest, MemberAccessExpressionSyntax whereExpression)
		{
			return Optional(Context.SemanticModel.GetSymbolInfo(whereExpression.Name).Symbol as IMethodSymbol)
				.Filter(whereSymbol => whereSymbol.ToString().StartsWith(IEnumerableSymbol))
				.Select(whereSymbol =>
				{
					Location location = whereExpression.Name.Identifier.GetLocation();
					return PrepareDiagnostic(location, expressionOfInterest.Name.Identifier.Text);
				});
		}
	}
}
