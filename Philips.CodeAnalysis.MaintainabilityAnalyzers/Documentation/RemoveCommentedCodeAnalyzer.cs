// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class RemoveCommentedCodeAnalyzer : SingleDiagnosticAnalyzer<CompilationUnitSyntax, RemoveCommentedCodeSyntaxNodeAction>
	{
		private const string Title = @"Remove commented code";
		private const string MessageFormat = @"Remove commented code on line {0}.";
		private const string Description = Title;

		public RemoveCommentedCodeAnalyzer()
			: base(DiagnosticId.RemoveCommentedCode, Title, MessageFormat, Description, Categories.Documentation, isEnabled: false)
		{ }
	}

	public class RemoveCommentedCodeSyntaxNodeAction : SyntaxNodeAction<CompilationUnitSyntax>
	{
		private const int InitialCodeLine = -20;

		public override IEnumerable<Diagnostic> Analyze()
		{
			return Node.DescendantTrivia().Where(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
				.Where(comment => comment.ToString().EndsWith(";"))
				.Select(node => node.GetLocation())
				.Fold<(List<Diagnostic> Diagnostics, int Line), Location>((new List<Diagnostic>(), InitialCodeLine),
				(prevStep, location) =>
				{
					var lineNumber = location.GetLineSpan().StartLinePosition.Line + 1;
					if (lineNumber - prevStep.Line > 1)
					{
						prevStep.Diagnostics.Add(PrepareDiagnostic(location, lineNumber));
					}
					return (prevStep.Diagnostics, lineNumber);
				}).Diagnostics;
		}
	}
}
