// © 2026 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class EnforceUnderscoresInTestMethodNameAnalyzer : TestMethodDiagnosticAnalyzer
	{
		private const string Title = @"Test method name must contain underscores";
		public static readonly string MessageFormat = @"Test method '{0}' does not contain underscores.";
		private const string Description = @"Test method names should contain underscores to separate logical parts of the name.";

		private static readonly DiagnosticDescriptor Rule = new(DiagnosticId.EnforceUnderscoresInTestMethodName.ToId(),
												Title, MessageFormat, Categories.Naming, DiagnosticSeverity.Error, isEnabledByDefault: false, description: Description, helpLinkUri: DiagnosticId.EnforceUnderscoresInTestMethodName.ToHelpLinkUrl());

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		protected override TestMethodImplementation OnInitializeTestMethodAnalyzer(AnalyzerOptions options, Compilation compilation, MsTestAttributeDefinitions definitions)
		{
			return new EnforceUnderscoresInTestMethodName(definitions, Helper);
		}

		private sealed class EnforceUnderscoresInTestMethodName : TestMethodImplementation
		{
			public EnforceUnderscoresInTestMethodName(MsTestAttributeDefinitions definitions, Helper helper) : base(definitions, helper)
			{ }

			protected override void OnTestMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol, bool isDataTestMethod)
			{
				var name = methodDeclaration.Identifier.Text;
				if (!name.Contains("_"))
				{
					Location location = methodDeclaration.Identifier.GetLocation();
					context.ReportDiagnostic(Diagnostic.Create(Rule, location, name));
				}
			}
		}
	}
}
