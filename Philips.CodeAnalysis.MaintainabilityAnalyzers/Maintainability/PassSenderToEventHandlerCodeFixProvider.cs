// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

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

	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PassSenderToEventHandlerCodeFixProvider)), Shared]
	public class PassSenderToEventHandlerCodeFixProvider : SingleDiagnosticCodeFixProvider<ArgumentSyntax>
	{
		protected override string Title => "Pass sender to EventHandler";

		protected override DiagnosticId DiagnosticId => DiagnosticId.PassSenderToEventHandler;

		protected override async Task<Document> ApplyFix(Document document, ArgumentSyntax node, CancellationToken cancellationToken)
		{
			ArgumentSyntax argument = node;
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

			if (argument.Parent is ArgumentListSyntax argumentList)
			{
				var index = argumentList.Arguments.IndexOf(argument);
				if (index == 0)
				{
					ArgumentSyntax thisArgument = SyntaxFactory.Argument(SyntaxFactory.ThisExpression()).WithTriviaFrom(argument);
					rootNode = rootNode.ReplaceNode(argument, thisArgument);
				}
				else if (index == 1)
				{
					SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken);
					var typeName = semanticModel?.GetTypeInfo(argument, cancellationToken).Type?.Name ?? "EventArgs";
					ArgumentSyntax emptyArgument = SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(typeName), SyntaxFactory.IdentifierName("Empty"))).WithTriviaFrom(argument);
					rootNode = rootNode.ReplaceNode(argument, emptyArgument);
				}
			}
			return document.WithSyntaxRoot(rootNode);
		}
	}
}
