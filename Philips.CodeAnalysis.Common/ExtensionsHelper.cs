// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Philips.CodeAnalysis.Common
{
	public static class ExtensionsHelper
	{
		public static bool IsDerivedFrom(this INamedTypeSymbol inputSymbol, INamedTypeSymbol other)
		{
			INamedTypeSymbol symbol = inputSymbol;
			while (symbol != null)
			{
				if (SymbolEqualityComparer.Default.Equals(symbol, other))
				{
					return true;
				}

				symbol = symbol.BaseType;
			}

			return false;
		}

		public static string GetFullName(this TypeSyntax typeSyntax, IReadOnlyDictionary<string, string> aliases)
		{
			string name = string.Empty;
			if (typeSyntax is SimpleNameSyntax simpleNameSyntax)
			{
				name = simpleNameSyntax.Identifier.Text;
			}
			else if (typeSyntax is QualifiedNameSyntax qualifiedNameSyntax)
			{
				string left = qualifiedNameSyntax.Left.GetFullName(aliases);
				string right = qualifiedNameSyntax.Right.Identifier.Text;
				name = $"{left}.{right}";
			}

			if (aliases.TryGetValue(name, out string aliased))
			{
				return aliased;
			}
			return name;
		}
	}
}