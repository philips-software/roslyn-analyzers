// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class TestMethodsMustBePublicAnalyzer : TestMethodDiagnosticAnalyzer
	{
		private const string Title = @"TestMethods/DataTestMethods must be public, instance methods";
		public static string MessageFormat = @"'{0}' is not a public instance method";
		private const string Description = @"";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.TestMethodsMustBePublic),
												Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);


		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		protected override TestMethodImplementation OnInitializeTestMethodAnalyzer(AnalyzerOptions options, Compilation compilation, MsTestAttributeDefinitions definitions) => new TestMethodsMustBePublic(definitions);

		private class TestMethodsMustBePublic : TestMethodImplementation
		{
			public TestMethodsMustBePublic(MsTestAttributeDefinitions definitions) : base(definitions)
			{ }

			protected override void OnTestMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol, bool isDataTestMethod)
			{
				if (!methodDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword) && methodDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword))
				{
					return;
				}

				context.ReportDiagnostic(Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), methodDeclaration.Identifier));
			}
		}
	}
}
