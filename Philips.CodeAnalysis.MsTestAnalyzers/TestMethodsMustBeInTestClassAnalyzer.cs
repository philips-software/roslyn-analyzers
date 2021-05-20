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
	public class TestMethodsMustBeInTestClassAnalyzer : TestAttributeDiagnosticAnalyzer
	{
		private const string Title = @"TestMethods/DataTestMethods must be in [TestClass]";
		public static string MessageFormat = @"{0} is not in a [TestClass]";
		private const string Description = @"Tests are only executed if they are [TestClass]";
		private const string Category = Categories.Maintainability;

		private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.TestMethodsMustBeInTestClass),
												Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);


		public TestMethodsMustBeInTestClassAnalyzer()
		{ }

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		protected override void OnTestAttributeMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, MsTestAttributeDefinitions attributes, HashSet<INamedTypeSymbol> presentAttributes)
		{
			if (Helper.IsInTestClass(context))
			{
				return;
			}

			var symbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);

			if (symbol != null && symbol.ContainingType.IsAbstract)
			{
				return;
			}

			context.ReportDiagnostic(Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), methodDeclaration.Identifier));
		}
	}
}
