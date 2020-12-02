// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	public abstract class AssertMethodCallDiagnosticAnalyzer : DiagnosticAnalyzer
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			Diagnostic diagnostic = null;
			InvocationExpressionSyntax invocationExpression = (InvocationExpressionSyntax)context.Node;
			MemberAccessExpressionSyntax memberAccessExpression = invocationExpression?.Expression as MemberAccessExpressionSyntax;
			if (memberAccessExpression == null)
			{
				return;
			}

			if (memberAccessExpression.Expression is IdentifierNameSyntax identifier && identifier.Identifier.Text.EndsWith("Assert"))
			{
				diagnostic = Analyze(context, invocationExpression, memberAccessExpression);
			}

			if (diagnostic != null)
			{
				IMethodSymbol memberSymbol = context.SemanticModel.GetSymbolInfo(memberAccessExpression).Symbol as IMethodSymbol;
				if ((memberSymbol == null) || !memberSymbol.ToString().StartsWith("Microsoft.VisualStudio.TestTools.UnitTesting.Assert"))
				{
					return;
				}
				context.ReportDiagnostic(diagnostic);
			}

		}

		protected abstract Diagnostic Analyze(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocationExpressionSyntax, MemberAccessExpressionSyntax memberAccessExpression);

		#endregion

		#region Public Interface

		public override sealed void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();

			context.RegisterCompilationStartAction(startContext =>
			{
				if (startContext.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.Assert") == null)
				{
					return;
				}

				startContext.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
			});
		}


		#endregion
	}
}
