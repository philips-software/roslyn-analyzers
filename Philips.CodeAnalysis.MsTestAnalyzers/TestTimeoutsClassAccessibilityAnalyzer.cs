// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class TestTimeoutsClassAccessibilityAnalyzer : SingleDiagnosticAnalyzer
	{
		private const string Title = @"TestTimeouts class should be internal";
		private const string MessageFormat = @"Class 'TestTimeouts' should be declared as internal";
		private const string Description = @"TestTimeouts classes should be internal to avoid accessibility issues";

		public TestTimeoutsClassAccessibilityAnalyzer()
			: base(DiagnosticId.TestTimeoutsClassShouldBeInternal, Title, MessageFormat, Description, Categories.MsTest, isEnabled: true)
		{ }

		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
		}

		private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
		{
			if (Helper.ForGeneratedCode.IsGeneratedCode(context))
			{
				return;
			}

			var classDeclaration = (ClassDeclarationSyntax)context.Node;

			// Check if the class is named "TestTimeouts"
			if (classDeclaration.Identifier.ValueText != "TestTimeouts")
			{
				return;
			}

			SyntaxTokenList modifiers = classDeclaration.Modifiers;

			// Check if the class has problematic modifiers:
			// 1. public (with or without sealed)
			// 2. internal static
			var hasPublic = modifiers.Any(SyntaxKind.PublicKeyword);
			var hasInternal = modifiers.Any(SyntaxKind.InternalKeyword);
			var hasStatic = modifiers.Any(SyntaxKind.StaticKeyword);

			var needsFix = hasPublic || (hasInternal && hasStatic);

			if (needsFix)
			{
				Location location = classDeclaration.Identifier.GetLocation();
				var diagnostic = Diagnostic.Create(Rule, location);
				context.ReportDiagnostic(diagnostic);
			}
		}
	}
}
