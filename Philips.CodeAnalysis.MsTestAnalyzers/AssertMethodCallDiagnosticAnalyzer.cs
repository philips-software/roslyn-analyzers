// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	public abstract class AssertMethodCallDiagnosticAnalyzer : DiagnosticAnalyzer
	{
		protected Helper Helper { get; set; }

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			var invocationExpression = (InvocationExpressionSyntax)context.Node;
			if (invocationExpression?.Expression is not MemberAccessExpressionSyntax memberAccessExpression)
			{
				return;
			}

			if (memberAccessExpression.Expression is IdentifierNameSyntax identifier && identifier.Identifier.Text.EndsWith(StringConstants.Assert))
			{
				foreach (Diagnostic diagnostic in Analyze(context, invocationExpression, memberAccessExpression))
				{
					if ((context.SemanticModel.GetSymbolInfo(memberAccessExpression).Symbol is not IMethodSymbol memberSymbol) || !memberSymbol.ToString().StartsWith(StringConstants.AssertFullyQualifiedName))
					{
						return;
					}

					context.ReportDiagnostic(diagnostic);
				}
			}
		}
		protected abstract IEnumerable<Diagnostic> Analyze(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocationExpressionSyntax, MemberAccessExpressionSyntax memberAccessExpression);


		public sealed override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();

			context.RegisterCompilationStartAction(startContext =>
			{
				if (startContext.Compilation.GetTypeByMetadataName(StringConstants.AssertFullyQualifiedName) == null)
				{
					return;
				}

				Helper = new Helper(startContext.Options, startContext.Compilation);
				startContext.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
			});
		}
	}
}
