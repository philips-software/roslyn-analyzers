// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AssertIsTrueParenthesisAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Assert.IsTrue/IsFalse Should not be in parenthesis";
		private const string MessageFormat = @"Do not call IsTrue/IsFalse with parenthesis around the argument";
		private const string Description = @"Assert.IsTrue((<actual> == <expected>)) => Assert.IsTrue(<expected> == <actual>)";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticId.AssertIsTrueParenthesis), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();

			context.RegisterCompilationStartAction(startContext =>
			{
				if (startContext.Compilation.GetTypeByMetadataName(StringConstants.AssertFullyQualifiedName) == null)
				{
					return;
				}

				startContext.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
			});
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			InvocationExpressionSyntax invocationExpression = (InvocationExpressionSyntax)context.Node;
			if (invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccessExpression)
			{
				return;
			}

			string memberName = memberAccessExpression.Name.ToString();
			if (memberName is not "IsTrue" and not "IsFalse")
			{
				return;
			}

			if (invocationExpression.ArgumentList.Arguments.Count == 0)
			{
				return;
			}

			ArgumentSyntax arg0 = invocationExpression.ArgumentList.Arguments[0];

			if (arg0.Expression.Kind() == SyntaxKind.ParenthesizedExpression)
			{
				context.ReportDiagnostic(Diagnostic.Create(Rule, arg0.GetLocation()));
			}
		}
	}
}
