// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class AvoidAssertConditionalAccessAnalyzer : AssertMethodCallDiagnosticAnalyzer
	{
		private const string Title = @"Assert Inline Null Check Usage";
		public const string MessageFormat = @"Do not use an inline null check while asserting. Use IsNotNull check.";
		private const string Description = @"Assert.AreEqual(<actual>?.attribute, <expected>) => Assert.IsNotNull(<actual>); Assert.AreEqual(<actual>.attribute, <expected>)";

		private const string Category = Categories.MsTest;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticId.AvoidAssertConditionalAccess), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		protected override IEnumerable<Diagnostic> Analyze(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocationExpressionSyntax, MemberAccessExpressionSyntax memberAccessExpression)
		{
			var memberName = memberAccessExpression.Name.ToString();
			if (memberName is not StringConstants.AreEqualMethodName and not StringConstants.AreNotEqualMethodName)
			{
				yield break;
			}

			ArgumentListSyntax argumentList = invocationExpressionSyntax.ArgumentList;

			if (argumentList.Arguments.Count < 2)
			{
				yield break;
			}

			ArgumentSyntax[] arguments = new[] { argumentList.Arguments[0], argumentList.Arguments[1] };
			foreach (ArgumentSyntax syntax in arguments.Where(InConditionalAccess))
			{
				Location location = syntax.GetLocation();
				var diagnostic = Diagnostic.Create(Rule, location);
				yield return diagnostic;
			}
		}

		private static bool InConditionalAccess(ArgumentSyntax argument)
		{
			return argument.DescendantNodes().Any(x => x.Kind() == SyntaxKind.ConditionalAccessExpression);
		}
	}
}
