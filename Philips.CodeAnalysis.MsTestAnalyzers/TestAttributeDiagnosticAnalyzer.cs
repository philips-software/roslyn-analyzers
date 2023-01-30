using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using Philips.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.CSharp;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	public abstract class TestAttributeDiagnosticAnalyzer : DiagnosticAnalyzer
	{
		public abstract class Implementation
		{
			public virtual void OnTestAttributeMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol, HashSet<INamedTypeSymbol> presentAttributes) { }
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
				if (startContext.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.Assert") == null)
				{
					return;
				}

				MsTestAttributeDefinitions definitions = MsTestAttributeDefinitions.FromCompilation(startContext.Compilation);

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
			MethodDeclarationSyntax methodDeclaration = (MethodDeclarationSyntax)context.Node;

			ISymbol symbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);

			if (symbol is null || symbol is not IMethodSymbol methodSymbol)
			{
				return;
			}

			HashSet<INamedTypeSymbol> presentAttributes = new();
			foreach (AttributeData attribute in methodSymbol.GetAttributes())
			{
				if (definitions.NonTestMethods.Contains(attribute.AttributeClass))
				{
					presentAttributes.Add(attribute.AttributeClass);
				}

				if (attribute.AttributeClass.IsDerivedFrom(definitions.TestMethodSymbol))
				{
					presentAttributes.Add(attribute.AttributeClass);
				}
			}

			if (presentAttributes.Count > 0)
			{
				implementation.OnTestAttributeMethod(context, methodDeclaration, methodSymbol, presentAttributes);
			}
		}
	}

}
