// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidArrayListAnalyzer)), Shared]
	public class AvoidArrayListCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Replace ArrayList with List<T>";

		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticIds.AvoidArrayList));

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			Diagnostic diagnostic = context.Diagnostics.First();
			TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

			if (root != null)
			{
				SyntaxToken token = root.FindToken(diagnosticSpan.Start);

				// Register a code action that will invoke the fix.
				context.RegisterCodeFix(
					CodeAction.Create(
						title: Title,
						createChangedDocument: c => SwapType(context.Document, token),
						equivalenceKey: Title),
					diagnostic);
			}
		}

		private async Task<Document> SwapType(Document document, SyntaxToken token)
		{
			SyntaxNode root = await document.GetSyntaxRootAsync();

			var variable = token.Parent?.Parent as VariableDeclarationSyntax;
			var type = variable?.Type;

			if (root != null && type != null)
			{
				var list = SyntaxFactory.ParseTypeName("System.Collections.Generic.List<int>");
				
				root = root.ReplaceNode(type, list.WithTriviaFrom(type).WithAdditionalAnnotations(Formatter.Annotation));

				return document.WithSyntaxRoot(root);
			}

			return document;
		}

		public sealed override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}
	}
}
