// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidMultipleLambdasOnSingleLineAnalyzer : SingleDiagnosticAnalyzer
	{
		private const string Title = @"Avoid putting multiple Lambda expressions on the same line";
		public const string MessageFormat = Title;
		private const string Description = @"Avoid putting multiple Lambda expressions on the same line. To improve readability, split them into separate lines.";

		public AvoidMultipleLambdasOnSingleLineAnalyzer()
			: base(DiagnosticId.AvoidMultipleLambdasOnSingleLine, Title, MessageFormat, Description, Categories.Readability, isEnabled: false)
		{ }

		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.SimpleLambdaExpression);
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ParenthesizedLambdaExpression);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			var node = (LambdaExpressionSyntax)context.Node;
			MethodDeclarationSyntax parent =
				node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();

			// We need at least 2 lambdas in the same method to have even the possibility of a violation.
			IEnumerable<LambdaExpressionSyntax> lambdas = parent?.DescendantNodes().OfType<LambdaExpressionSyntax>();
			if (lambdas == null || lambdas.Count() < 2)
			{
				return;
			}

			// Get a list of the lambdas that start on the same line, excluding our lambda.
			IEnumerable<LambdaExpressionSyntax> lambdasOnSameLine = FindOtherLambdasOnSameLine(node, lambdas);
			if (lambdasOnSameLine == null || !lambdasOnSameLine.Any())
			{
				return;
			}

			// Do not trigger a diagnostic on the first lambda on the line.
			if (IsLeftMost(node, lambdasOnSameLine))
			{
				return;
			}

			Location loc = node.GetLocation();
			context.ReportDiagnostic(Diagnostic.Create(Rule, loc));
		}

		private static IEnumerable<LambdaExpressionSyntax> FindOtherLambdasOnSameLine(LambdaExpressionSyntax ourLambda, IEnumerable<LambdaExpressionSyntax> lambdas)
		{
			List<LambdaExpressionSyntax> result = new();
			var theLine = ourLambda.GetLocation().GetLineSpan().StartLinePosition.Line;
			foreach (LambdaExpressionSyntax lambda in lambdas)
			{
				var currentLine = lambda.GetLocation().GetLineSpan().StartLinePosition.Line;
				// Do not report ourLambda itself.
				if (currentLine == theLine && !ReferenceEquals(lambda, ourLambda))
				{
					result.Add(lambda);
				}
			}
			return result;
		}

		private static bool IsLeftMost(LambdaExpressionSyntax ourLambda, IEnumerable<LambdaExpressionSyntax> lambdas)
		{
			// ourLambda is the left most if all the lambdas are further to the right, which means a higher Character number.
			var column = ourLambda.GetLocation().GetLineSpan().StartLinePosition.Character;
			return lambdas.All(l => l.GetLocation().GetLineSpan().StartLinePosition.Character > column);
		}
	}
}
