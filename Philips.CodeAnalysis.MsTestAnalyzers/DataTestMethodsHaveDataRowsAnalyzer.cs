// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class DataTestMethodsHaveDataRowsAnalyzer : TestMethodDiagnosticAnalyzer
	{
		private const string Title = @"DataTestMethods must have at least 1 DataRow or 1 DynamicData, TestMethods must have none";

		public static readonly string MessageFormatMismatchedCount = @"Test {0} has {1} DataRowAttributes and {2} DynamicDataAttributes.";

		public static readonly string MessageFormatIsTestMethod = @"TestMethod has parameterized input data.  Convert to DataTestMethod.";
		public static readonly string MessageFormatIsDataTestMethod = @"DataTestMethod has no input data.  Convert to TestMethod.";

		private const string Description = @"DataTestMethods are only executed with DataRows";
		private const string Category = Categories.MsTest;

		private static readonly DiagnosticDescriptor Rule = new(DiagnosticId.DataTestMethodsHaveDataRows.ToId(),
												Title, MessageFormatMismatchedCount, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);
		private static readonly DiagnosticDescriptor RuleShouldBeTestMethod = new(DiagnosticId.DataTestMethodsHaveDataRows.ToId(),
												Title, MessageFormatIsDataTestMethod, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);
		private static readonly DiagnosticDescriptor RuleShouldBeDataTestMethod = new(DiagnosticId.DataTestMethodsHaveDataRows.ToId(),
												Title, MessageFormatIsTestMethod, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		protected override TestMethodImplementation OnInitializeTestMethodAnalyzer(AnalyzerOptions options, Compilation compilation, MsTestAttributeDefinitions definitions)
		{
			return new DataTestMethodsHaveDataRowsImplementation(definitions);
		}
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, RuleShouldBeTestMethod, RuleShouldBeDataTestMethod);

		public class DataTestMethodsHaveDataRowsImplementation : TestMethodImplementation
		{
			public DataTestMethodsHaveDataRowsImplementation(MsTestAttributeDefinitions definitions) : base(definitions)
			{ }
			private void CollectSupportingData(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration,
												out int dynamicDataCount, out int dataRowCount, out bool hasTestSource)
			{
				dynamicDataCount = 0;
				dataRowCount = 0;
				hasTestSource = false;

				foreach (AttributeSyntax attribute in methodDeclaration.AttributeLists.SelectMany(x => x.Attributes))
				{
					if (AttributeHelper.IsDataRowAttribute(attribute, context))
					{
						dataRowCount++;
						continue;
					}

					if (AttributeHelper.IsAttribute(attribute, context, MsTestFrameworkDefinitions.DynamicDataAttribute, out _, out _))
					{
						dynamicDataCount++;
						continue;
					}

					SymbolInfo symbol = context.SemanticModel.GetSymbolInfo(attribute);
					if (symbol.Symbol is IMethodSymbol method && method.ContainingType.AllInterfaces.Contains(Definitions.ITestSourceSymbol))
					{
						hasTestSource = true;
					}
				}
			}

			protected override void OnTestMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol, bool isDataTestMethod)
			{
				CollectSupportingData(context, methodDeclaration, out var dynamicDataCount, out var dataRowCount, out var hasTestSource);

				if (isDataTestMethod)
				{
					if (dataRowCount != 0 && dynamicDataCount == 0)
					{
						return;
					}

					if (dataRowCount == 0 && dynamicDataCount == 1)
					{
						return;
					}

					Location location = methodDeclaration.Identifier.GetLocation();
					if (dataRowCount != 0 && dynamicDataCount != 0)
					{

						context.ReportDiagnostic(Diagnostic.Create(Rule, location, methodDeclaration.Identifier.ToString(), dataRowCount, dynamicDataCount));
					}
					else if (!hasTestSource)
					{
						context.ReportDiagnostic(Diagnostic.Create(RuleShouldBeTestMethod, location));
					}
				}
				else
				{
					if (dataRowCount == 0 && dynamicDataCount == 0)
					{
						return;
					}

					Location location = methodDeclaration.Identifier.GetLocation();
					context.ReportDiagnostic(Diagnostic.Create(RuleShouldBeDataTestMethod, location));
				}
			}
		}
	}
}
