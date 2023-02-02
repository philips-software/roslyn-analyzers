// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{

	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PassSenderToEventHandlerCodeFixProvider)), Shared]
	public class PassSenderToEventHandlerCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Pass sender to EventHandler";

		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticId.PassSenderToEventHandler));
		public sealed override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			Diagnostic diagnostic = context.Diagnostics.First();
			TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

			var argument = root.FindNode(diagnosticSpan) as ArgumentSyntax;

			// Register a code action that will invoke the fix.
			context.RegisterCodeFix(
				CodeAction.Create(
					title: Title,
					createChangedDocument: c => ReplaceArgument(context.Document, argument, c),
					equivalenceKey: Title),
				diagnostic);
		}

		private async Task<Document> ReplaceArgument(Document document, ArgumentSyntax argument, CancellationToken c)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(c).ConfigureAwait(false);

			if (argument.Parent is ArgumentListSyntax argumentList)
			{
				var index = argumentList.Arguments.IndexOf(argument);
				if (index == 0)
				{
					var thisArgument = SyntaxFactory.Argument(SyntaxFactory.ThisExpression()).WithTriviaFrom(argument);
					rootNode = rootNode.ReplaceNode(argument, thisArgument);
				}
				else if (index == 1)
				{
					var semanticModel = await document.GetSemanticModelAsync(c);
					var typeName = semanticModel?.GetTypeInfo(argument, c).Type?.Name ?? "EventArgs";
					var emptyArgument = SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(typeName), SyntaxFactory.IdentifierName("Empty"))).WithTriviaFrom(argument);
					rootNode = rootNode.ReplaceNode(argument, emptyArgument);
				}
			}
			return document.WithSyntaxRoot(rootNode);
		}
	}
}
