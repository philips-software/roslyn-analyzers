// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Philips.CodeAnalysis.Common
{
	public class TypesHelper
	{
		private readonly CodeFixHelper _helper;

		internal TypesHelper(CodeFixHelper helper)
		{
			_helper = helper;
		}

		public string GetTypeNameWithoutGeneric(TypeSyntax type)
		{
			IReadOnlyDictionary<string, string> aliases = _helper.ForNamespaces.GetUsingAliases(type);
			var typeName = type.GetFullName(aliases);
			if (type is GenericNameSyntax genericName)
			{
				var baseName = genericName.Identifier.Text;
				if (!aliases.TryGetValue(baseName, out typeName))
				{
					typeName = baseName;
				}
			}

			return typeName;
		}

		public bool IsExtensionClass(INamedTypeSymbol declaredSymbol)
		{
			return
				declaredSymbol is { MightContainExtensionMethods: true } &&
					!declaredSymbol.GetMembers().Any(m =>
						m.Kind == SymbolKind.Method &&
						m.DeclaredAccessibility == Accessibility.Public &&
						!((IMethodSymbol)m).IsExtensionMethod);
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
