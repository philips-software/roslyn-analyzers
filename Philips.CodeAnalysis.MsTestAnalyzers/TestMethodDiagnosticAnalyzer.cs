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
	public abstract class TestMethodDiagnosticAnalyzer : TestAttributeDiagnosticAnalyzer
	{
		protected AttributeHelper AttributeHelper { get; private set; }

		public TestMethodDiagnosticAnalyzer()
			: this(new AttributeHelper())
		{ }

		public TestMethodDiagnosticAnalyzer(AttributeHelper attributeHelper)
		{
			AttributeHelper = attributeHelper;
		}

		protected sealed override Implementation OnInitializeAnalyzer(AnalyzerOptions options, Compilation compilation, MsTestAttributeDefinitions definitions)
		{
			return OnInitializeTestMethodAnalyzer(options, compilation, definitions);
		}


		protected abstract TestMethodImplementation OnInitializeTestMethodAnalyzer(AnalyzerOptions options, Compilation compilation, MsTestAttributeDefinitions definitions);

		public abstract class TestMethodImplementation : Implementation
		{
			protected AttributeHelper AttributeHelper { get; private set; }

			protected MsTestAttributeDefinitions Definitions { get; }

			protected TestMethodImplementation(MsTestAttributeDefinitions definitions)
				: this(definitions, new AttributeHelper())
			{ }
			protected TestMethodImplementation(MsTestAttributeDefinitions definitions, AttributeHelper attributeHelper)
			{
				Definitions = definitions;
				AttributeHelper = attributeHelper;
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
