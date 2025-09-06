// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidUnnecessaryAttributeParenthesesAnalyzer : SingleDiagnosticAnalyzer<AttributeSyntax,
		AvoidUnnecessaryAttributeParenthesesSyntaxNodeAction>
	{
		private const string Title = @"Avoid unnecessary parentheses in attributes";
		private const string MessageFormat = @"Remove unnecessary parentheses from attribute";
		private const string Description = @"Attributes without arguments should not have empty parentheses. " +
			@"For example: [TestClass()] => [TestClass]";

		public AvoidUnnecessaryAttributeParenthesesAnalyzer()
			: base(DiagnosticId.AvoidUnnecessaryAttributeParentheses, Title, MessageFormat, Description,
				Categories.Maintainability, isEnabled: false)
		{
		}

		protected override SyntaxKind GetSyntaxKind()
		{
			return SyntaxKind.Attribute;
		}
	}

	public class AvoidUnnecessaryAttributeParenthesesSyntaxNodeAction : SyntaxNodeAction<AttributeSyntax>
	{
		public override void Analyze()
		{
			// Check if the attribute has an argument list
			if (Node.ArgumentList == null)
			{
				return;
			}

			// Check if the argument list is empty (has no arguments)
			if (Node.ArgumentList.Arguments.Count == 0)
			{
				Location location = Node.ArgumentList.GetLocation();
				ReportDiagnostic(location);
			}
		}
	}
}
