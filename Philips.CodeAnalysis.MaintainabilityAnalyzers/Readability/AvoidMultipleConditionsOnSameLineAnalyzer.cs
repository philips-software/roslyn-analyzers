// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	/// <summary>
	/// Report when a multi line condition statement (if or ?), does not include a newline on its logical operators.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidMultipleConditionsOnSameLineAnalyzer : DiagnosticAnalyzer
	{
		/// <summary>
		/// Diagnostic Id for this analyzer.
		/// </summary>
		private const string Title = "Divide multiline conditions on the logical operators.";
		private const string Message =
			"Divide multiline conditions around line {0} such, that the logical operators are at the end " +
			"of the line.";
		private const string Description = "Divide long conditions in consistent way";
		private const string Category = "Shoscar";

		private static readonly DiagnosticDescriptor Rule =
			new (
				Helper.ToDiagnosticId(DiagnosticIds.AvoidMultipleConditionsOnSameLine),
				Title,
				Message,
				Category,
				DiagnosticSeverity.Warning,
				isEnabledByDefault: true,
				description: Description);

		/// <summary>
		/// <inheritdoc cref="DiagnosticAnalyzer.SupportedDiagnostics"/>
		/// </summary>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create(Rule);

		/// <summary>
		/// <inheritdoc/>
		/// </summary>
		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.LogicalAndExpression);
			context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.LogicalOrExpression);
		}

		private void AnalyzeNode(SyntaxNodeAnalysisContext context)
		{
			GeneratedCodeDetector generatedCodeDetector = new();
			if (generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			var logicalNode = context.Node;
			var location = logicalNode.GetLocation();
			if (!IsMultiLine(location))
			{
				return;
			}
			
			var childNodes = logicalNode.ChildNodes();
			bool hasCombinedNode = childNodes.OfType<ParenthesizedExpressionSyntax>().Any();
			if (hasCombinedNode)
			{
				return;
			}

			// Checked in parts:
			// 1) first line should only have open parentheses.
			// 2) if newline is on operator or open parentheses.
			var tokenBefore = logicalNode.GetFirstToken().GetPreviousToken();
			var check1Pass = ContainsEndOfLine(tokenBefore);
			if (!check1Pass)
			{
				ReportDiagnostic(context, tokenBefore);
			}

			var lastToken = logicalNode.GetLastToken();
			var violations = logicalNode.DescendantTokens()
				.Where(ContainsEndOfLine)
				.Where(IsIllegalLineBreakToken); // Check 2).
			foreach (var violation in violations)
			{
				ReportDiagnostic(context, violation);
			}
		}

		private void ReportDiagnostic(SyntaxNodeAnalysisContext context, SyntaxToken violation)
		{
			var loc = violation.GetLocation();
			var lineNumber = loc.GetLineSpan().StartLinePosition.Line + 1;
			context.ReportDiagnostic(Diagnostic.Create(Rule, loc, lineNumber));
		}

		private bool IsMultiLine(Location loc)
		{
			var lineSpan = loc.GetLineSpan();
			var startLine = lineSpan.StartLinePosition.Line;
			var endLine = lineSpan.EndLinePosition.Line;
			return (startLine != endLine);
		}

		private bool ContainsEndOfLine(SyntaxToken token)
		{
			return token.TrailingTrivia.Any(SyntaxKind.EndOfLineTrivia);
		}

		private bool IsIllegalLineBreakToken(SyntaxToken token)
		{
			// We allow line breaks on the following tokens:
			// - ( for more complex logic expressions
			// - && and || as these are the logical operators.
			return !token.IsKind(SyntaxKind.OpenParenToken) &&
			       !token.IsKind(SyntaxKind.AmpersandAmpersandToken) &&
			       !token.IsKind(SyntaxKind.BarBarToken);
		}
	}
}
