// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace Philips.CodeAnalysis.Common
{
	/// <summary>
	/// Abstract base class for <see cref="CodeFixProvider"/> that fix a single Diagnostic in a source file.
	/// </summary>
	/// <typeparam name="TSyntax">The <see cref="SyntaxNode"/> type to fix.</typeparam>
	public abstract class SingleDiagnosticCodeFixProvider<TSyntax> : CodeFixProvider where TSyntax : SyntaxNode
	{
		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticId));

		public sealed override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			SyntaxNode root =
				await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			Diagnostic diagnostic = context.Diagnostics.First();
			TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

			if (root != null)
			{
				TSyntax node = GetNode(root, diagnosticSpan);

				context.RegisterCodeFix(
					CodeAction.Create(
						title: Title,
						createChangedDocument: c => ApplyFix(context.Document, node, diagnostic.Properties, c),
						equivalenceKey: Title),
					diagnostic);
			}
		}

		protected abstract string Title { get; }

		protected abstract DiagnosticId DiagnosticId { get; }

		protected abstract Task<Document> ApplyFix(Document document, TSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken);

		protected virtual TSyntax GetNode(SyntaxNode root, TextSpan diagnosticSpan)
		{
			SyntaxToken token = root.FindToken(diagnosticSpan.Start);
			return token.Parent?.AncestorsAndSelf().OfType<TSyntax>().FirstOrDefault();
		}
	}
}
