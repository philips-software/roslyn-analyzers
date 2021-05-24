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
	public class NoEmptyTestMethodsDiagnosticAnalyzer : TestAttributeDiagnosticAnalyzer
	{
		private const string Title = @"No Test Methods";
		public const string MessageFormat = @"Remove empty test method '{0}'";
		private const string Description = @"Remove empty test method '{0}'";
		private const string Category = Categories.Maintainability;

		public DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.TestMethodsMustNotBeEmpty), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }


		protected override void OnTestAttributeMethod(SyntaxNodeAnalysisContext context, MsTestAttributeDefinitions attributes, (MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol) methodInfo, HashSet<INamedTypeSymbol> presentAttributes)
		{
			MethodDeclarationSyntax methodDeclaration = methodInfo.methodDeclaration;
			if (methodDeclaration.Body == null)
			{
				//during the intellisense phase the body of a method can be non-existent.
				return;
			}

			if (methodDeclaration.Body.Statements.Any())
			{
				//not empty
				return;
			}

			context.ReportDiagnostic(Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), methodDeclaration.Identifier));
		}
	}
}
