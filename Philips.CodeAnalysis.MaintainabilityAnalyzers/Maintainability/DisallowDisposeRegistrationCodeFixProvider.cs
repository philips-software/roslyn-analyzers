// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DisallowDisposeRegistrationCodeFixProvider)), Shared]
	public class DisallowDisposeRegistrationCodeFixProvider : SingleDiagnosticCodeFixProvider<AssignmentExpressionSyntax>
	{
		protected override string Title => "Unregister instead of registering";

		protected override DiagnosticId DiagnosticId => DiagnosticId.DisallowDisposeRegistration;

		protected override async Task<Document> ApplyFix(Document document, AssignmentExpressionSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			AssignmentExpressionSyntax newAssignmentExpression = SyntaxFactory.AssignmentExpression(SyntaxKind.SubtractAssignmentExpression, node.Left, node.Right);
			SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken);
			SyntaxNode newRoot = oldRoot.ReplaceNode(node, newAssignmentExpression);
			return document.WithSyntaxRoot(newRoot);
		}
	}
}
