// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class TestMethodsShouldHaveValidReturnTypesAnalyzer : TestMethodDiagnosticAnalyzer
	{
		private const string Title = @"TestMethods must return void or Task for async methods";
		public static string MessageFormat = @"Test method should return '{0}', actually returns '{1}'";
		private const string Description = @"MSTest will not run tests that return something other than void, or Task for async tests.";
		private const string Category = Categories.Maintainability;

		private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.TestMethodsMustHaveValidReturnType),
												Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);


		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		protected override TestMethodImplementation OnInitializeTestMethodAnalyzer(AnalyzerOptions options, Compilation compilation, MsTestAttributeDefinitions definitions) => new TestMethodsShouldHaveValidReturnTypes(compilation, definitions);

		private class TestMethodsShouldHaveValidReturnTypes : TestMethodImplementation
		{
			private readonly INamedTypeSymbol _taskSymbol;

			public TestMethodsShouldHaveValidReturnTypes(Compilation compilation, MsTestAttributeDefinitions definitions) : base(definitions)
			{
				_taskSymbol = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
			}

			protected override void OnTestMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol, bool isDataTestMethod)
			{
				if (!methodSymbol.IsAsync && methodSymbol.ReturnsVoid)
				{
					// not async, returns void.
					return;
				}

				if (!methodSymbol.IsAsync)
				{
					// error.  Not async, doesn't return void.
					context.ReportDiagnostic(Diagnostic.Create(Rule, methodDeclaration.ReturnType.GetLocation(), context.Compilation.GetSpecialType(SpecialType.System_Void).ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat), methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
					return;
				}

				// async method.  Must return Task or Task<T>
				if (_taskSymbol is null || SymbolEqualityComparer.Default.Equals(_taskSymbol, methodSymbol.ReturnType))
				{
					return;
				}

				context.ReportDiagnostic(Diagnostic.Create(Rule, methodDeclaration.ReturnType.GetLocation(), _taskSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat), methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
			}
		}
	}
}
