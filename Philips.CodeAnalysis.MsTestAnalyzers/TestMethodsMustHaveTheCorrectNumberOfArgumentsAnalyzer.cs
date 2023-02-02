// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class TestMethodsMustHaveTheCorrectNumberOfArgumentsAnalyzer : TestMethodDiagnosticAnalyzer
	{
		private const string Title = @"TestMethods/DataTestMethods must have the correct number of arguments";
		public static readonly string MessageFormat = @"'{0}' has the wrong number of parameters";
		private const string Description = @"DataTestMethods should have the same number of parameters of the DataRows, TestMethods should have no arguments";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticId.TestMethodsMustHaveTheCorrectNumberOfArguments),
												Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);


		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }


		protected override TestMethodImplementation OnInitializeTestMethodAnalyzer(AnalyzerOptions options, Compilation compilation, MsTestAttributeDefinitions definitions)
		{
			return new TestMethodsMustHaveTheCorrectNumberOfArguments(definitions);
		}

		private sealed class TestMethodsMustHaveTheCorrectNumberOfArguments : TestMethodImplementation
		{
			public TestMethodsMustHaveTheCorrectNumberOfArguments(MsTestAttributeDefinitions definitions) : base(definitions)
			{ }

			protected override void OnTestMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol, bool isDataTestMethod)
			{
				int? expectedNumberOfParameters;
				if (!isDataTestMethod)
				{
					expectedNumberOfParameters = 0;
				}
				else
				{
					if (!TryGetExpectedParameters(methodDeclaration, context, out expectedNumberOfParameters))
					{
						context.ReportDiagnostic(Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), methodDeclaration.Identifier));
						return;
					}
				}

				if (expectedNumberOfParameters != null && expectedNumberOfParameters != methodDeclaration.ParameterList.Parameters.Count)
				{
					context.ReportDiagnostic(Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), methodDeclaration.Identifier));
				}
			}

			private void CollectSupportingData(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, 
												out bool anyCustomDataSources, out bool anyDynamicData, out HashSet<int> dataRowParameters)
			{
				anyCustomDataSources = false;
				anyDynamicData = false;
				dataRowParameters = new();
				foreach (AttributeSyntax attribute in methodDeclaration.AttributeLists.SelectMany(x => x.Attributes))
				{
					if (AttributeHelper.IsDataRowAttribute(attribute, context))
					{
						int argumentCount = attribute.ArgumentList.Arguments.Count((arg) => { return arg.NameEquals?.Name.Identifier.ValueText != @"DisplayName"; });
						dataRowParameters.Add(argumentCount);
						continue;
					}

					if (AttributeHelper.IsAttribute(attribute, context, MsTestFrameworkDefinitions.DynamicDataAttribute, out _, out _))
					{
						anyDynamicData = true;
						continue;
					}

					var symbol = context.SemanticModel.GetSymbolInfo(attribute);
					if (symbol.Symbol is IMethodSymbol method)
					{
						if (method.ContainingType.AllInterfaces.Contains(Definitions.ITestSourceSymbol))
						{
							anyCustomDataSources = true;
						}

						continue;
					}
				}
			}

			private bool TryGetExpectedParameters(MethodDeclarationSyntax methodDeclaration, SyntaxNodeAnalysisContext context, out int? expectedNumberOfParameters)
			{
				CollectSupportingData(context, methodDeclaration, out bool anyCustomDataSources, out bool anyDynamicData, out HashSet<int> dataRowParameters);

				if (anyDynamicData || anyCustomDataSources)
				{
					expectedNumberOfParameters = null;

					return dataRowParameters.Count == 0;
				}

				if (dataRowParameters.Count == 0)
				{
					expectedNumberOfParameters = 0;
					return true;
				}

				if (dataRowParameters.Count != 1)
				{
					expectedNumberOfParameters = 0;
					return false;
				}

				expectedNumberOfParameters = dataRowParameters.First();
				return true;
			}
		}
	}
}
