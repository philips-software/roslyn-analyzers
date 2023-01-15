// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.Common
{
	public static class ConstructorSyntaxHelper
	{
		/// <summary>
		/// CreateMapping
		/// </summary>
		/// <param name="context"></param>
		/// <param name="constructors"></param>
		/// <returns></returns>
		public static Dictionary<ConstructorDeclarationSyntax, ConstructorDeclarationSyntax> CreateMapping(SyntaxNodeAnalysisContext context, ConstructorDeclarationSyntax[] constructors)
		{
			Dictionary<ConstructorDeclarationSyntax, ISymbol> deferredCtor = new();
			Dictionary<ISymbol, ConstructorDeclarationSyntax> symbolToCtor = new();

			foreach (var ctor in constructors)
			{
				IMethodSymbol method = context.SemanticModel.GetDeclaredSymbol(ctor);
				if (method != null)
				{
					symbolToCtor[method] = ctor;
				}

				if (ctor.Initializer != null && ctor.Initializer.ThisOrBaseKeyword.IsKind(SyntaxKind.ThisKeyword))
				{
					var otherCostructor = context.SemanticModel.GetSymbolInfo(ctor.Initializer).Symbol;
					deferredCtor[ctor] = otherCostructor;
				}
			}

			Dictionary<ConstructorDeclarationSyntax, ConstructorDeclarationSyntax> result = new();
			foreach (var ctor in constructors)
			{
				if (deferredCtor.TryGetValue(ctor, out var otherCtor) && otherCtor != null)
				{
					result[ctor] = symbolToCtor[otherCtor];
				}
			}

			return result;
		}

		/// <summary>
		/// GetCtorChain
		/// </summary>
		/// <param name="mapping"></param>
		/// <param name="ctor"></param>
		/// <returns></returns>
		public static List<ConstructorDeclarationSyntax> GetCtorChain(Dictionary<ConstructorDeclarationSyntax, ConstructorDeclarationSyntax> mapping, ConstructorDeclarationSyntax ctor)
		{
			List<ConstructorDeclarationSyntax> chain = new() { ctor };

			HashSet<ConstructorDeclarationSyntax> seenConstructors = new();
			while (true)
			{
				if (!mapping.TryGetValue(ctor, out ctor))
				{
					break;
				}

				if (!seenConstructors.Add(ctor))
				{
					//We've seen this constructor already.  There is a loop in the constructor chain.  This is not allowed per C#, but as we are just looking at 
					//written code, not compiled code, the user can type it.
					break;
				}

				chain.Add(ctor);
			}

			return chain;
		}


	}
}
