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
				TestCategorySymbol = compilation.GetTypeByMetadataName(MsTestFrameworkDefinitions.TestCategoryAttribute.FullName),
				ITestSourceSymbol = compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.ITestDataSource"),
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
				definitions.DynamicDataSymbol,
				definitions.TestCategorySymbol
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
		public INamedTypeSymbol ITestSourceSymbol { get; private set; }
		public INamedTypeSymbol TestCategorySymbol { get; private set; }

		public ImmutableHashSet<INamedTypeSymbol> NonTestMethods { get; private set; }
	}

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

			if (symbol is null || !(symbol is IMethodSymbol methodSymbol))
			{
				return;
			}

			HashSet<INamedTypeSymbol> presentAttributes = new HashSet<INamedTypeSymbol>();
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

	public abstract class TestMethodDiagnosticAnalyzer : TestAttributeDiagnosticAnalyzer
	{
		public TestMethodDiagnosticAnalyzer()
		{ }

		protected sealed override Implementation OnInitializeAnalyzer(AnalyzerOptions options, Compilation compilation, MsTestAttributeDefinitions definitions)
		{
			return OnInitializeTestMethodAnalyzer(options, compilation, definitions);
		}


		protected abstract TestMethodImplementation OnInitializeTestMethodAnalyzer(AnalyzerOptions options, Compilation compilation, MsTestAttributeDefinitions definitions);

		public abstract class TestMethodImplementation : Implementation
		{
			protected MsTestAttributeDefinitions Definitions { get; }

			public TestMethodImplementation(MsTestAttributeDefinitions definitions)
			{
				Definitions = definitions;
			}

			protected abstract void OnTestMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol, bool isDataTestMethod);

			public sealed override void OnTestAttributeMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol, HashSet<INamedTypeSymbol> presentAttributes)
			{
				bool isTestMethod = false;
				bool isDataTestMethod = false;

				foreach (INamedTypeSymbol attribute in presentAttributes)
				{
					isTestMethod = attribute.IsDerivedFrom(Definitions.TestMethodSymbol);
					isDataTestMethod = attribute.IsDerivedFrom(Definitions.DataTestMethodSymbol);

					if (isTestMethod || isDataTestMethod)
					{
						break;
					}
				}

				if (!isTestMethod && !isDataTestMethod)
				{
					return;
				}

				OnTestMethod(context, methodDeclaration, methodSymbol, isDataTestMethod);
			}
		}
	}
}
