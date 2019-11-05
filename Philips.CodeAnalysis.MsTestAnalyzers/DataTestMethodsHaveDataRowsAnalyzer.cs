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
		private const string Title = @"DataTestMethods must have at least 1 DataRow, TestMethods must have none";
		public static string MessageFormat = @"Test {0} has {1} DataRowAttributes.";
		private const string Description = @"DataTestMethods are only executed with DataRows";
		private const string Category = Categories.Maintainability;

		private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.DataTestMethodsHaveDataRows),
												Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);


		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		protected override void OnTestMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, bool isDataTestMethod)
		{
			int dataRowCount = 0;
			foreach (AttributeSyntax attribute in methodDeclaration.AttributeLists.SelectMany(x => x.Attributes))
			{
				if (Helper.IsDataRowAttribute(attribute, context))
				{
					dataRowCount++;
				}
			}

			if (isDataTestMethod)
			{

				if (dataRowCount != 0)
				{
					return;
				}
			}
			else
			{
				if (dataRowCount == 0)
				{
					return;
				}
			}

			context.ReportDiagnostic(Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), methodDeclaration.Identifier.ToString(), dataRowCount));
		}
	}
}
