// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	public abstract class TestClassDiagnosticAnalyzer : DiagnosticAnalyzerBase
	{
		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration);
		}

		protected abstract void OnTestClass(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax classDeclaration);

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			var classDeclaration = (ClassDeclarationSyntax)context.Node;

			if (!Helper.ForTests.IsTestClass(classDeclaration, context))
			{
				return;
			}

			OnTestClass(context, classDeclaration);
		}
	}
}
