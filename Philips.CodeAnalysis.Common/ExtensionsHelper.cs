// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

		/// <summary>
		/// Determines if a local symbol is declared as an out parameter in a method call.
		/// </summary>
		/// <param name="localSymbol">The local symbol to check.</param>
		/// <returns>True if the symbol is declared as an out parameter, false otherwise.</returns>
		public static bool IsOutParameterDeclaration(this ILocalSymbol localSymbol)
		{
			// Find the declaration syntax for this local symbol
			SyntaxReference declarationSyntax = localSymbol.DeclaringSyntaxReferences.FirstOrDefault();
			if (declarationSyntax == null)
			{
				return false;
			}

			// Check if the declaration is within an ArgumentSyntax with an out modifier
			ArgumentSyntax argumentSyntax = declarationSyntax.GetSyntax().Ancestors().OfType<ArgumentSyntax>().FirstOrDefault();
			return argumentSyntax?.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword) ?? false;
		}
	}
}
