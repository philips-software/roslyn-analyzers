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
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TestHasTimeoutCodeFixProvider)), Shared]
	public class TestHasTimeoutCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Add Test Timeout";

		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticIds.TestHasTimeoutAttribute)); }
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
			MethodDeclarationSyntax attributeList = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

			// Register a code action that will invoke the fix.
			context.RegisterCodeFix(
				CodeAction.Create(
					title: Title,
					createChangedDocument: c => AddTestTimeout(context.Document, attributeList, c),
					equivalenceKey: Title),
				diagnostic);
		}

		private async Task<Document> AddTestTimeout(Document document, MethodDeclarationSyntax method, CancellationToken cancellationToken)
		{
			SyntaxList<AttributeListSyntax> attributeLists = method.AttributeLists;
			string category = string.Empty;
			string timeout = string.Empty;
			foreach (AttributeListSyntax attributes in attributeLists)
			{
				foreach (AttributeSyntax attributesyntax in attributes.Attributes)
				{
					if (attributesyntax.Name.ToString().Contains(@"Timeout"))
					{
						return document;
					}
					else if (attributesyntax.Name.ToString().Contains(@"TestCategory"))
					{
						category = attributesyntax.ArgumentList.Arguments.First().ToString();
					}
				}
			}

			switch (category)
			{
				case "TestDefinitions.UnitTests":
					timeout = "(TestTimeouts.CiAppropriate)";
					break;
				case "TestDefinitions.IntegrationTests":
					timeout = "(TestTimeouts.Integration)";
					break;
				case "TestDefinitions.NightlyTests":
					timeout = "(TestTimeouts.Nightly)";
					break;
				case "TestDefinitions.SmokeTests":
					timeout = "(TestTimeouts.Smoke)";
					break;
				default:
					timeout = "(TestTimeouts.CiAppropriate)";
					break;
			}

			NameSyntax name = SyntaxFactory.ParseName("Timeout");
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken);
			AttributeArgumentListSyntax arguments = SyntaxFactory.ParseAttributeArgumentList(timeout);
			AttributeSyntax attribute = SyntaxFactory.Attribute(name, arguments);

			AttributeListSyntax attributeList = SyntaxFactory.AttributeList(
				SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(attribute));

			MethodDeclarationSyntax newMethod = method.WithAttributeLists(method.AttributeLists.Add(attributeList));

			SyntaxNode newRoot = rootNode.ReplaceNode(method, newMethod);
			Document newDocument = document.WithSyntaxRoot(newRoot);
			return newDocument;
		}
	}
}