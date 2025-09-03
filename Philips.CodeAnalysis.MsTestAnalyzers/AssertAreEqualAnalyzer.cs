// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
		private const string Category = Categories.MsTest;

		private static readonly DiagnosticDescriptor Rule = new(DiagnosticId.AssertAreEqual.ToId(), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		protected override IEnumerable<Diagnostic> Analyze(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocationExpressionSyntax, MemberAccessExpressionSyntax memberAccessExpression)
		{
			var memberName = memberAccessExpression.Name switch
			{
				GenericNameSyntax generic => generic.Identifier.ToString(),
				SimpleNameSyntax name => name.ToString()
			};

			if (memberName is not StringConstants.AreEqualMethodName and not StringConstants.AreNotEqualMethodName)
			{
				return Array.Empty<Diagnostic>();
			}

			SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccessExpression);
			ISymbol symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
			if ((symbol is not IMethodSymbol memberSymbol) || !memberSymbol.ToString().StartsWith(StringConstants.AssertFullyQualifiedName))
			{
				return Array.Empty<Diagnostic>();
			}

			// Assert.AreEqual is incorrectly used if the literal is the second argument (including null) or if the first argument is null
			ArgumentListSyntax argumentList = invocationExpressionSyntax.ArgumentList;

			var isArg0Literal = Helper.ForLiterals.IsLiteral(argumentList.Arguments[0].Expression, context.SemanticModel);
			var isArg1Literal = Helper.ForLiterals.IsLiteral(argumentList.Arguments[1].Expression, context.SemanticModel);
			var isArg0Null = IsNull(argumentList.Arguments[0].Expression);

			if (!isArg0Literal && !isArg1Literal)
			{
				return Array.Empty<Diagnostic>();
			}

			if (isArg0Literal && !isArg0Null)
			{
				return Array.Empty<Diagnostic>();
			}

			Location location = invocationExpressionSyntax.GetLocation();
			var diagnostic = Diagnostic.Create(Rule, location);
			return new[] { diagnostic };
		}

		private bool IsNull(ExpressionSyntax expression)
		{
			return expression is LiteralExpressionSyntax literal &&
				   literal.IsKind(SyntaxKind.NullLiteralExpression);
		}
	}
}
