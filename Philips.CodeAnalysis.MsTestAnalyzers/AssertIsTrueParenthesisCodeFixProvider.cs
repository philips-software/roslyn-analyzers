// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AssertIsTrueParenthesisCodeFixProvider)), Shared]
	public class AssertIsTrueParenthesisCodeFixProvider : SingleDiagnosticCodeFixProvider<InvocationExpressionSyntax>
	{
		protected override string Title => "Refactor IsTrue/IsFalse parentheses usage";

		protected override DiagnosticId DiagnosticId => DiagnosticId.AssertIsTrueParenthesis;

		protected override async Task<Document> ApplyFix(Document document, InvocationExpressionSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			ArgumentSyntax arg = node.ArgumentList.Arguments[0];

			var expression = (ParenthesizedExpressionSyntax)arg.Expression;

			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);

			root = root.ReplaceNode(expression, expression.Expression);

			return document.WithSyntaxRoot(root);
		}
	}
}
