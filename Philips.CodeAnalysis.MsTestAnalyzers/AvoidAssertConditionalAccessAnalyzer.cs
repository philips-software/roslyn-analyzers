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
	public class AvoidAssertConditionalAccessAnalyzer : AssertMethodCallDiagnosticAnalyzer
	{
		private const string Title = @"Assert Inline Null Check Usage";
		public const string MessageFormat = @"Do not use an inline null check while asserting. Use IsNotNull check.";
		private const string Description = @"Assert.AreEqual(<actual>?.attribute, <expected>) => Assert.IsNotNull(<actual>); Assert.AreEqual(<actual>.attribute, <expected>)";

		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.AvoidAssertConditionalAccess), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		protected override Diagnostic Analyze(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocationExpressionSyntax, MemberAccessExpressionSyntax memberAccessExpression)
		{
			string memberName = memberAccessExpression.Name.ToString();
			if (memberName != @"AreEqual" && memberName != @"AreNotEqual")
			{
				return null;
			}

			if (invocationExpressionSyntax.ArgumentList?.Arguments.Count < 2)
			{
				return null;
			}

			ArgumentSyntax actual = invocationExpressionSyntax.ArgumentList?.Arguments[0];
			ArgumentSyntax expected = invocationExpressionSyntax.ArgumentList?.Arguments[1];

			if (expected.Expression.Kind() == SyntaxKind.ConditionalAccessExpression || actual.Expression.Kind() == SyntaxKind.ConditionalAccessExpression)
			{
				Diagnostic diagnostic = Diagnostic.Create(Rule, invocationExpressionSyntax.GetLocation());
				return diagnostic;
			}

			return null;
		}
	}
}
