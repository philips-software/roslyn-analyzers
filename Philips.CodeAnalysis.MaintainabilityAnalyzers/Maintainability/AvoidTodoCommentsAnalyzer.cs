// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
		private static readonly Regex TodoWordRegex = new(@"(?<=\W|^)TODO(?=\W|$)", RegexOptions.IgnoreCase);

		public override void Analyze()
		{
			IEnumerable<SyntaxTrivia> comments = Node.DescendantTrivia()
				.Where(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
								 trivia.IsKind(SyntaxKind.MultiLineCommentTrivia));

			foreach (SyntaxTrivia comment in comments)
			{
				var commentText = comment.ToString();
				if (ContainsTodoAsWord(commentText))
				{
					Location location = comment.GetLocation();
					ReportDiagnostic(location);
				}
			}
		}

		private static bool ContainsTodoAsWord(string text)
		{
			// Performance optimization: quick IndexOf check before expensive regex
			if (text.IndexOf("TODO", StringComparison.OrdinalIgnoreCase) < 0)
			{
				return false;
			}

			return TodoWordRegex.IsMatch(text);
		}
	}
}
