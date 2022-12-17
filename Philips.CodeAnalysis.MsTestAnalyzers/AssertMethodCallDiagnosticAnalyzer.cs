// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
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
			InvocationExpressionSyntax invocationExpression = (InvocationExpressionSyntax)context.Node;
			if (invocationExpression?.Expression is not MemberAccessExpressionSyntax memberAccessExpression)
			{
				return;
			}

			if (memberAccessExpression.Expression is IdentifierNameSyntax identifier && identifier.Identifier.Text.EndsWith("Assert"))
			{
				foreach (Diagnostic diagnostic in Analyze(context, invocationExpression, memberAccessExpression) ?? Array.Empty<Diagnostic>())
				{
					if ((context.SemanticModel.GetSymbolInfo(memberAccessExpression).Symbol is not IMethodSymbol memberSymbol) || !memberSymbol.ToString().StartsWith("Microsoft.VisualStudio.TestTools.UnitTesting.Assert"))
					{
						return;
					}

					context.ReportDiagnostic(diagnostic);
				}
			}
		}

		protected abstract IEnumerable<Diagnostic> Analyze(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocationExpressionSyntax, MemberAccessExpressionSyntax memberAccessExpression);

		#endregion

		#region Public Interface

		public sealed override void Initialize(AnalysisContext context)
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
