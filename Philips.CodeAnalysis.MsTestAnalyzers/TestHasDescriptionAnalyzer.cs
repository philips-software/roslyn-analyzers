// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class TestHasDescriptionAnalyzer : TestMethodDiagnosticAnalyzer
	{
		private const string Title = @"Test must have proper Description Attribute value";
		public const string MessageFormat = @"Test Description Attribute must not have a literal string and length of the reference value should be less than 25 characters.";
		private const string Description = @"Test Description Attribute must not have a literal string and length of the reference value should be less than 25 characters.";
		private const string Category = Categories.Maintainability;

		private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.AvoidDescriptionAttribute), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		protected override TestMethodImplementation OnInitializeTestMethodAnalyzer(AnalyzerOptions options, Compilation compilation, MsTestAttributeDefinitions definitions) => new TestHasDescription(definitions);

		public class TestHasDescription : TestMethodImplementation
		{
			public TestHasDescription(MsTestAttributeDefinitions definitions) : base(definitions)
			{ }

			protected override void OnTestMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol, bool isDataTestMethod)
			{
				if (!Helper.HasAttribute(methodDeclaration.AttributeLists, context, MsTestFrameworkDefinitions.DescriptionAttribute, out Location location, out AttributeArgumentSyntax argument))
				{
					return;
				}

				string descriptionName = argument.ToString();
				string value = context.SemanticModel.GetConstantValue(argument.Expression).Value.ToString();
				if (descriptionName.Contains("\"") || value.Length > 25)
				{
					Diagnostic diagnostic = Diagnostic.Create(Rule, location);
					context.ReportDiagnostic(diagnostic);
				}
			}
		}
	}
}