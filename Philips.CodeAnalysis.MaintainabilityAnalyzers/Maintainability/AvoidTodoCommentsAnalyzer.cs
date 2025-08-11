// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidTodoCommentsAnalyzer : SingleDiagnosticAnalyzer<CompilationUnitSyntax, AvoidTodoCommentsSyntaxNodeAction>
	{
		private const string Title = @"Avoid TODO comments";
		public const string MessageFormat = @"Avoid TODO comments in source code";
		private const string Description = @"Source code is not the place to get attention for a workitem. Use a proper task management system instead.";

		public AvoidTodoCommentsAnalyzer()
			: base(DiagnosticId.AvoidTodoComments, Title, MessageFormat, Description, Categories.Maintainability)
		{ }
	}

	public class AvoidTodoCommentsSyntaxNodeAction : SyntaxNodeAction<CompilationUnitSyntax>
	{
		public override void Analyze()
		{
			IEnumerable<SyntaxTrivia> comments = Node.DescendantTrivia()
				.Where(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
								 trivia.IsKind(SyntaxKind.MultiLineCommentTrivia));

			if (!comments.Any())
			{
				return;
			}

			foreach (SyntaxTrivia comment in comments)
			{
				var commentText = comment.ToString();
				if (commentText.ToUpperInvariant().Contains("TODO"))
				{
					Location location = comment.GetLocation();
					ReportDiagnostic(location);
				}
			}
		}
	}
}
