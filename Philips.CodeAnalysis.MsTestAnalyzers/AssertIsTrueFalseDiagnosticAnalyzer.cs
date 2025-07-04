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
		protected override IEnumerable<Diagnostic> Analyze(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocationExpressionSyntax, MemberAccessExpressionSyntax memberAccessExpression)
		{
			bool isIsTrue;
			var memberName = memberAccessExpression.Name.ToString();
			switch (memberName)
			{
				case StringConstants.IsTrue:
					isIsTrue = true;
					break;
				case StringConstants.IsFalse:
					isIsTrue = false;
					break;
				default:
					return Array.Empty<Diagnostic>();
			}

			Diagnostic result = Check(context, invocationExpressionSyntax, invocationExpressionSyntax.ArgumentList, isIsTrue);

			return result is null ? Array.Empty<Diagnostic>() : [result];
		}
		protected virtual Diagnostic Check(SyntaxNodeAnalysisContext context, SyntaxNode node, ArgumentListSyntax arguments, bool isIsTrue)
		{
			SeparatedSyntaxList<ArgumentSyntax> allArguments = arguments.Arguments;

			ExpressionSyntax test = allArguments.FirstOrDefault()?.Expression;

			return test != null ? Check(context, node, test, isIsTrue) : null;
		}

		protected abstract Diagnostic Check(SyntaxNodeAnalysisContext context, SyntaxNode node, ExpressionSyntax test, bool isIsTrue);
	}
}
