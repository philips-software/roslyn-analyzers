// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;

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
	}
}
