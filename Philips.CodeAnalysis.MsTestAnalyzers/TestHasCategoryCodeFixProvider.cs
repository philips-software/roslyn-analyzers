// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

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

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TestHasCategoryCodeFixProvider)), Shared]
	public class TestHasCategoryCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Add Test Category";

		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticId.TestHasCategoryAttribute)); }
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

			// Find the method declaration identified by the diagnostic.
			if (root != null)
			{
				SyntaxNode syntaxNode = root.FindToken(diagnosticSpan.Start).Parent;
				if (syntaxNode != null)
				{
					MethodDeclarationSyntax attributeList = syntaxNode.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

					// Register a code action that will invoke the fix.
					context.RegisterCodeFix(
						CodeAction.Create(
							title: Title,
							createChangedDocument: c => AddTestCategory(context.Document, attributeList, c),
							equivalenceKey: Title),
						diagnostic);
				}
			}
		}

		private async Task<Document> AddTestCategory(Document document, MethodDeclarationSyntax method, CancellationToken cancellationToken)
		{
			SyntaxList<AttributeListSyntax> attributeLists = method.AttributeLists;

			foreach (AttributeListSyntax attributes in attributeLists)
			{
				foreach (AttributeSyntax attributesyntax in attributes.Attributes)
				{
					if (attributesyntax.Name.ToString().Contains(@"TestCategory"))
					{
						return document;
					}
				}
			}

			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken);

			NameSyntax name = SyntaxFactory.ParseName("TestCategory");
			AttributeArgumentListSyntax arguments = SyntaxFactory.ParseAttributeArgumentList("(TestDefinitions.UnitTests)");
			AttributeSyntax attribute = SyntaxFactory.Attribute(name, arguments);

			AttributeListSyntax attributeList = SyntaxFactory.AttributeList(
				SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(attribute));

			attributeLists = method.AttributeLists.Add(attributeList);
			MethodDeclarationSyntax newMethod = method.WithAttributeLists(attributeLists);

			SyntaxNode newRoot = rootNode.ReplaceNode(method, newMethod);
			Document newDocument = document.WithSyntaxRoot(newRoot);
			return newDocument;
		}
	}
}