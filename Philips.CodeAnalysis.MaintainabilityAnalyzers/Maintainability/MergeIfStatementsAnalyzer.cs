// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using LanguageExt.SomeHelp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class MergeIfStatementsAnalyzer : SingleDiagnosticAnalyzer<IfStatementSyntax, MergeIfStatementsSyntaxNodeAction>
	{
		private const string Title = "Merge If Statements";
		private const string MessageFormat = Title;
		private const string Description = "Merging If statement with outer If statement to reduce cognitive load";

		public MergeIfStatementsAnalyzer()
			: base(DiagnosticId.MergeIfStatements, Title, MessageFormat, Description, Categories.Maintainability, isEnabled: false)
		{ }
	}

	public class MergeIfStatementsSyntaxNodeAction : SyntaxNodeAction<IfStatementSyntax>
	{
		public override IEnumerable<Diagnostic> Analyze()
		{
			// Node has an else clause
			if (Node.Else != null)
			{
				return Option<Diagnostic>.None;
			}

			SyntaxNode parent = Node.Parent;

			if (parent is BlockSyntax parentBlockSyntax)
			{
				// Has multiple statements in the block
				if (parentBlockSyntax.Statements.Count > 1)
				{
					return Option<Diagnostic>.None;
				}

				parent = parentBlockSyntax.Parent;
			}

			// Parent is not an If statement
			if (parent is not IfStatementSyntax parentIfSyntax)
			{
				return Option<Diagnostic>.None;
			}

			// Parent has an else clause
			if (parentIfSyntax.Else != null)
			{
				return Option<Diagnostic>.None;
			}

			// Has ||
			if (IfConditionHasLogicalAnd(Node))
			{
				return Option<Diagnostic>.None;
			}

			// Parent has ||
			if (IfConditionHasLogicalAnd(parentIfSyntax))
			{
				return Option<Diagnostic>.None;
			}

			Location location = Node.IfKeyword.GetLocation();
			return PrepareDiagnostic(location).ToSome();
		}

		private bool IfConditionHasLogicalAnd(IfStatementSyntax ifStatement)
		{
			return ifStatement.Condition.DescendantTokens().Any((token) => { return token.Kind() == SyntaxKind.BarBarToken; });
		}
	}
}
