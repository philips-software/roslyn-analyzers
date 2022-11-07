// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
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

		protected override IEnumerable<Diagnostic> Analyze(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocationExpressionSyntax, MemberAccessExpressionSyntax memberAccessExpression)
		{
			bool isIsTrue;
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

			Diagnostic result = Check(context, invocationExpressionSyntax, invocationExpressionSyntax.ArgumentList, isIsTrue);

			if (result is null)
			{
				return Array.Empty<Diagnostic>();
			}

			return new[] { result };
		}
		protected virtual Diagnostic Check(SyntaxNodeAnalysisContext context, SyntaxNode node, ArgumentListSyntax arguments, bool isIsTrue)
		{
			var allArguments = arguments.Arguments;

			var test = allArguments.FirstOrDefault()?.Expression;

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
