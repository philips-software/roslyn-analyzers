// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
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
	public class AssertAreEqualLiteralAnalyzer : AssertMethodCallDiagnosticAnalyzer
	{
		private const string Title = @"Assert.AreEqual(true, true) Usage";
		private const string MessageFormat = @"Do not call AreEqual with a literal true/false";
		private const string Description = MessageFormat;

		private const string Category = Categories.MsTest;

		private static readonly DiagnosticDescriptor Rule = new(DiagnosticId.AssertAreEqualLiteral.ToId(), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		protected override IEnumerable<Diagnostic> Analyze(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocationExpressionSyntax, MemberAccessExpressionSyntax memberAccessExpression)
		{
			switch (memberAccessExpression.Name.ToString())
			{
				case @"AreEqual":
				case StringConstants.AreNotEqualMethodName:
					break;
				default:
					return Array.Empty<Diagnostic>();
			}

			if (invocationExpressionSyntax.ArgumentList?.Arguments.Count < 2)
			{
				return Array.Empty<Diagnostic>();
			}

			ArgumentSyntax expected = invocationExpressionSyntax.ArgumentList?.Arguments[0];
			ArgumentSyntax actual = invocationExpressionSyntax.ArgumentList?.Arguments[1];

			// We only need to check 'expected'.  Other analyzers catch any literals in the 'actual'
			if (!Helper.ForLiterals.IsTrueOrFalse(expected.Expression))
			{
				return Array.Empty<Diagnostic>();
			}

			// If the 'actual' is a nullable type, then it's reasonable to call, e.g., AreEqual(true, bool?)
			TypeInfo expectedType = context.SemanticModel.GetTypeInfo(expected.Expression);
			TypeInfo actualType = context.SemanticModel.GetTypeInfo(actual.Expression);

			Conversion conversion = context.SemanticModel.Compilation.ClassifyConversion(actualType.Type, expectedType.Type);
			Location location = invocationExpressionSyntax.GetLocation();
			return conversion.IsNullable ? Array.Empty<Diagnostic>() : (IEnumerable<Diagnostic>)([Diagnostic.Create(Rule, location)]);
		}
	}
}
