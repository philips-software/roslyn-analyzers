// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidStaticMethodAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Avoid static Methods";
		public const string MessageFormat = @"Avoid static Methods when possible";
		private const string Description = @"Do not unnecessarily mark methods as static.";
		private const string Category = Categories.Maintainability;

		public DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.AvoidStaticMethods), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: false, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.MethodDeclaration);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			MethodDeclarationSyntax methodDeclarationSyntax = context.Node as MethodDeclarationSyntax;
			if (methodDeclarationSyntax == null)
			{
				return;
			}

			// Only analyzing static method declarations
			if (!methodDeclarationSyntax.Modifiers.Any(SyntaxKind.StaticKeyword))
			{
				return;
			}

			// If the method is marked "extern", let it go.
			if (methodDeclarationSyntax.Modifiers.Any(SyntaxKind.ExternKeyword))
			{
				return;
			}

			// If the class is static, we need to let it go.
			ClassDeclarationSyntax classDeclarationSyntax = context.Node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
			if (classDeclarationSyntax == null)
			{
				return;
			}
			if (classDeclarationSyntax.Modifiers.Any(SyntaxKind.StaticKeyword))
			{
				return;
			}

			// The Main entrypoint to the program must be static
			if (methodDeclarationSyntax.Identifier.ValueText == @"Main")
			{
				return;
			}

			// Hunt for static members
			INamedTypeSymbol us = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
			if (us == null)
			{
				return;
			}

			foreach (IdentifierNameSyntax identifierNameSyntax in methodDeclarationSyntax.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
			{
				ISymbol symbol = context.SemanticModel.GetSymbolInfo(identifierNameSyntax).Symbol;
				if (symbol == null)
				{
					continue;
				}
				if (symbol.IsStatic && !symbol.IsExtern)
				{
					// We found a static thing being used in this method.  Is the thing ours?
					if (SymbolEqualityComparer.Default.Equals(symbol.ContainingType, us))
					{
						// This method must be static because it references something static of ours.  We are done.
						return;
					}
				}
			}

			// Hunt for evidence that this is a factory method
			foreach (ObjectCreationExpressionSyntax objectCreationExpressionSyntax in methodDeclarationSyntax.DescendantNodesAndSelf().OfType<ObjectCreationExpressionSyntax>())
			{
				ISymbol objectCreationSymbol = context.SemanticModel.GetSymbolInfo(objectCreationExpressionSyntax).Symbol;
				if (SymbolEqualityComparer.Default.Equals(objectCreationSymbol?.ContainingType, us))
				{
					return;
				}
			}

			// Check if this method is being used for DynamicData, if so, let it go
			string returnType = methodDeclarationSyntax.ReturnType.ToString();
			if (string.Equals(returnType, "IEnumerable<object[]>", StringComparison.CurrentCultureIgnoreCase))
			{
				return;
			}

			Diagnostic diagnostic = Diagnostic.Create(Rule, methodDeclarationSyntax.Modifiers.First(t => t.Kind() == SyntaxKind.StaticKeyword).GetLocation());
			context.ReportDiagnostic(diagnostic);
		}
	}
}
