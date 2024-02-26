// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.Common
{
	public class ConstructorSyntaxHelper
	{
		internal ConstructorSyntaxHelper()
		{
			// Hide the constructor
		}

		public IReadOnlyDictionary<ConstructorDeclarationSyntax, ConstructorDeclarationSyntax> CreateMapping(SyntaxNodeAnalysisContext context, ConstructorDeclarationSyntax[] constructors)
		{
			Dictionary<ConstructorDeclarationSyntax, ISymbol> deferredCtor = [];
			Dictionary<ISymbol, ConstructorDeclarationSyntax> symbolToCtor = [];

			foreach (ConstructorDeclarationSyntax ctor in constructors)
			{
				IMethodSymbol method = context.SemanticModel.GetDeclaredSymbol(ctor);
				if (method != null)
				{
					symbolToCtor[method] = ctor;
				}

				if (ctor.Initializer != null && ctor.Initializer.ThisOrBaseKeyword.IsKind(SyntaxKind.ThisKeyword))
				{
					ISymbol otherConstructor = context.SemanticModel.GetSymbolInfo(ctor.Initializer).Symbol;
					deferredCtor[ctor] = otherConstructor;
				}
			}

			Dictionary<ConstructorDeclarationSyntax, ConstructorDeclarationSyntax> result = [];
			foreach (ConstructorDeclarationSyntax ctor in constructors)
			{
				if (deferredCtor.TryGetValue(ctor, out ISymbol otherCtor) && otherCtor != null)
				{
					result[ctor] = symbolToCtor[otherCtor];
				}
			}

			return result;
		}

		public IReadOnlyList<ConstructorDeclarationSyntax> GetCtorChain(IReadOnlyDictionary<ConstructorDeclarationSyntax, ConstructorDeclarationSyntax> mapping, ConstructorDeclarationSyntax ctor)
		{
			List<ConstructorDeclarationSyntax> chain = [ctor];

			HashSet<ConstructorDeclarationSyntax> seenConstructors = [];
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
