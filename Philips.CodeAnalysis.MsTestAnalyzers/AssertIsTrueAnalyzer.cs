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
	public class AssertIsTrueAnalyzer : AssertIsTrueFalseDiagnosticAnalyzer
	{
		private const string IsEqualTitle = @"Assert.IsTrue/IsFalse Usage";
		private const string IsEqualMessageFormat = @"Do not call IsTrue/IsFalse if AreEqual/AreNotEqual will suffice";
		private const string IsEqualDescription = @"Assert.IsTrue(<actual> == <expected>) => Assert.AreEqual(<expected>, <actual>)";
		private const string Category = Categories.Maintainability;
		private const string EqualsName = "Equals";

		private static readonly DiagnosticDescriptor IsEqualRule = new(Helper.ToDiagnosticId(DiagnosticId.AssertIsEqual), IsEqualTitle, IsEqualMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: IsEqualDescription);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(IsEqualRule); } }

		protected override Diagnostic Check(SyntaxNodeAnalysisContext context, SyntaxNode node, ExpressionSyntax test, bool isIsTrue)
		{
			var kind = test.Kind();
			var location = node.GetLocation();
			switch (kind)
			{
				case SyntaxKind.LogicalNotExpression:
					//recurse a bit here.
					_ = Check(context, node, ((PrefixUnaryExpressionSyntax)test).Operand, isIsTrue);
					return null;
				case SyntaxKind.LogicalAndExpression:
					if (isIsTrue)
					{
						return Diagnostic.Create(IsEqualRule, location);
					}
					return null;
				case SyntaxKind.EqualsExpression:
				case SyntaxKind.NotEqualsExpression:
					return Diagnostic.Create(IsEqualRule, location);
				case SyntaxKind.InvocationExpression:
					//they are calling a function.  Don't let them calls .Equals or something like that.
					if (CheckForEqualityFunction(context, (InvocationExpressionSyntax)test))
					{
						return Diagnostic.Create(IsEqualRule, location);
					}
					return null;
				case SyntaxKind.IdentifierName:
					//need to check where the identifier comes from?
					return null;
				default:
					return null;
			}
		}

		private static bool CheckForEqualityFunction(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax test)
		{
			if (test.Expression is not MemberAccessExpressionSyntax member || member.Name.ToString() != EqualsName)
			{
				//they called something that wasn't a member function.  Maybe a static method.
				return false;
			}

			IMethodSymbol sym = (IMethodSymbol)context.SemanticModel.GetSymbolInfo(member).Symbol;

			if (sym == null)
			{
				return false;
			}

			//would love to check if the types are actually IComparable<> here.  Speaks to intent.
			return sym.Name == EqualsName;
		}
	}
}
