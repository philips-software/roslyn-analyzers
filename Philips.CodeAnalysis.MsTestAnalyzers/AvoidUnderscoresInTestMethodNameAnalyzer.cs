// © 2026 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidUnderscoresInTestMethodNameAnalyzer : TestMethodDiagnosticAnalyzer
	{
		private const string Title = @"Test method name must not contain underscores";
		public static readonly string MessageFormat = @"Test method '{0}' contains underscores. Use PascalCase instead.";
		private const string Description = @"Test method names should use PascalCase without underscores.";

		private static readonly DiagnosticDescriptor Rule = new(DiagnosticId.AvoidUnderscoresInTestMethodName.ToId(),
												Title, MessageFormat, Categories.Naming, DiagnosticSeverity.Error, isEnabledByDefault: false, description: Description, helpLinkUri: DiagnosticId.AvoidUnderscoresInTestMethodName.ToHelpLinkUrl());

		private static readonly ImmutableArray<DiagnosticDescriptor> _diagnostics = ImmutableArray.Create(Rule);
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _diagnostics;

		protected override TestMethodImplementation OnInitializeTestMethodAnalyzer(AnalyzerOptions options, Compilation compilation, MsTestAttributeDefinitions definitions)
		{
			return new AvoidUnderscoresInTestMethodName(definitions, Helper);
		}

		private sealed class AvoidUnderscoresInTestMethodName : TestMethodImplementation
		{
			public AvoidUnderscoresInTestMethodName(MsTestAttributeDefinitions definitions, Helper helper) : base(definitions, helper)
			{ }

			protected override void OnTestMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol, bool isDataTestMethod)
			{
				var name = methodDeclaration.Identifier.Text;
				if (name.Contains("_"))
				{
					Location location = methodDeclaration.Identifier.GetLocation();
					context.ReportDiagnostic(Diagnostic.Create(Rule, location, name));
				}
			}
		}
	}
}
