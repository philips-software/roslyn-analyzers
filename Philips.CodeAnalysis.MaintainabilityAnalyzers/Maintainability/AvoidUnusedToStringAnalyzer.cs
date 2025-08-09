// © 2024 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidUnusedToStringAnalyzer : SingleDiagnosticAnalyzer<InvocationExpressionSyntax, AvoidUnusedToStringSyntaxNodeAction>
	{
		private const string Title = @"Avoid calling ToString() when the result is discarded";
		private const string MessageFormat = @"Avoid calling ToString() when the result is discarded or unused";
		private const string Description = @"Calling ToString() when the result is assigned to a discard variable or used as a standalone expression statement is unnecessary and should be removed.";

		public AvoidUnusedToStringAnalyzer()
			: base(DiagnosticId.AvoidUnusedToString, Title, MessageFormat, Description, Categories.Maintainability, isEnabled: true)
		{ }
	}

	public class AvoidUnusedToStringSyntaxNodeAction : SyntaxNodeAction<InvocationExpressionSyntax>
	{
		public override void Analyze()
		{
			// Check if this is a ToString() method call
			if (Node.Expression is not MemberAccessExpressionSyntax memberAccess)
			{
				return;
			}

			if (memberAccess.Name is not IdentifierNameSyntax { Identifier.ValueText: StringConstants.ToStringMethodName })
			{
				return;
			}

			// Check if ToString() is called with no arguments
			if (Node.ArgumentList.Arguments.Count > 0)
			{
				return;
			}

			// Check if the result is discarded or unused
			if (IsResultDiscardedOrUnused())
			{
				Location location = memberAccess.Name.GetLocation();
				ReportDiagnostic(location);
			}
		}

		private bool IsResultDiscardedOrUnused()
		{
			SyntaxNode parent = Node.Parent;

			// Case 1: Assignment to discard variable (_ = expr.ToString())
			if (parent is AssignmentExpressionSyntax assignment &&
				assignment.Left is IdentifierNameSyntax { Identifier.ValueText: "_" })
			{
				return true;
			}

			// Case 2: Standalone expression statement (expr.ToString();)
			if (parent is ExpressionStatementSyntax)
			{
				return true;
			}

			return false;
		}
	}
}
