// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AssertIsTrueParenthesisAnalyzer : SingleDiagnosticAnalyzer<InvocationExpressionSyntax, AssertIsTrueParenthesisSyntaxNodeAction>
	{
		private const string Title = @"Assert.IsTrue/IsFalse Should not be in parenthesis";
		private const string MessageFormat = @"Do not call IsTrue/IsFalse with parenthesis around the argument";
		private const string Description = @"Assert.IsTrue((<actual> == <expected>)) => Assert.IsTrue(<expected> == <actual>)";
		public AssertIsTrueParenthesisAnalyzer()
			: base(DiagnosticId.AssertIsTrueParenthesis, Title, MessageFormat, Description, Categories.Maintainability)
		{
		}
	}

	public class AssertIsTrueParenthesisSyntaxNodeAction : SyntaxNodeAction<InvocationExpressionSyntax>
	{
		public override void Analyze()
		{
			if (Node.Expression is not MemberAccessExpressionSyntax memberAccessExpression)
			{
				return;
			}

			string memberName = memberAccessExpression.Name.ToString();
			if (memberName is not "IsTrue" and not "IsFalse")
			{
				return;
			}

			if (Node.ArgumentList.Arguments.Count == 0)
			{
				return;
			}

			ArgumentSyntax arg0 = Node.ArgumentList.Arguments[0];

			if (arg0.Expression.Kind() == SyntaxKind.ParenthesizedExpression)
			{
				var location = arg0.GetLocation();
				ReportDiagnostic(location);
			}
		}
	}
}
