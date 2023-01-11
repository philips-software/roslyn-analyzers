// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
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
			
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration);
		}

		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			ClassDeclarationSyntax classDeclaration = (ClassDeclarationSyntax)context.Node;

			LambdaVisitor visitor = new();

			visitor.Visit(classDeclaration);

			List<LambdaExpressionSyntax> lambdas = visitor.Lambdas;
			if (lambdas.Count <= 1)
			{
				return;
			}

			foreach (LambdaExpressionSyntax violation in FindLambdasOnSameLine(lambdas))
			{
				Location loc = violation.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(Rule, loc));
			}
		}

		private static IEnumerable<LambdaExpressionSyntax> FindLambdasOnSameLine(List<LambdaExpressionSyntax> lambdas)
		{
			// Using HashSet to filter out duplicates.
			HashSet<LambdaExpressionSyntax> result = new();
			int previousLine = -1;
			foreach(LambdaExpressionSyntax lambda in lambdas)
			{
				int currentLine = lambda.GetLocation().GetLineSpan().EndLinePosition.Line;
				if (previousLine == currentLine)
				{
					result.Add(lambda);
				}
				previousLine = currentLine;
			}
			return result;
		}
	}
}
