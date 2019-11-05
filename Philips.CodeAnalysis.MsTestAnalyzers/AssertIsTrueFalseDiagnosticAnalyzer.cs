// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	public abstract class AssertIsTrueFalseDiagnosticAnalyzer : AssertMethodCallDiagnosticAnalyzer
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods

		protected override Diagnostic Analyze(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocationExpressionSyntax, MemberAccessExpressionSyntax memberAccessExpression)
		{
			bool isIsTrue = false;
			string memberName = memberAccessExpression.Name.ToString();
			switch (memberName)
			{
				case "IsTrue":
					isIsTrue = true;
					break;
				case "IsFalse":
					isIsTrue = false;
					break;
				default:
					return null;
			}

			return Check(context, invocationExpressionSyntax, invocationExpressionSyntax.ArgumentList, isIsTrue);
		}
		protected virtual Diagnostic Check(SyntaxNodeAnalysisContext context, SyntaxNode node, ArgumentListSyntax arguments, bool isIsTrue)
		{
			var allArguments = arguments.Arguments;

			var test = allArguments.First()?.Expression;

			if (test != null)
			{
				return Check(context, node, test, isIsTrue);
			}
			return null;
		}

		protected abstract Diagnostic Check(SyntaxNodeAnalysisContext context, SyntaxNode node, ExpressionSyntax test, bool isIsTrue);

		#endregion

		#region Public Interface
		#endregion
	}
}
