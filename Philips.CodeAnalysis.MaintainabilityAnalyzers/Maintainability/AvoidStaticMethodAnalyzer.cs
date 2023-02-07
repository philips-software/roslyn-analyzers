// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using LanguageExt.SomeHelp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidStaticMethodAnalyzer : SingleDiagnosticAnalyzer<MethodDeclarationSyntax, AvoidStaticMethodSyntaxNodeAction>
	{
		private const string Title = @"Avoid static Methods";
		public const string MessageFormat = @"Avoid static Methods when possible";
		private const string Description = @"Do not unnecessarily mark methods as static.";

		public AvoidStaticMethodAnalyzer()
			: base(DiagnosticId.AvoidStaticMethods, Title, MessageFormat, Description, Categories.Maintainability, isEnabled: false)
		{ }
	}
	public class AvoidStaticMethodSyntaxNodeAction : SyntaxNodeAction<MethodDeclarationSyntax>
	{
		public override IEnumerable<Diagnostic> Analyze()
		{
			// Only analyzing static method declarations
			if (!Node.Modifiers.Any(SyntaxKind.StaticKeyword))
			{
				return Option<Diagnostic>.None;
			}

			// If the method is marked "extern", let it go.
			if (Node.Modifiers.Any(SyntaxKind.ExternKeyword))
			{
				return Option<Diagnostic>.None;
			}

			// If the class is static, we need to let it go.
			ClassDeclarationSyntax classDeclarationSyntax = Context.Node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
			if (classDeclarationSyntax == null)
			{
				return Option<Diagnostic>.None;
			}
			if (classDeclarationSyntax.Modifiers.Any(SyntaxKind.StaticKeyword))
			{
				return Option<Diagnostic>.None;
			}

			// The Main entrypoint to the program must be static
			if (Node.Identifier.ValueText == @"Main")
			{
				return Option<Diagnostic>.None;
			}

			// Hunt for static members
			INamedTypeSymbol us = Context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
			if (us == null)
			{
				return Option<Diagnostic>.None;
			}

			if (ReferencesAnotherStatic(us, Context))
			{
				return Option<Diagnostic>.None;
			}

			// Hunt for evidence that this is a factory method
			foreach (ObjectCreationExpressionSyntax objectCreationExpressionSyntax in Node.DescendantNodesAndSelf().OfType<ObjectCreationExpressionSyntax>())
			{
				ISymbol objectCreationSymbol = Context.SemanticModel.GetSymbolInfo(objectCreationExpressionSyntax).Symbol;
				if (SymbolEqualityComparer.Default.Equals(objectCreationSymbol?.ContainingType, us))
				{
					return Option<Diagnostic>.None;
				}
			}

			// Check if this method is being used for DynamicData, if so, let it go
			var returnType = Node.ReturnType.ToString();
			if (string.Equals(returnType, "IEnumerable<object[]>", StringComparison.OrdinalIgnoreCase))
			{
				return Option<Diagnostic>.None;
			}

			Location location = Node.Modifiers.First(t => t.Kind() == SyntaxKind.StaticKeyword).GetLocation();
			return PrepareDiagnostic(location).ToSome();
		}

		private bool ReferencesAnotherStatic(INamedTypeSymbol us, SyntaxNodeAnalysisContext context)
		{
			foreach (IdentifierNameSyntax identifierNameSyntax in Node.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
			{
				ISymbol symbol = context.SemanticModel.GetSymbolInfo(identifierNameSyntax).Symbol;
				if (symbol == null)
				{
					continue;
				}

				// Look for a static thing being used in this method that is ours
				if (symbol.IsStatic && !symbol.IsExtern && SymbolEqualityComparer.Default.Equals(symbol.ContainingType, us))
				{
					// This method must be static because it references something static of ours.  We are done.
					return true;
				}
			}
			return false;
		}
	}
}
