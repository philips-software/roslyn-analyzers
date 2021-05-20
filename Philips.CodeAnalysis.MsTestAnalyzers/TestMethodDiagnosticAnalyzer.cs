// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	public class MsTestAttributeDefinitions
	{
		public static MsTestAttributeDefinitions FromCompilation(Compilation compilation)
		{
			MsTestAttributeDefinitions definitions = new MsTestAttributeDefinitions()
			{
				TestMethodSymbol = compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.TestMethodAttribute.FullName),
				DataTestMethodSymbol = compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.DataTestMethodAttribute.FullName),
				TestClassSymbol = compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.TestClassAttribute.FullName),
				ClassInitializeSymbol = compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.ClassInitializeAttribute.FullName),
				ClassCleanupSymbol = compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.ClassCleanupAttribute.FullName),
				AssemblyInitializeSymbol = compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.AssemblyInitializeAttribute.FullName),
				AssemblyCleanupSymbol = compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.AssemblyCleanupAttribute.FullName),
				TestInitializeSymbol = compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.TestInitializeAttribute.FullName),
				TestCleanupSymbol = compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.TestCleanupAttribute.FullName),
				DataRowSymbol = compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.DataRowAttribute.FullName),
				DynamicDataSymbol = compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.DynamicDataAttribute.FullName),
			};

			definitions.NonTestMethods = ImmutableHashSet.Create<INamedTypeSymbol>(SymbolEqualityComparer.Default,
				definitions.TestClassSymbol,
				definitions.ClassInitializeSymbol,
				definitions.ClassCleanupSymbol,
				definitions.AssemblyCleanupSymbol,
				definitions.AssemblyInitializeSymbol,
				definitions.TestInitializeSymbol,
				definitions.TestCleanupSymbol,
				definitions.DataRowSymbol,
				definitions.DynamicDataSymbol
			);

			return definitions;
		}

		private MsTestAttributeDefinitions() { }

		public INamedTypeSymbol TestMethodSymbol { get; private set; }
		public INamedTypeSymbol DataTestMethodSymbol { get; private set; }
		public INamedTypeSymbol TestClassSymbol { get; private set; }
		public INamedTypeSymbol ClassInitializeSymbol { get; private set; }
		public INamedTypeSymbol ClassCleanupSymbol { get; private set; }
		public INamedTypeSymbol AssemblyInitializeSymbol { get; private set; }
		public INamedTypeSymbol AssemblyCleanupSymbol { get; private set; }
		public INamedTypeSymbol TestInitializeSymbol { get; private set; }
		public INamedTypeSymbol TestCleanupSymbol { get; private set; }
		public INamedTypeSymbol DataRowSymbol { get; private set; }
		public INamedTypeSymbol DynamicDataSymbol { get; private set; }

		public ImmutableHashSet<INamedTypeSymbol> NonTestMethods { get; private set; }
	}

	public abstract class TestAttributeDiagnosticAnalyzer : DiagnosticAnalyzer
	{
		protected TestAttributeDiagnosticAnalyzer()
		{ }

		protected virtual void OnInitializeAnalyzer(AnalyzerOptions options, Compilation compilation) { }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterCompilationStartAction(startContext =>
			{
				if (startContext.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.Assert") == null)
				{
					return;
				}

				OnInitializeAnalyzer(startContext.Options, startContext.Compilation);

				MsTestAttributeDefinitions definitions = MsTestAttributeDefinitions.FromCompilation(startContext.Compilation);

				startContext.RegisterSyntaxNodeAction((x) => Analyze(definitions, x), SyntaxKind.MethodDeclaration);
			});
		}

		protected abstract void OnTestAttributeMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, MsTestAttributeDefinitions attributes, HashSet<INamedTypeSymbol> presentAttributes);

		private void Analyze(MsTestAttributeDefinitions definitions, SyntaxNodeAnalysisContext context)
		{
			MethodDeclarationSyntax methodDeclaration = (MethodDeclarationSyntax)context.Node;

			ISymbol symbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);

			if (symbol is null)
			{
				return;
			}

			HashSet<INamedTypeSymbol> presentAttributes = new HashSet<INamedTypeSymbol>();
			foreach (AttributeData attribute in symbol.GetAttributes())
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
				OnTestAttributeMethod(context, methodDeclaration, definitions, presentAttributes);
			}
		}
	}

	public abstract class TestMethodDiagnosticAnalyzer : TestAttributeDiagnosticAnalyzer
	{
		public TestMethodDiagnosticAnalyzer()
		{ }

		protected abstract void OnTestMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, bool isDataTestMethod);

		protected override void OnTestAttributeMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, MsTestAttributeDefinitions attributes, HashSet<INamedTypeSymbol> presentAttributes)
		{
			bool isTestMethod = false;
			bool isDataTestMethod = false;

			foreach (INamedTypeSymbol attribute in presentAttributes)
			{
				isTestMethod = attribute.IsDerivedFrom(attributes.TestMethodSymbol);
				isDataTestMethod = attribute.IsDerivedFrom(attributes.DataTestMethodSymbol);

				if (isTestMethod || isDataTestMethod)
				{
					break;
				}
			}

			if (!isTestMethod && !isDataTestMethod)
			{
				return;
			}

			OnTestMethod(context, methodDeclaration, isDataTestMethod);
		}
	}
}
