// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
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
		public const string MessageFormat = @"Assert.AreEqual/AreNotEqual should be of the form AreEqual(<Expected Non-Null Literal>, <Actual Expression>).";
		private const string Title = @"Assert.AreEqual/AreNotEqual Usage";
		private const string Description = @"Assert.AreEqual(<actual>, <expected>) => Assert.AreEqual(<expected>, <actual>) and Assert.AreEqual(null, <actual>) => Assert.IsNull(<actual>).";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticId.AssertAreEqual), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		protected override IEnumerable<Diagnostic> Analyze(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocationExpressionSyntax, MemberAccessExpressionSyntax memberAccessExpression)
		{
			string memberName = memberAccessExpression.Name switch
			{
				GenericNameSyntax generic => generic.Identifier.ToString(),
				SimpleNameSyntax name => name.ToString()
			};

			if (memberName is not @"AreEqual" and not @"AreNotEqual")
			{
				return null;
			}

			if ((context.SemanticModel.GetSymbolInfo(memberAccessExpression).Symbol is not IMethodSymbol memberSymbol) || !memberSymbol.ToString().StartsWith("Microsoft.VisualStudio.TestTools.UnitTesting.Assert"))
			{
				return null;
			}

			// Assert.AreEqual is incorrectly used if the literal is the second argument (including null) or if the first argument is null
			ArgumentListSyntax argumentList = invocationExpressionSyntax.ArgumentList;

			bool arg0Literal = Helper.IsLiteral(argumentList.Arguments[0].Expression, context.SemanticModel);
			bool arg1Literal = Helper.IsLiteral(argumentList.Arguments[1].Expression, context.SemanticModel);
			bool arg0Null = IsNull(argumentList.Arguments[0].Expression);

			if (!arg0Literal && !arg1Literal)
			{
				return null;
			}

			if (arg0Literal && !arg0Null)
			{
				return null;
			}

			if (arg1Literal || arg0Null)
			{
				var location = invocationExpressionSyntax.GetLocation();
				Diagnostic diagnostic = Diagnostic.Create(Rule, location);
				return new[] { diagnostic };
			}

			return null;
		}


		private bool IsNull(ExpressionSyntax expression)
		{
			return expression.ToString().Contains("null");
		}
	}
}
