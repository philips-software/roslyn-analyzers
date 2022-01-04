// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TestContextCodeFixProvider)), Shared]
	public class TestContextCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Remove Test Context declaration";

		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticIds.TestContext)); }
		}

		public sealed override FixAllProvider GetFixAllProvider()
		{
			// See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
			return WellKnownFixAllProviders.BatchFixer;
		}

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			Diagnostic diagnostic = context.Diagnostics.First();
			TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

			// find the node which is the "TestContext" type identifier
			// the parent of the parent is the declaration we want to remove
			if (root != null)
			{
				SyntaxNode node = root.FindToken(diagnosticSpan.Start).Parent;

				// Register a code action that will invoke the fix.
				context.RegisterCodeFix(
					CodeAction.Create(
						title: Title,
						createChangedDocument: c => TestContextFix(context.Document, node, c),
						equivalenceKey: Title),
					diagnostic);
			}
		}

		private async Task<Document> TestContextFix(Document document, SyntaxNode declaration, CancellationToken cancellationToken)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken);

			// find the underlying variable
			IEnumerable<SyntaxNode> propNodes = declaration.DescendantNodes();
			ReturnStatementSyntax returnStatement = propNodes.OfType<ReturnStatementSyntax>().First();
			string varName = string.Empty;
			if (returnStatement != null)
			{
				IdentifierNameSyntax returnVar = returnStatement.Expression as IdentifierNameSyntax;
				if (returnVar != null)
				{
					varName = returnVar.Identifier.ToString();
				}
			}

			// remove the property
			if (rootNode != null)
			{
				rootNode = rootNode.RemoveNode(declaration, SyntaxRemoveOptions.KeepNoTrivia);

				if (!string.IsNullOrEmpty(varName))
				{
					foreach (VariableDeclarationSyntax varDeclaration in rootNode.DescendantNodes()
						.OfType<VariableDeclarationSyntax>())
					{
						if (varDeclaration.Variables[0].Identifier.ToString() == varName)
						{
							// remove the underlying variable
							if (varDeclaration.Parent != null)
								rootNode = rootNode.RemoveNode(varDeclaration.Parent, SyntaxRemoveOptions.KeepNoTrivia);
							break;
						}
					}
				}

				document = document.WithSyntaxRoot(rootNode);
			}

			return document;
		}
	}
}