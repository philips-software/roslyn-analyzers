// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

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
			: base(DiagnosticId.AssertAreEqualTypesMatch, Title, MessageFormat, Description, Categories.Maintainability)
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

			string memberName = maes.Name.ToString();
			if (memberName is not StringConstants.AreEqualMethodName and not StringConstants.AreNotEqualMethodName)
			{
				return;
			}

			if (Context.SemanticModel.GetSymbolInfo(maes).Symbol is not IMethodSymbol memberSymbol || !memberSymbol.ToString().StartsWith(StringConstants.AssertFullyQualifiedName))
			{
				return;
			}

			// If it resolved to Are[Not]Equal<T>, then we know the types are the same.
			if (memberSymbol.IsGenericMethod)
			{
				return;
			}

			ArgumentListSyntax argumentList = Node.ArgumentList;
			TypeInfo ti1 = Context.SemanticModel.GetTypeInfo(argumentList.Arguments[0].Expression);
			TypeInfo ti2 = Context.SemanticModel.GetTypeInfo(argumentList.Arguments[1].Expression);

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
