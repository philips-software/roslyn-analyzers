// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;

namespace Philips.CodeAnalysis.Common
{
	public class TypesHelper
	{
		public bool IsExtensionClass(INamedTypeSymbol declaredSymbol)
		{
			return
				declaredSymbol is { MightContainExtensionMethods: true } &&
					!declaredSymbol.GetMembers().Any(m =>
						m.Kind == SymbolKind.Method &&
						m.DeclaredAccessibility == Accessibility.Public &&
						!((IMethodSymbol)m).IsExtensionMethod);
		}

		public INamedTypeSymbol GetTypeSymbol(ISymbol symbol)
		{
			return symbol switch
			{
				IFieldSymbol fieldSymbol => fieldSymbol.Type as INamedTypeSymbol,
				IPropertySymbol propertySymbol => propertySymbol.Type as INamedTypeSymbol,
				ILocalSymbol localSymbol => localSymbol.Type as INamedTypeSymbol,
				IParameterSymbol parameterSymbol => parameterSymbol.Type as INamedTypeSymbol,
				INamedTypeSymbol namedTypeSymbol => namedTypeSymbol,
				_ => null
			};
		}

		public bool IsInheritingFromClass(INamedTypeSymbol inputType, string classTypeName)
		{
			INamedTypeSymbol type = inputType;
			while (type != null)
			{
				if (type.Name == classTypeName)
				{
					return true;
				}
				type = type.BaseType;
			}

			return false;
		}

		public bool IsUserControl(INamedTypeSymbol type)
		{
			return IsInheritingFromClass(type, @"ContainerControl");
		}

	}
}
