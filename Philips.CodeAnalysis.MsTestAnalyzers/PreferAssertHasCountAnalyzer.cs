// © 2026 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	/// <summary>
	/// Flags <c>Assert.AreEqual(a.Count, b.Count)</c> patterns that Microsoft's MSTEST0037 does not cover,
	/// recommending <c>Assert.HasCount(a.Count, b)</c> instead.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class PreferAssertHasCountAnalyzer : AssertMethodCallDiagnosticAnalyzer
	{
		private const string Title = @"Prefer Assert.HasCount over Assert.AreEqual(x.Count, y.Count)";
		private const string MessageFormat = @"Prefer Assert.HasCount(expected.Count, actual) over Assert.AreEqual on two .Count expressions";
		private const string Description = @"When both arguments to Assert.AreEqual end in .Count, Assert.HasCount provides a clearer failure message.";
		private const string Category = Categories.MsTest;
		private const string CountMemberName = "Count";

		private static readonly DiagnosticDescriptor Rule = new(
			DiagnosticId.PreferAssertHasCount.ToId(),
			Title,
			MessageFormat,
			Category,
			DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Description,
			helpLinkUri: DiagnosticId.PreferAssertHasCount.ToHelpLinkUrl());

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		protected override IEnumerable<Diagnostic> Analyze(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocationExpressionSyntax, MemberAccessExpressionSyntax memberAccessExpression)
		{
			var memberName = memberAccessExpression.Name switch
			{
				GenericNameSyntax generic => generic.Identifier.ToString(),
				SimpleNameSyntax name => name.ToString()
			};

			if (memberName != StringConstants.AreEqualMethodName)
			{
				return Array.Empty<Diagnostic>();
			}

			SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccessExpression);
			ISymbol symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
			if ((symbol is not IMethodSymbol memberSymbol) || !memberSymbol.ToString().StartsWith(StringConstants.AssertFullyQualifiedName))
			{
				return Array.Empty<Diagnostic>();
			}

			ArgumentListSyntax argumentList = invocationExpressionSyntax.ArgumentList;
			if (argumentList == null || argumentList.Arguments.Count < 2)
			{
				return Array.Empty<Diagnostic>();
			}

			if (!IsCountMemberAccess(argumentList.Arguments[0].Expression) ||
				!IsCountMemberAccess(argumentList.Arguments[1].Expression))
			{
				return Array.Empty<Diagnostic>();
			}

			Location location = invocationExpressionSyntax.GetLocation();
			var diagnostic = Diagnostic.Create(Rule, location);
			return new[] { diagnostic };
		}

		private static bool IsCountMemberAccess(ExpressionSyntax expression)
		{
			return expression is MemberAccessExpressionSyntax memberAccess &&
				memberAccess.Name.Identifier.ValueText == CountMemberName;
		}
	}
}
