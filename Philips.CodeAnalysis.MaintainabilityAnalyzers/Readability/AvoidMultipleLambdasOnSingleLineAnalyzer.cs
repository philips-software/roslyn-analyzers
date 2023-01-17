// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Concurrent;
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
			SyntaxNode node = context.Node;
			MethodDeclarationSyntax methodDeclaration =
				node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
			
			var lambdas = methodDeclaration?.DescendantNodes().OfType<LambdaExpressionSyntax>();
			if (lambdas == null || !lambdas.Any())
			{
				return;
			}

			if (FindLambdasOnSameLine(node, lambdas))
			{
				Location loc = node.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(Rule, loc));
			}
		}

		private static bool FindLambdasOnSameLine(SyntaxNode ourLambda, IEnumerable<LambdaExpressionSyntax> lambdas)
		{
			int previousLine = -1;
			foreach(LambdaExpressionSyntax lambda in lambdas)
			{
				// If we find a violation, report based on the heuristic that the violator is our lambda.
				int currentLine = lambda.GetLocation().GetLineSpan().EndLinePosition.Line;
				if (previousLine == currentLine && lambda == ourLambda)
				{
					return true;
				}
				previousLine = currentLine;
			}
			return false;
		}
	}
}
