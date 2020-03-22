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
	public class TestHasCategoryAttributeAnalyzer : TestMethodDiagnosticAnalyzer
	{
		public const string FileName = @"TestsWithUnsupportedCategory.Allowed.txt";
		private const string Title = @"Test must have an appropriate TestCategory";
		public const string MessageFormat = @"Test must have an appropriate TestCategory attribute. Check EditorConfig";
		private const string Description = @"Tests are required to have an appropriate TestCategory to allow running tests category wise.";
		private const string Category = Categories.Maintainability;

		private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.TestHasCategoryAttribute), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }


		protected override void OnTestMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, bool isDataTestMethod)
		{
			SyntaxList<AttributeListSyntax> attributeLists = methodDeclaration.AttributeLists;

			Location categoryLocation;


			ClassDeclarationSyntax classDeclaration = methodDeclaration.Parent as ClassDeclarationSyntax;
			if (classDeclaration != null)
			{
				var exceptions = AdditionalFilesHelper.LoadExceptions(FileName);
				if (exceptions.Contains($"{classDeclaration.Identifier.Text}.{methodDeclaration.Identifier.Text}"))
					return;
			}

			if (Helper.HasAttribute(attributeLists, context, MsTestFrameworkDefinitions.TestCategoryAttribute, out categoryLocation, out string category))
			{
				List<string> allowedCategories = AdditionalFilesHelper.GetValuesFromEditorConfig(Rule.Id, @"allowed_test_categories");
				if (!allowedCategories.Contains(category.Replace("\"", string.Empty)))
				{
					Diagnostic diagnostic = Diagnostic.Create(Rule, categoryLocation);
					context.ReportDiagnostic(diagnostic);
				}
			}
			else
			{
				Diagnostic diagnostic = Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation());
				context.ReportDiagnostic(diagnostic);
			}
		}
	}
}
