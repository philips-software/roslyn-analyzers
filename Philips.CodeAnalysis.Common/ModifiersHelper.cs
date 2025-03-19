// © 2024 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Philips.CodeAnalysis.Common
{
	public class ModifiersHelper
	{
		internal ModifiersHelper()
		{
			// Hide the constructor.
		}

		public SyntaxTokenList GetModifiers(SyntaxNode node)
		{
			if (node is MethodDeclarationSyntax method)
			{
				return method.Modifiers;
			}
			else if (node is MemberDeclarationSyntax member)
			{
				return member.Modifiers;
			}
			else if (node is PropertyDeclarationSyntax prop)
			{
				return prop.Modifiers;
			}
			else if (node is TypeDeclarationSyntax type)
			{
				return type.Modifiers;
			}
			return new SyntaxTokenList();
		}

		public bool IsOverridden(SyntaxNode node)
		{
			return GetModifiers(node).Any(SyntaxKind.OverrideKeyword);
		}

		public bool IsCallableFromOutsideClass(SyntaxNode node)
		{
			SyntaxTokenList modifiers = GetModifiers(node);
			return modifiers.Any(SyntaxKind.PublicKeyword) || modifiers.Any(SyntaxKind.InternalKeyword) || modifiers.Any(SyntaxKind.ProtectedKeyword);
		}

	}
}
