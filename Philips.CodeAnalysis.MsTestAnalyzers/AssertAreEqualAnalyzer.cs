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
	public class AssertAreEqualAnalyzer : AssertMethodCallDiagnosticAnalyzer
	{
		private const string Title = @"Assert.AreEqual/AreNotEqual Usage";
		private const string MessageFormat = @"Assert.AreEqual/AreNotEqual should be of the form AreEqual(<Expected Non-Null Literal>, <Actual Expression>).";
		private const string Description = @"Assert.AreEqual(<actual>, <expected>) => Assert.AreEqual(<expected>, <actual>) and Assert.AreEqual(null, <actual>) => Assert.IsNull(<actual>).";
		private const string Category = Categories.Maintainability;

		private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.AssertAreEqual), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		protected override Diagnostic Analyze(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocationExpressionSyntax, MemberAccessExpressionSyntax memberAccessExpression)
		{
			string memberName = memberAccessExpression.Name.ToString();
			if ((memberName != @"AreEqual") && (memberName != @"AreNotEqual"))
			{
				return null;
			}

			IMethodSymbol memberSymbol = context.SemanticModel.GetSymbolInfo(memberAccessExpression).Symbol as IMethodSymbol;
			if ((memberSymbol == null) || !memberSymbol.ToString().StartsWith("Microsoft.VisualStudio.TestTools.UnitTesting.Assert"))
			{
				return null;
			}

			// Assert.AreEqual is incorrectly used if the literal is the second argument (including null) or if the first argument is null
			bool isUsedIncorrectly = false;
			ArgumentListSyntax argumentList = invocationExpressionSyntax.ArgumentList as ArgumentListSyntax;
			LiteralExpressionSyntax arg0Literal = argumentList.Arguments[0].Expression as LiteralExpressionSyntax;
			LiteralExpressionSyntax arg1Literal = argumentList.Arguments[1].Expression as LiteralExpressionSyntax;
			if ((arg1Literal != null) || Helper.IsConstantExpression(argumentList.Arguments[1].Expression, context.SemanticModel))
			{
				if ((arg0Literal == null) && !Helper.IsConstantExpression(argumentList.Arguments[0].Expression, context.SemanticModel))
				{
					isUsedIncorrectly = true;
				}
				else if (arg1Literal != null)
				{
					Optional<object> literalValue = context.SemanticModel.GetConstantValue(arg1Literal);
					if (literalValue.Value == null)
					{
						isUsedIncorrectly = true;
					}
				}
			}
			else if (arg0Literal != null)
			{
				Optional<object> literalValue = context.SemanticModel.GetConstantValue(arg0Literal);
				if (literalValue.Value == null)
				{
					isUsedIncorrectly = true;
				}
			}

			if (isUsedIncorrectly)
			{
				Diagnostic diagnostic = Diagnostic.Create(Rule, invocationExpressionSyntax.GetLocation());
				return diagnostic;
			}

			return null;
		}
	}
}
