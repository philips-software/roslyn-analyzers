// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class RemoveCommentedCodeAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Remove commented code";
		private const string MessageFormat = @"Remove commented code on line {0}.";
		private const string Description = @"Remove commented code";
		private const string Category = Categories.Documentation;
		private const int InitialCodeLine = -20;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.RemoveCommentedCode), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.CompilationUnit);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			CompilationUnitSyntax node = (CompilationUnitSyntax)context.Node;

			GeneratedCodeDetector generatedCodeDetector = new();
			if (generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			var comments = node.DescendantTrivia().Where(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia));
			if (!comments.Any())
			{
				return;
			}

			int previousViolationLine = InitialCodeLine;
			foreach (var location in comments.Where(comment => comment.ToString().EndsWith(";")).Select(node => node.GetLocation()))
			{
				var lineNumber = location.GetLineSpan().StartLinePosition.Line + 1;
				if (lineNumber - previousViolationLine > 1)
				{
					Diagnostic diagnostic = Diagnostic.Create(Rule, location, lineNumber);
					context.ReportDiagnostic(diagnostic);
				}
				previousViolationLine = lineNumber;
			}
		}
	}
}
