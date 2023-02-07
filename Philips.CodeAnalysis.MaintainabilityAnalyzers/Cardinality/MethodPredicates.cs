// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using LanguageExt;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using static LanguageExt.Prelude;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Cardinality
{

		public static class MethodPredicates
		{
		public static bool IsNotOverridenMethod(MethodDeclarationSyntax m)
		{
			return !m.Modifiers.Any(SyntaxKind.OverrideKeyword);
		}

		public static bool IsNotExtensionMethod(IEnumerable<(string MethodName, ParameterSyntax ParameterSyntax, IParameterSymbol TypeSymbol)> ps)
		{
			return !ps.Any((p) => p.TypeSymbol.IsThis);
		}

		public static (SyntaxToken MethodId, PredefinedTypeSyntax ReturnType) MethodReturnType(MethodDeclarationSyntax m)
		{
			return (m.Identifier, m.ReturnType as PredefinedTypeSyntax);
		}

		public static IEnumerable<(string MethodName, ParameterSyntax ParameterSyntax, IParameterSymbol TypeSymbol)> MethodParameters(MethodDeclarationSyntax m, SyntaxNodeAnalysisContext context)
		{
			return m.ParameterList.Parameters.Select((p) => (m.Identifier.Text, p, context.SemanticModel.GetDeclaredSymbol(p)));
		}

		}
}
