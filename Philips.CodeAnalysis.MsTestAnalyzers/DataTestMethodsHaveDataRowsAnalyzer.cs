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

		public static string MessageFormatMismatchedCount = @"Test {0} has {1} DataRowAttributes and {2} DynamicDataAttributes.";

		public static string MessageFormatIsTestMethod = @"TestMethod has parameterized input data.  Convert to DataTestMethod.";
		public static string MessageFormatIsDataTestMethod = @"DataTestMethod has no input data.  Convert to TestMethod.";

		private const string Description = @"DataTestMethods are only executed with DataRows";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.DataTestMethodsHaveDataRows),
												Title, MessageFormatMismatchedCount, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);
		private static readonly DiagnosticDescriptor RuleShouldBeTestMethod = new(Helper.ToDiagnosticId(DiagnosticIds.DataTestMethodsHaveDataRows),
												Title, MessageFormatIsDataTestMethod, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);
		private static readonly DiagnosticDescriptor RuleShouldBeDataTestMethod = new(Helper.ToDiagnosticId(DiagnosticIds.DataTestMethodsHaveDataRows),
												Title, MessageFormatIsTestMethod, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		protected override TestMethodImplementation OnInitializeTestMethodAnalyzer(AnalyzerOptions options, Compilation compilation, MsTestAttributeDefinitions definitions)
		{
			return new DataTestMethodsHaveDataRowsImplementation(definitions);
		}
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule, RuleShouldBeTestMethod, RuleShouldBeDataTestMethod); } }

		public class DataTestMethodsHaveDataRowsImplementation : TestMethodImplementation
		{
			public DataTestMethodsHaveDataRowsImplementation(MsTestAttributeDefinitions definitions) : base(definitions)
			{ }

			protected override void OnTestMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol, bool isDataTestMethod)
			{
				int dynamicDataCount = 0;
				int dataRowCount = 0;
				bool hasTestSource = false;
				foreach (AttributeSyntax attribute in methodDeclaration.AttributeLists.SelectMany(x => x.Attributes))
				{
					if (Helper.IsDataRowAttribute(attribute, context))
					{
						dataRowCount++;
						continue;
					}

					if (Helper.IsAttribute(attribute, context, MsTestFrameworkDefinitions.DynamicDataAttribute, out _, out _))
					{
						dynamicDataCount++;
						continue;
					}

					SymbolInfo symbol = context.SemanticModel.GetSymbolInfo(attribute);
					if (symbol.Symbol != null && symbol.Symbol is IMethodSymbol method)
					{
						if (method.ContainingType.AllInterfaces.Contains(Definitions.ITestSourceSymbol))
						{
							hasTestSource = true;
						}
					}
				}

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

					if (dataRowCount != 0 && dynamicDataCount != 0)
					{

						context.ReportDiagnostic(Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), methodDeclaration.Identifier.ToString(), dataRowCount, dynamicDataCount));
					}
					else if (!hasTestSource)
					{
						context.ReportDiagnostic(Diagnostic.Create(RuleShouldBeTestMethod, methodDeclaration.Identifier.GetLocation()));
					}
				}
				else
				{
					if (dataRowCount == 0 && dynamicDataCount == 0)
					{
						return;
					}

					context.ReportDiagnostic(Diagnostic.Create(RuleShouldBeDataTestMethod, methodDeclaration.Identifier.GetLocation()));
				}
			}
		}
	}
}
