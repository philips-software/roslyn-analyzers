// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure
{
	/// <summary>
	/// Analyzer class that verifies that after the 'as' keyword, the variable is checked for null before being used. This prevents an <see cref="System.NullReferenceException"/> being thrown at runtime.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class DereferenceNullAnalyzer : SingleDiagnosticAnalyzer<ExpressionSyntax, DereferenceNullSyntaxNodeAction>
	{
		private const string Title = @"Dereference Null after As";
		public const string MessageFormat = @"Using the 'as' expression means '{0}' could be null. Either check before dereferencing, or cast if you definitively know it will be this type in this context.";
		private const string Description = @"Using the 'as' expression means a check should be made before dereferencing, or cast if you definitively know it will be this type in this context.";

		public DereferenceNullAnalyzer()
			: base(DiagnosticId.DereferenceNull, Title, MessageFormat, Description, Categories.RuntimeFailure)
		{ }
	}

	public class DereferenceNullSyntaxNodeAction : SyntaxNodeAction<ExpressionSyntax>
	{
		private void Report(IdentifierNameSyntax identifier)
		{
			Location location = identifier.GetLocation();
			ReportDiagnostic(location, identifier.Identifier.ValueText);
		}

		/// <summary>
		/// The DataFlowAnalysis API is fairly limited.  It just provides a StartStatement and EndStatement.
		/// However, an "if" statement includes the entire Block that comes after the if clause.  This is
		/// insufficient granularity.  Consider the following example:
		///  y = x as yType
		///  if (blah)
		///  {
		///    -optional code here-
		///    y.ToString()
		/// We need to pass to DataFlowAnalysis a statement that is before y.ToString() for the endStatement.
		/// But if the firstStatement is the if clause, it includes the entire if block, of which endStatement is inside of.
		/// This causes DataFlowAnalysis to throw an exception.
		/// 
		/// Stated differently (and more problematically), DataFlowAnalysis only works on statements in the same statement list, ie the same block.
		/// https://github.com/kislyuk/roslyn/blob/89169a3380e48fc834c72e38355867881d030e94/Src/Compilers/CSharp/Source/Compilation/SyntaxTreeSemanticModel.cs#L1902
		///
		/// Fortunately, from a practical perspective, it is likely a common scenario that in this case, the reason it is in a nested block is precisely because
		/// it checked for null.
		/// </summary>
		private bool SameBlock(BlockSyntax blockOfInterest, SyntaxNode node)
		{
			BlockSyntax ourSymbolsBlock = node.Ancestors().OfType<BlockSyntax>().First();
			return ourSymbolsBlock.IsEquivalentTo(blockOfInterest);
		}

		/// <summary>
		/// Just handle the simple case of "y = x as yType;"
		/// </summary>
		private bool IsCaseWeUnderstand(SyntaxNode syntaxNode)
		{
			var binaryExpressionSyntax = syntaxNode as BinaryExpressionSyntax;
			return
				(binaryExpressionSyntax == null || binaryExpressionSyntax.OperatorToken.Kind() == SyntaxKind.AsKeyword) &&
				binaryExpressionSyntax != null &&
				binaryExpressionSyntax.Parent is EqualsValueClauseSyntax equalsValueClauseSyntax &&
				equalsValueClauseSyntax.Parent is VariableDeclaratorSyntax;
		}

		/// <summary>
		/// Return the Statement containing node, offset by the specified amount
		/// </summary>
		private (StatementSyntax statementSyntax, int index) GetStatement(BlockSyntax blockOfInterest, SyntaxNode node, int offset)
		{
			StatementSyntax ourStatement = node.Ancestors().OfType<StatementSyntax>().First();
			int statementOfInterestIndex;

			//verify we found the statement we are looking for
			if (blockOfInterest.Statements.Contains(ourStatement))
			{
				statementOfInterestIndex = blockOfInterest.Statements.IndexOf(ourStatement) + offset;
				StatementSyntax statementOfInterest = blockOfInterest.Statements[statementOfInterestIndex];
				return (statementOfInterest, statementOfInterestIndex);
			}

			// the statement of interest is nested within another statement
			var childNodes = blockOfInterest.DescendantNodes().OfType<StatementSyntax>().ToList();

			statementOfInterestIndex = childNodes.IndexOf(ourStatement) + offset;

			return (childNodes[statementOfInterestIndex], statementOfInterestIndex);
		}


		/// <summary>
		/// Find the first time ourSymbol is used, i.e., de-referenced.
		/// </summary>
		private IdentifierNameSyntax GetFirstMemberAccess(ISymbol ourSymbol, SemanticModel model, BlockSyntax blockOfInterest)
		{
			IEnumerable<MemberAccessExpressionSyntax> memberAccesses = blockOfInterest.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>();
			foreach (MemberAccessExpressionSyntax memberAccessExpressionSyntax in memberAccesses)
			{
				if (memberAccessExpressionSyntax.Expression is IdentifierNameSyntax identifierNameSyntax)
				{
					SymbolInfo potentialSymbolOfInterest = model.GetSymbolInfo(identifierNameSyntax);
					if (potentialSymbolOfInterest.Symbol == null)
					{
						continue;
					}

					if (SymbolEqualityComparer.Default.Equals(potentialSymbolOfInterest.Symbol, ourSymbol))
					{
						return identifierNameSyntax;
					}
				}
			}

			// It was never de-referenced.
			return null;
		}

		private bool HasNullCheck(ExpressionSyntax condition)
		{
			var conditionAsString = condition.ToString();
			if (conditionAsString.Contains(@"null") || conditionAsString.Contains(@".HasValue"))
			{
				// There's a null check of some kind in some order. Don't be too picky, just let it go to minimize risk of a false positive
				return true;
			}

			return false;
		}

		public override void Analyze()
		{
			if (!IsCaseWeUnderstand(Node))
			{
				return;
			}

			// Collect some items we'll use repeatedly
			if (Node.Parent?.Parent is not VariableDeclaratorSyntax variableDeclarationSyntax)
			{
				return;
			}

			BlockSyntax blockOfInterest = variableDeclarationSyntax.Ancestors().OfType<BlockSyntax>().First();
			SemanticModel model = Context.SemanticModel;
			ISymbol ourSymbol = model.GetDeclaredSymbol(variableDeclarationSyntax);

			//  Identify where y is first used (ie., MemberAccessExpression)
			IdentifierNameSyntax identifierNameSyntax = GetFirstMemberAccess(ourSymbol, model, blockOfInterest);
			if (identifierNameSyntax == null)
			{
				return;
			}
			if (!SameBlock(blockOfInterest, identifierNameSyntax))
			{
				return;
			}

			//  Evaluate the code between (ie after) "y = x as yType" and "y.Foo" (ie before) to see if y is read (ie checked for null) or written to (ie rendering our check moot)
			(StatementSyntax firstStatementOfAnalysis, var firstStatementOfAnalysisIndex) = GetStatement(blockOfInterest, variableDeclarationSyntax, offset: 1);
			(StatementSyntax lastStatementOfAnalysis, var lastStatementOfAnalysisIndex) = GetStatement(blockOfInterest, identifierNameSyntax, offset: -1);

			if (CheckStatements(lastStatementOfAnalysisIndex, firstStatementOfAnalysisIndex, firstStatementOfAnalysis, identifierNameSyntax))
			{
				return;
			}

			var isOurSymbolReadOrWritten = OurSymbolIsReadOrWritten(model, firstStatementOfAnalysis, lastStatementOfAnalysis, ourSymbol);
			if (!isOurSymbolReadOrWritten)
			{
				Report(identifierNameSyntax);
			}
		}

		private static bool OurSymbolIsReadOrWritten(SemanticModel model, StatementSyntax firstStatementOfAnalysis,
			StatementSyntax lastStatementOfAnalysis, ISymbol ourSymbol)
		{
			var isOurSymbolReadOrWritten = false;
			DataFlowAnalysis result = model.AnalyzeDataFlow(firstStatementOfAnalysis, lastStatementOfAnalysis);
			if (result != null)
			{
				isOurSymbolReadOrWritten |= result.ReadInside.Any(assignedValue => SymbolEqualityComparer.Default.Equals(assignedValue, ourSymbol));
				isOurSymbolReadOrWritten |= result.WrittenInside.Any(assignedValue => SymbolEqualityComparer.Default.Equals(assignedValue, ourSymbol));
			}

			return isOurSymbolReadOrWritten;
		}

		private bool CheckStatements(int lastStatementOfAnalysisIndex,
			int firstStatementOfAnalysisIndex, StatementSyntax firstStatementOfAnalysis,
			IdentifierNameSyntax identifierNameSyntax)
		{
			if (lastStatementOfAnalysisIndex < firstStatementOfAnalysisIndex)
			{
				// There's nothing to analyze; they immediately used the symbol after the 'as'

				// Before reporting an error, note common possibility that there's nothing to analyze between the statements, but within the statement there exists a check
				if (firstStatementOfAnalysis is IfStatementSyntax ifStatementSyntax &&
					HasNullCheck(ifStatementSyntax.Condition))
				{
					return true;
				}

				if (firstStatementOfAnalysis is WhileStatementSyntax whileStatementSyntax &&
					HasNullCheck(whileStatementSyntax.Condition))
				{
					return true;
				}

				if (firstStatementOfAnalysis is ReturnStatementSyntax returnStatementSyntax &&
					HasNullCheck(returnStatementSyntax.Expression))
				{
					return true;
				}

				if (firstStatementOfAnalysis.DescendantNodesAndSelf().OfType<ConditionalExpressionSyntax>()
					.Any(c => HasNullCheck(c.Condition)))
				{
					return true;
				}

				Report(identifierNameSyntax);
				return true;
			}

			return false;
		}
	}
}

