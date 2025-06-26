// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AssertAreEqualTypesMatchAnalyzer : SingleDiagnosticAnalyzer<InvocationExpressionSyntax, AssertAreEqualTypesMatchSyntaxNodeAction>
	{
		public const string MessageFormat = @"The expected argument is of type {0}, which does not match the type of actual, which is {1}.";
		private const string Title = @"Assert.AreEqual parameter types must match";
		private const string Description = @"The types of the parameters of Are[Not]Equal must match.";

		public AssertAreEqualTypesMatchAnalyzer()
			: base(DiagnosticId.AssertAreEqualTypesMatch, Title, MessageFormat, Description, Categories.MsTest)
		{
		}
	}

	public class AssertAreEqualTypesMatchSyntaxNodeAction : SyntaxNodeAction<InvocationExpressionSyntax>
	{
		public override void Analyze()
		{
			if (Node.Expression is not MemberAccessExpressionSyntax maes)
			{
				return;
			}

			var memberName = maes.Name.ToString();
			if (memberName is not StringConstants.AreEqualMethodName and not StringConstants.AreNotEqualMethodName)
			{
				return;
			}

			SymbolInfo symbolInfo = Context.SemanticModel.GetSymbolInfo(maes);
			ISymbol symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault(); // We just need any match; AssertEqual has >35
			if (symbol is not IMethodSymbol memberSymbol || !memberSymbol.ToString().StartsWith(StringConstants.AssertFullyQualifiedName))
			{
				return;
			}

			ArgumentListSyntax argumentList = Node.ArgumentList;
			TypeInfo ti1 = Context.SemanticModel.GetTypeInfo(argumentList.Arguments[0].Expression);
			TypeInfo ti2 = Context.SemanticModel.GetTypeInfo(argumentList.Arguments[1].Expression);

			// If it resolved to Are[Not]Equal<T>, then we know the types are the same.
			if (memberSymbol.IsGenericMethod)
			{
				if (ti1.Type == null || ti2.Type == null)
				{
					return;
				}

				// If Roslyn inferred a common T, but the types are clearly different, report a finding.
				if (!SymbolEqualityComparer.Default.Equals(ti1.Type, ti2.Type))
				{
					Location location = Node.GetLocation();
					ReportDiagnostic(location, ti1.Type.ToString(), ti2.Type.ToString());
				}

				return;
			}

			// Our <actual> is of type object.  If it matches the type of <expected>, then AreEqual will pass, so we wouldn't want to fail here.
			// However, if the types differ, it will be a runtime Assert fail, so the early notice is advantageous.  The code is also clearer.
			// Moreover, if it were AreNotEqual, that is particularly insidious, because the Assert would pass due to the type difference
			// rather than the value difference.  Let's play it safe, and require the author to be clear.
			if (!Context.SemanticModel.Compilation.ClassifyConversion(ti2.Type, ti1.Type).IsImplicit)
			{
				Location location = Node.GetLocation();
				ReportDiagnostic(location, ti1.Type.ToString(), ti2.Type.ToString());
			}
		}
	}
}
