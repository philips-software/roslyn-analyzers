// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	/// <summary>
	/// Report when a multi line condition statement (if or ?), does not include a newline on its logical operators.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class SplitMultiLineConditionOnLogicalOperatorAnalyzer : DiagnosticAnalyzer
	{
		/// <summary>
		/// Diagnostic Id for this analyzer.
		/// </summary>
		private const string Title = "Avoid multiple conditions on the same line.";
		private const string Message =
			"Split multiline conditions around line {0} such that the logical operators are at the end " +
			"of the line.";
		private const string Description = "Avoid multiple conditions on the same line of a multi-line condition statement. Instead, break lines right after the logical operators.";
		private const string Category = "Readability";

		private static readonly DiagnosticDescriptor Rule =
			new (
				Helper.ToDiagnosticId(DiagnosticIds.SplitMultiLineConditionOnLogicalOperator),
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

			var logicalNode = FindHighestLogicalInHierarchy(context.Node);
			if (!ReferenceEquals(logicalNode, context.Node))
			{
				return;
			}

			var location = logicalNode.GetLocation();
			if (!IsMultiLine(location))
			{
				return;
			}
			
			var lastToken = logicalNode.GetLastToken();
			var violations = logicalNode.DescendantTokens()
				.Where(ContainsEndOfLine)
				.Where(IsIllegalLineBreakToken) // Check 2).
				.Where(t => t != lastToken);
			foreach (var violation in violations)
			{
				ReportDiagnostic(context, violation);
			}
		}

		private void ReportDiagnostic(SyntaxNodeAnalysisContext context, SyntaxToken violation)
		{
			// Report the location of the nearest SyntaxNode.
			var loc = violation.Parent?.GetLocation();
			var lineNumber = loc.GetLineSpan().StartLinePosition.Line + 1;
			context.ReportDiagnostic(Diagnostic.Create(Rule, loc, lineNumber));
		}

		private SyntaxNode FindHighestLogicalInHierarchy(SyntaxNode startNode)
		{
			SyntaxNode foundNode = startNode;
			SyntaxNode parent = startNode.Parent;
			while (parent != null)
			{
				SyntaxKind currentKind = parent.Kind();
				if (currentKind is SyntaxKind.LogicalAndExpression or SyntaxKind.LogicalOrExpression)
				{
					foundNode = parent;
				}

				// Prevent breaking out of the context.
				if (currentKind is SyntaxKind.IfStatement
				    or SyntaxKind.SimpleAssignmentExpression
				    or SyntaxKind.ReturnStatement)
				{
					break;
				}

				parent = parent.Parent;
			}
			return foundNode;
		}

		private bool IsMultiLine(Location loc)
		{
			var lineSpan = loc.GetLineSpan();
			var startLine = lineSpan.StartLinePosition.Line;
			var endLine = lineSpan.EndLinePosition.Line;
			return startLine != endLine;
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
			// => for lambdas
			return 
				!token.IsKind(SyntaxKind.OpenParenToken) &&
			    !token.IsKind(SyntaxKind.AmpersandAmpersandToken) &&
			    !token.IsKind(SyntaxKind.BarBarToken) &&
			    !token.IsKind(SyntaxKind.EqualsGreaterThanToken);
		}
	}
}
