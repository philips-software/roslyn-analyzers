// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
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
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TestHasTimeoutCodeFixProvider)), Shared]
	public class TestHasTimeoutCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Add Test Timeout";

		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticId.TestHasTimeoutAttribute)); }
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
							createChangedDocument: c => AddTestTimeout(context.Document, diagnostic.Properties.GetValueOrDefault(TestHasTimeoutAnalyzer.DefaultTimeoutKey), attributeList, c),
							equivalenceKey: Title),
						diagnostic);
				}
			}
		}

		private async Task<Document> AddTestTimeout(Document document, string defaultTimeout, MethodDeclarationSyntax method, CancellationToken cancellationToken)
		{
			// any timeout.  1000ms should be a good default.
			defaultTimeout ??= "1000";

			ExpressionSyntax expression;
			if (int.TryParse(defaultTimeout, NumberStyles.Integer, CultureInfo.InvariantCulture, out int integerTimeout))
			{
				expression = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(integerTimeout));
			}
			else
			{
				expression = SyntaxFactory.ParseExpression(defaultTimeout);
			}

			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken);

			NameSyntax name = SyntaxFactory.ParseName("Timeout");
			var newAttribute = SyntaxFactory.Attribute(name,
				SyntaxFactory.AttributeArgumentList(
					SyntaxFactory.SingletonSeparatedList(
						SyntaxFactory.AttributeArgument(expression))));

			SyntaxList<AttributeListSyntax> attributeLists = method.AttributeLists;

			foreach (AttributeListSyntax attributes in attributeLists)
			{
				foreach (AttributeSyntax attributesyntax in attributes.Attributes)
				{
					if (attributesyntax.Name.ToString().Contains(@"Timeout"))
					{
						SyntaxNode newRoot = rootNode.ReplaceNode(attributesyntax, newAttribute);

						return document.WithSyntaxRoot(newRoot);
					}
				}
			}

			AttributeListSyntax attributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(newAttribute));

			var newAttributeLists = method.AttributeLists.Add(attributeList);
			MethodDeclarationSyntax newMethod = method.WithAttributeLists(newAttributeLists);

			var root = rootNode.ReplaceNode(method, newMethod);
			Document newDocument = document.WithSyntaxRoot(root);
			return newDocument;
		}
	}
}