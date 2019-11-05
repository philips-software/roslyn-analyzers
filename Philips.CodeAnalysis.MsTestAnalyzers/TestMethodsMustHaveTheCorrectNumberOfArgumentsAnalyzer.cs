// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class TestMethodsMustHaveTheCorrectNumberOfArgumentsAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"TestMethods/DataTestMethods must have the correct number of arguments";
		public static string MessageFormat = @"'{0}' has the wrong number of parameters";
		private const string Description = @"DataTestMethods should have the same number of parameters of the DataRows, TestMethods should have no arguments";
		private const string Category = Categories.Maintainability;

		private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.TestMethodsMustHaveTheCorrectNumberOfArguments),
												Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);


		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.MethodDeclaration);
		}

		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			MethodDeclarationSyntax methodDeclaration = (MethodDeclarationSyntax)context.Node;

			if (!Helper.IsTestMethod(methodDeclaration.AttributeLists, context, out bool isDataTestMethod))
			{
				return;
			}

			int expectedNumberOfParameters;
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

			if (expectedNumberOfParameters != methodDeclaration.ParameterList.Parameters.Count)
			{
				context.ReportDiagnostic(Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), methodDeclaration.Identifier));
			}
		}

		private static bool TryGetExpectedParameters(MethodDeclarationSyntax methodDeclaration, SyntaxNodeAnalysisContext context, out int expectedNumberOfParameters)
		{
			HashSet<int> dataRowParameters = new HashSet<int>();
			foreach (AttributeSyntax attribute in methodDeclaration.AttributeLists.SelectMany(x => x.Attributes))
			{
				if (!Helper.IsDataRowAttribute(attribute, context))
				{
					continue;
				}

				dataRowParameters.Add(attribute.ArgumentList.Arguments.Count);
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
