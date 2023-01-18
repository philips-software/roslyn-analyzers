// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidMultipleLambdasOnSingleLineAnalyzer : DiagnosticAnalyzer
	{

		private const string Title = @"Avoid putting multiple Lambda expressions on the same line";
		public const string MessageFormat = @"Avoid putting multiple Lambda expressions on the same line";
		private const string Description = @"Avoid putting multiple Lambda expressions on the same line. To improve readability, split them into separate lines.";
		private const string Category = Categories.Readability;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.AvoidMultipleLambdasOnSingleLine), Title,
			MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);


		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.SimpleLambdaExpression);
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ParenthesizedLambdaExpression);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			LambdaExpressionSyntax node = (LambdaExpressionSyntax)context.Node;
			MethodDeclarationSyntax parent =
				node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
			
			var lambdas = parent?.DescendantNodes().OfType<LambdaExpressionSyntax>();
			if (lambdas == null || lambdas.Count() < 2)
			{
				return;
			}

			// Get a list of the lambdas that start on the same line, excluding our lambda.
			var lambdasOnSameLine = FindOtherLambdasOnSameLine(node, lambdas);
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
			// Using HashSet to filter out duplicates.
			// And relying on the fact that the order in the SyntaxTree is the same as in the .cs file.
			List<LambdaExpressionSyntax> result = new();
			int theLine = ourLambda.GetLocation().GetLineSpan().StartLinePosition.Line;
			foreach(LambdaExpressionSyntax lambda in lambdas)
			{
				int currentLine = lambda.GetLocation().GetLineSpan().StartLinePosition.Line;
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
			// ourLambda is the left most if all the lambdas are further to the right, whihc means a higher Character number.
			int column = ourLambda.GetLocation().GetLineSpan().StartLinePosition.Character;
			return lambdas.All(l => l.GetLocation().GetLineSpan().StartLinePosition.Character > column);
		}
	}
}
