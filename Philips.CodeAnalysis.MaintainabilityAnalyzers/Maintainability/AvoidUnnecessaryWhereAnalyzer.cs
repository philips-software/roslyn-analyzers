// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;
using System.Collections.Generic;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	/// <summary>
	/// Note that a CodeFixer isn't necessary. At the time of this writing, VS offers a refactoring but seemingly not an analyzer.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidUnnecessaryWhereAnalyzer : SingleDiagnosticAnalyzer<InvocationExpressionSyntax, AvoidUnnecessaryWhereSyntaxNodeAction>
	{
		private const string Title = @"Avoid unnecessary 'Where'";
		public const string MessageFormat = @"Move predicate from 'Where' to '{0}'";
		private const string Description = @"Invoking Where is unnecessary";

		public AvoidUnnecessaryWhereAnalyzer()
			: base(DiagnosticId.AvoidUnnecessaryWhere, Title, MessageFormat, Description, Categories.Maintainability, isEnabled: false)
		{ }
	}

	public class AvoidUnnecessaryWhereSyntaxNodeAction : SyntaxNodeAction<InvocationExpressionSyntax>
	{
		// List of terminal methods that can replace Where
		private static readonly HashSet<string> _terminalMethods =
		[
			"Count", "Any", "Single", "SingleOrDefault", "Last", "LastOrDefault", "First", "FirstOrDefault"
		];

		public override void Analyze()
		{
			// Only analyze invocations with no arguments
			if (Node.ArgumentList.Arguments.Count > 0)
			{
				return;
			}

			// Check if this is a method invocation of a terminal method
			if (Node.Expression is not MemberAccessExpressionSyntax memberAccess)
			{
				return;
			}

			// Get the method name and check if it's a terminal method
			var methodName = memberAccess.Name.Identifier.Text;
			if (!_terminalMethods.Contains(methodName))
			{
				return;
			}

			// Check if the previous expression is a Where call
			if (memberAccess.Expression is not InvocationExpressionSyntax invocation)
			{
				return;
			}

			if (invocation.Expression is not MemberAccessExpressionSyntax whereMemberAccess)
			{
				return;
			}

			// Check if it's a Where method
			if (whereMemberAccess.Name.Identifier.Text != "Where")
			{
				return;
			}

			// Verify it's System.Linq.Where
			if (Context.SemanticModel.GetSymbolInfo(whereMemberAccess.Name).Symbol is not IMethodSymbol whereSymbol)
			{
				return;
			}

			// Check if it's from System.Linq
			var whereSymbolString = whereSymbol.ToString();
			if (!whereSymbolString.StartsWith("System.Collections.Generic.IEnumerable"))
			{
				return;
			}

			// Report the diagnostic
			Location location = whereMemberAccess.Name.Identifier.GetLocation();
			ReportDiagnostic(location, methodName);
		}
	}
}
