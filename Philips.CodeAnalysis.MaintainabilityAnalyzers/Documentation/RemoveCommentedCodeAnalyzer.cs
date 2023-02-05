// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class RemoveCommentedCodeAnalyzer : SingleDiagnosticAnalyzer<CompilationUnitSyntax>
	{
		private const string Title = @"Remove commented code";
		private const string MessageFormat = @"Remove commented code on line {0}.";
		private const string Description = Title;
		private const string Category = Categories.Documentation;
		private const int InitialCodeLine = -20;

		public RemoveCommentedCodeAnalyzer()
			: base(DiagnosticId.RemoveCommentedCode, Title, MessageFormat, Description, Categories.Documentation)
		{ }

		protected override void Analyze()
		{
			var comments = Node.DescendantTrivia().Where(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia));
			if (!comments.Any())
			{
				return;
			}

			int previousViolationLine = InitialCodeLine;
			foreach (var location in comments.Where(comment => comment.ToString().EndsWith(";"))
											 .Select(node => node.GetLocation()))
			{
				var lineNumber = location.GetLineSpan().StartLinePosition.Line + 1;
				if (lineNumber - previousViolationLine > 1)
				{
					ReportDiagnostic(location, lineNumber);
				}
				previousViolationLine = lineNumber;
			}
		}
	}
}
