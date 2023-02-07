// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using LanguageExt;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

using static LanguageExt.Prelude;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AssertIsTrueParenthesisAnalyzer : SingleDiagnosticAnalyzer<InvocationExpressionSyntax, AssertIsTrueParenthesisSyntaxNodeAction>
	{
		private const string Title = @"Assert.IsTrue/IsFalse Should not be in parenthesis";
		private const string MessageFormat = @"Do not call IsTrue/IsFalse with parenthesis around the argument";
		private const string Description = @"Assert.IsTrue((<actual> == <expected>)) => Assert.IsTrue(<expected> == <actual>)";
		public AssertIsTrueParenthesisAnalyzer()
			: base(DiagnosticId.AssertIsTrueParenthesis, Title, MessageFormat, Description, Categories.MsTest)
		{
		}
	}

	public class AssertIsTrueParenthesisSyntaxNodeAction : SyntaxNodeAction<InvocationExpressionSyntax>
	{
		public override IEnumerable<Diagnostic> Analyze()
		{
			if (Node.Expression is not MemberAccessExpressionSyntax memberAccessExpression)
			{
				return Option<Diagnostic>.None;
			}

			var memberName = memberAccessExpression.Name.ToString();
			if (memberName is not StringConstants.IsTrue and not StringConstants.IsFalse)
			{
				return Option<Diagnostic>.None;
			}

			if (Node.ArgumentList.Arguments.Count == 0)
			{
				return Option<Diagnostic>.None;
			}

			ArgumentSyntax arg0 = Node.ArgumentList.Arguments[0];

			if (arg0.Expression.Kind() == SyntaxKind.ParenthesizedExpression)
			{
				Location location = arg0.GetLocation();
				return Optional(PrepareDiagnostic(location));
			}

			return Option<Diagnostic>.None;
		}
	}
}
