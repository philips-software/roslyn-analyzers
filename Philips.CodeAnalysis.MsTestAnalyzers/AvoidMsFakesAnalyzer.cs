// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidMsFakesAnalyzer : SingleDiagnosticAnalyzer<UsingStatementSyntax, AvoidMsFakesSyntaxNodeAction>
	{
		private const string Title = @"Avoid MS Fakes";
		public const string MessageFormat = @"Do not use MS Fakes as a Dependency Injection solution.";
		private const string Description = @"Avoid MS Fakes. Use Moq instead for example.  If applicable, remove the Reference and the .fakes file as well.";

		public AvoidMsFakesAnalyzer()
			: base(DiagnosticId.AvoidMsFakes, Title, MessageFormat, Description, Categories.Maintainability)
		{ }
	}
	public class AvoidMsFakesSyntaxNodeAction : SyntaxNodeAction<UsingStatementSyntax>
	{
		public override void Analyze()
		{
			ExpressionSyntax expression = Node.Expression;
			if (expression == null)
			{
				return;
			}

			if (expression.ToString().Contains(@"ShimsContext.Create"))
			{
				CSharpSyntaxNode violation = expression;
				var location = violation.GetLocation();
				ReportDiagnostic(location);
			}
		}
	}
}
