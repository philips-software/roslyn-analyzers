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
	public class DereferenceNullAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Dereference Null after As";
		public const string MessageFormat = @"Using the 'as' expression means '{0}' could be null. Either check before dereferencing, or cast if you definitively know it will be this type in this context.";
		private const string Description = @"Using the 'as' expression means a check should be made before dereferencing, or cast if you definitively know it will be this type in this context.";
		private const string Category = Categories.RuntimeFailure;

		public static readonly DiagnosticDescriptor Rule = new(
			Helper.ToDiagnosticId(DiagnosticIds.DereferenceNull),
			Title, MessageFormat, Category,
			DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);


		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.AsExpression);
		}

		private static void Report(SyntaxNodeAnalysisContext context, IdentifierNameSyntax identifier)
		{
			var diagnostic = Diagnostic.Create(Rule, identifier.GetLocation(), identifier.Identifier.ValueText);
			context.ReportDiagnostic(diagnostic);
		}

		/// <summary>
		/// The DataFlowAnalysis API is fairly limited.  It just provides a StartStatement and EndStatement.
		/// However, an "if" statement includes the entire Block that comes after the if clause.  This is
		/// insufficient granularity.  Consider the following example:
		///  y = x as yType;
		///  if (blah)
		///  {
		///    -optional code here-
		///    y.ToString();
		/// We need to pass to DataFlowAnalysis a statement that is before y.ToString() for the endStatement.
		/// But if the firstStatement is the if clause, it includes the entire if block, of which endStatement is inside of.
		/// This causes DataFlowAnalysis to throw an exception.

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
			BinaryExpressionSyntax binaryExpressionSyntax = syntaxNode as BinaryExpressionSyntax;
			return (binaryExpressionSyntax == null || binaryExpressionSyntax.OperatorToken.Kind() == SyntaxKind.AsKeyword)
				&& binaryExpressionSyntax != null 
				&& binaryExpressionSyntax.Parent is EqualsValueClauseSyntax equalsValueClauseSyntax
				&& equalsValueClauseSyntax.Parent is VariableDeclaratorSyntax;
		}

		/// <summary>
		/// Return the Statement containing node, offset by the specified amount
		/// </summary>
		private (StatementSyntax, int) GetStatement(BlockSyntax blockOfInterest, SyntaxNode node, int offset)
		{
			StatementSyntax ourStatement = node.Ancestors().OfType<StatementSyntax>().First();
			int statementOfInterestIndex;

			//verify we found the statement we are looking for
			if (blockOfInterest.Statements.Contains(ourStatement))
			{
				statementOfInterestIndex = blockOfInterest.Statements.IndexOf(ourStatement) + offset;
				return (blockOfInterest.Statements.ElementAt(statementOfInterestIndex), statementOfInterestIndex);
			}

			// the statement of interest is nested within another statement
			List<StatementSyntax> childNodes = blockOfInterest.DescendantNodes().OfType<StatementSyntax>().ToList();

			statementOfInterestIndex = childNodes.IndexOf(ourStatement) + offset;

			return (childNodes[statementOfInterestIndex], statementOfInterestIndex);
		}


		/// <summary>
		/// Find the first time ourSymbol is used, i.e., de-referenced.
		/// </summary>
		private IdentifierNameSyntax GetFirstMemberAccess(ISymbol ourSymbol, SemanticModel model, BlockSyntax blockOfInterest)
		{
			SimpleMemberAccessVisitor visitor = new();
			visitor.Visit(blockOfInterest);
			foreach (MemberAccessExpressionSyntax memberAccessExpressionSyntax in visitor.MemberAccesses)
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
			string conditionAsString = condition.ToString();
			if (conditionAsString.Contains(@"null") || conditionAsString.Contains(@".HasValue"))
			{
				// There's a null check of some kind in some order. Don't be too picky, just let it go to minimize risk of a false positive
				return true;
			}

			return false;
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			if (!IsCaseWeUnderstand(context.Node))
			{
				return;
			}

			// Collect some items we'll use repeatedly
			if (context.Node.Parent?.Parent is VariableDeclaratorSyntax variableDeclarationSyntax)
			{
				BlockSyntax blockOfInterest = variableDeclarationSyntax.Ancestors().OfType<BlockSyntax>().First();
				var model = context.SemanticModel;
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
				(StatementSyntax firstStatementOfAnalysis, int firstStatementOfAnalysisIndex) = GetStatement(blockOfInterest, variableDeclarationSyntax, offset: 1);
				(StatementSyntax lastStatementOfAnalysis, int lastStatementOfAnalysisIndex) = GetStatement(blockOfInterest, identifierNameSyntax, offset: -1);

				if (lastStatementOfAnalysisIndex < firstStatementOfAnalysisIndex)
				{
					// There's nothing to analyze; they immediately used the symbol after the 'as'

					// Before reporting an error, note common possibility that the situation could be:
					// string y = obj as string;
					// if (y != null && y.ToString() == @"")
					// Ie there's nothing to analyze between the statements, but within the statement exists a check
					if (firstStatementOfAnalysis is IfStatementSyntax ifStatementSyntax)
					{
						if (HasNullCheck(ifStatementSyntax.Condition))
						{
							return;
						}
					}
					
					if(firstStatementOfAnalysis is WhileStatementSyntax whileStatementSyntax)
					{
						if (HasNullCheck(whileStatementSyntax.Condition))
						{
							return;
						}
					}

					if (firstStatementOfAnalysis.DescendantNodesAndSelf().OfType<ConditionalExpressionSyntax>().Any(c => HasNullCheck(c.Condition)))
					{
						return;
					}

					Report(context, identifierNameSyntax);
					return;
				}

				bool ourSymbolIsReadOrWritten = false;
				DataFlowAnalysis result = model.AnalyzeDataFlow(firstStatementOfAnalysis, lastStatementOfAnalysis);
				if (result != null)
				{
					foreach (ISymbol assignedValue in result.ReadInside)
					{
						if (SymbolEqualityComparer.Default.Equals(assignedValue, ourSymbol))
						{
							// We shouldn't just be checking that we read our symbol; we should really see if it's checked for null
							ourSymbolIsReadOrWritten = true;
							break;
						}
					}

					foreach (ISymbol assignedValue in result.WrittenInside)
					{
						if (SymbolEqualityComparer.Default.Equals(assignedValue, ourSymbol))
						{
							ourSymbolIsReadOrWritten = true;
							break;
						}
					}
				}

				if (!ourSymbolIsReadOrWritten)
				{
					Report(context, identifierNameSyntax);
				}
			}
		}
	}


	/// <summary>
	/// 
	/// </summary>
	public class SimpleMemberAccessVisitor : CSharpSyntaxWalker
	{
		public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
		{
			base.VisitMemberAccessExpression(node);
			MemberAccesses.Add(node);
		}

		public List<MemberAccessExpressionSyntax> MemberAccesses { get; } = new List<MemberAccessExpressionSyntax>();
	}
}

