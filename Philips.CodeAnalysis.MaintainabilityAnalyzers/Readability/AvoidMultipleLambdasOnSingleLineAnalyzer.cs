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

		private readonly HashSet<string> visitedTypes = new();

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.SimpleLambdaExpression);
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ParenthesizedLambdaExpression);
			context.RegisterCompilationAction(ClearCache);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			SyntaxNode node = context.Node;
			TypeDeclarationSyntax typeDeclaration =
				node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
			
			// Prevent reporting the same class twice.
			string typeName = $"{node.SyntaxTree.FilePath}-{typeDeclaration?.Identifier.Text}";
			lock (visitedTypes)
			{
				if (visitedTypes.Contains(typeName))
				{
					return;
				}

				visitedTypes.Add(typeName);
			}


			var lambdas = typeDeclaration?.DescendantNodes().OfType<LambdaExpressionSyntax>();
			if (lambdas == null || !lambdas.Any())
			{
				return;
			}

			foreach (LambdaExpressionSyntax violation in FindLambdasOnSameLine(lambdas))
			{
				Location loc = violation.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(Rule, loc));
			}
		}

		private void ClearCache(CompilationAnalysisContext _)
		{
			lock (visitedTypes)
			{
				visitedTypes.Clear();
			}
		}

		private static IEnumerable<LambdaExpressionSyntax> FindLambdasOnSameLine(IEnumerable<LambdaExpressionSyntax> lambdas)
		{
			// Using HashSet to filter out duplicates.
			// And relying on the fact that the order in the SyntaxTree is the same as in the .cs file.
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
