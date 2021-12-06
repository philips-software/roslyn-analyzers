using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class DereferenceNullAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Dereference Null after As";
		public const string MessageFormat = @"Using the 'as' expression means '{0}' could be null.  Either check before dereferencing, or cast if you definitively know it will be this type in this context.";
		private const string Description = @"Avoid hard-coded passwords.  (Avoid this analyzer by not naming something Password.)";
		private const string Category = Categories.RuntimeFailure;

		public static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			Helper.ToDiagnosticId(DiagnosticIds.DereferenceNull),
			Title, MessageFormat, Category,
			DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }


		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.AsExpression);
		}

		private void Report(SyntaxNodeAnalysisContext context, IdentifierNameSyntax identifier)
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

		/// Stated differently (and more problematically), DataFlowAnalysis only works on statements in the same statementlist, ie the same block.
		/// https://github.com/kislyuk/roslyn/blob/89169a3380e48fc834c72e38355867881d030e94/Src/Compilers/CSharp/Source/Compilation/SyntaxTreeSemanticModel.cs#L1902
		///
		/// Fortunately, from a practical perspective, it is likely a common scenario that in this case, the reason it is in a nested block is precisely because
		/// it checked for null.
		/// </summary>
		/// <param name="blockOfInterest"></param>
		/// <param name="node"></param>
		/// <returns></returns>
		private bool SameBlock(BlockSyntax blockOfInterest, SyntaxNode node)
		{
			BlockSyntax ourSymbolsBlock = node.Ancestors().OfType<BlockSyntax>().First();
			return ourSymbolsBlock.IsEquivalentTo(blockOfInterest);
		}

		/// <summary>
		/// Just handle the simple case of "y = x as yType;"
		/// </summary>
		/// <param name="syntaxNode"></param>
		/// <returns></returns>
		private bool IsCaseWeUnderstand(SyntaxNode syntaxNode)
		{
			BinaryExpressionSyntax binaryExpressionSyntax = syntaxNode as BinaryExpressionSyntax;
			if (binaryExpressionSyntax.OperatorToken.Kind() != SyntaxKind.AsKeyword)
			{
				return false;
			}

			if (binaryExpressionSyntax.Parent is EqualsValueClauseSyntax equalsValueClauseSyntax)
			{
				if (equalsValueClauseSyntax.Parent is VariableDeclaratorSyntax)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Return the Statement containing node, offset by the specified amount
		/// </summary>
		private (StatementSyntax, int) GetStatement(BlockSyntax blockOfInterest, SyntaxNode node, int offset)
		{
			StatementSyntax ourStatement = node.Ancestors().OfType<StatementSyntax>().First();
			int statementOfInterestIndex = blockOfInterest.Statements.IndexOf(ourStatement) + offset;
			return (blockOfInterest.Statements.ElementAt(statementOfInterestIndex), statementOfInterestIndex);
		}


		/// <summary>
		/// Find the first time ourSymbol is used, i.e., dereferenced.
		/// </summary>
		/// <param name="ourSymbol"></param>
		/// <param name="model"></param>
		/// <param name="blockOfInterest"></param>
		/// <returns></returns>
		private IdentifierNameSyntax GetFirstMemberAccess(ISymbol ourSymbol, SemanticModel model, BlockSyntax blockOfInterest)
		{
			SimpleMemberAccessVisitor visitor = new SimpleMemberAccessVisitor();
			visitor.Visit(blockOfInterest);
			foreach (MemberAccessExpressionSyntax memberAccessExpressionSyntax in visitor.MemberAccesses)
			{
				if (memberAccessExpressionSyntax.Expression is IdentifierNameSyntax identifierNameSyntax)
				{
					SymbolInfo potentialSymbolOfInterest = model.GetSymbolInfo(identifierNameSyntax);
					if (potentialSymbolOfInterest.Symbol == null)
						continue;

					if (SymbolEqualityComparer.Default.Equals(potentialSymbolOfInterest.Symbol, ourSymbol))
					{
						return identifierNameSyntax;
					}
				}
			}

			// It was never dereferenced.
			return null;
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			if (!IsCaseWeUnderstand(context.Node))
			{
				return;
			}

			// Collect some items we'll use repeatedly
			VariableDeclaratorSyntax variableDeclaratorSyntax = context.Node.Parent.Parent as VariableDeclaratorSyntax;
			BlockSyntax blockOfInterest = variableDeclaratorSyntax.Ancestors().OfType<BlockSyntax>().First();
			var model = context.SemanticModel;
			ISymbol ourSymbol = model.GetDeclaredSymbol(variableDeclaratorSyntax);

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
			(StatementSyntax firstStatementOfAnalysis, int firstStatementOfAnalysisIndex) = GetStatement(blockOfInterest, variableDeclaratorSyntax, offset: 1);
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
					if (ifStatementSyntax.Condition.ToString().Contains(@"null"))
					{
						// There's an "if" statement with a likely null check of some kind in some order.  Don't be too picky, just let it go to minimize risk of a false positive
						return;
					}
				}
				if (firstStatementOfAnalysis is WhileStatementSyntax whileStatementSyntax)
				{
					if (whileStatementSyntax.Condition.ToString().Contains(@"null"))
					{
						// There's an "if" statement with a likely null check of some kind in some order.  Don't be too picky, just let it go to minimize risk of a false positive
						return;
					}
				}


				Report(context, identifierNameSyntax);
				return;
			}

			bool ourSymbolIsReadOrWritten = false;
			DataFlowAnalysis result = model.AnalyzeDataFlow(firstStatementOfAnalysis, lastStatementOfAnalysis);
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
			if (!ourSymbolIsReadOrWritten)
			{
				Report(context, identifierNameSyntax);
				return;
			}
		}
	}


	/// <summary>
	/// 
	/// </summary>
	public class SimpleMemberAccessVisitor : CSharpSyntaxWalker
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods

		#endregion

		#region Public Interface

		public SimpleMemberAccessVisitor() : base(SyntaxWalkerDepth.Node)
		{ }

		public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
		{
			base.VisitMemberAccessExpression(node);
			MemberAccesses.Add(node);
		}

		public List<MemberAccessExpressionSyntax> MemberAccesses { get; } = new List<MemberAccessExpressionSyntax>();

		#endregion
	}
}

