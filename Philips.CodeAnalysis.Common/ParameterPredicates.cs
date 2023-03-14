// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.Common
{
	public static class ParameterPredicates
	{
		public static bool IsEnum(this ParameterSyntax p, SyntaxNodeAnalysisContext context)
		{
			if (context.SemanticModel.GetDeclaredSymbol(p).Type is ITypeSymbol typ)
			{
				return typ.TypeKind == TypeKind.Enum;
			}
			return false;
		}
	}
}
