// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Philips.CodeAnalysis.Common
{
	public static class MethodPredicates
	{
		public static bool IsOverridden(this MethodDeclarationSyntax methodDeclarationSyntax)
		{
			return methodDeclarationSyntax.Modifiers.Any(SyntaxKind.OverrideKeyword);
		}

		public static bool ReturnsVoid(this MethodDeclarationSyntax methodDeclarationSyntax)
		{
			if (methodDeclarationSyntax.ReturnType is PredefinedTypeSyntax predefinedTypeSyntax)
			{
				return predefinedTypeSyntax.Keyword.IsKind(SyntaxKind.VoidKeyword);
			}
			return false;
		}

		public static Diagnostic CreateDiagnostic(this MethodDeclarationSyntax methodDeclarationSyntax, DiagnosticDescriptor rule)
		{
			Location location = methodDeclarationSyntax.Identifier.GetLocation();
			return Diagnostic.Create(rule, location, methodDeclarationSyntax.Identifier.Text);
		}
	}
}
