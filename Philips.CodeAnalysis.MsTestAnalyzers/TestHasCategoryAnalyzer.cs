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

		private HashSet<string> _exceptions = new HashSet<string>();
		private HashSet<string> _allowedCategories = new HashSet<string>();

		protected override void OnInitializeAnalyzer(AnalyzerOptions options, Compilation compilation)
		{
			AdditionalFilesHelper helper = new AdditionalFilesHelper(options, compilation);

			_exceptions = helper.LoadExceptions(FileName);
			_allowedCategories = helper.GetValuesFromEditorConfig(Rule.Id, @"allowed_test_categories");
		}


		protected override void OnTestMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, bool isDataTestMethod)
		{
			SyntaxList<AttributeListSyntax> attributeLists = methodDeclaration.AttributeLists;

			if (methodDeclaration.Parent is ClassDeclarationSyntax classDeclaration)
			{
				if (_exceptions.Contains($"{classDeclaration.Identifier.Text}.{methodDeclaration.Identifier.Text}"))
					return;
			}

			if (!Helper.HasAttribute(attributeLists, context, MsTestFrameworkDefinitions.TestCategoryAttribute, out Location categoryLocation, out AttributeArgumentSyntax argumentSyntax))
			{
				Diagnostic diagnostic = Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation());
				context.ReportDiagnostic(diagnostic);
				return;
			}

			Optional<object> argument = context.SemanticModel.GetConstantValue(argumentSyntax.Expression);

			if (!argument.HasValue)
			{
				//this should not be possible.  Attribute values must by compile time constants
				return;
			}

			if (!_allowedCategories.Contains((string)argument.Value))
			{
				Diagnostic diagnostic = Diagnostic.Create(Rule, categoryLocation);
				context.ReportDiagnostic(diagnostic);
			}
		}
	}
}
