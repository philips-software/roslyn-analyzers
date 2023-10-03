// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	public abstract class TestAttributeDiagnosticAnalyzer : DiagnosticAnalyzer
	{
		public abstract class Implementation
		{
			public virtual void OnTestAttributeMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol, IReadOnlyCollection<INamedTypeSymbol> presentAttributes) { }
		}

		protected TestAttributeDiagnosticAnalyzer()
		{ }

		protected abstract Implementation OnInitializeAnalyzer(AnalyzerOptions options, Compilation compilation, MsTestAttributeDefinitions definitions);

		public sealed override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterCompilationStartAction(startContext =>
			{
				if (startContext.Compilation.GetTypeByMetadataName(StringConstants.AssertFullyQualifiedName) == null)
				{
					return;
				}

				var definitions = MsTestAttributeDefinitions.FromCompilation(startContext.Compilation);

				Implementation implementation = OnInitializeAnalyzer(startContext.Options, startContext.Compilation, definitions);

				if (implementation is null)
				{
					return;
				}

				startContext.RegisterSyntaxNodeAction((x) => Analyze(definitions, x, implementation), SyntaxKind.MethodDeclaration);
			});
		}

		private void Analyze(MsTestAttributeDefinitions definitions, SyntaxNodeAnalysisContext context, Implementation implementation)
		{
			var methodDeclaration = (MethodDeclarationSyntax)context.Node;

			ISymbol symbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);

			if (symbol is not IMethodSymbol methodSymbol)
			{
				return;
			}

			HashSet<INamedTypeSymbol> presentAttributes = new();
			foreach (INamedTypeSymbol attribute in methodSymbol.GetAttributes().Select(attribute => attribute.AttributeClass))
			{
				if (definitions.NonTestMethods.Contains(attribute))
				{
					_ = presentAttributes.Add(attribute);
				}

				if (attribute.IsDerivedFrom(definitions.TestMethodSymbol))
				{
					_ = presentAttributes.Add(attribute);
				}
			}

			if (presentAttributes.Count > 0)
			{
				implementation.OnTestAttributeMethod(context, methodDeclaration, methodSymbol, presentAttributes);
			}
		}
	}

}
