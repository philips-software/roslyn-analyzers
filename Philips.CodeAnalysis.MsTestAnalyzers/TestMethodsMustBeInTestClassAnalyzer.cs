// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class TestMethodsMustBeInTestClassAnalyzer : TestAttributeDiagnosticAnalyzer
	{
		private const string Title = @"TestMethods/DataTestMethods must be in [TestClass]";
		public static readonly string MessageFormat = @"{0} is not in a [TestClass]";
		private const string Description = @"Tests are only executed if they are [TestClass]";
		private const string Category = Categories.MsTest;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticId.TestMethodsMustBeInTestClass),
												Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);


		public TestMethodsMustBeInTestClassAnalyzer()
		{ }

		protected override Implementation OnInitializeAnalyzer(AnalyzerOptions options, Compilation compilation, MsTestAttributeDefinitions definitions)
		{
			return new TestMethodsMustBeInTestClass();
		}

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		private sealed class TestMethodsMustBeInTestClass : Implementation
		{
			public override void OnTestAttributeMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol, HashSet<INamedTypeSymbol> presentAttributes)
			{
				TestHelper testHelper = new();
				if (testHelper.IsInTestClass(context))
				{
					return;
				}

				ISymbol symbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);

				if (symbol != null && symbol.ContainingType.IsAbstract)
				{
					return;
				}

				Location location = methodDeclaration.Identifier.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(Rule, location, methodDeclaration.Identifier));
			}
		}
	}
}
