// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
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
	/// Abstract base class for <see cref="CodeFixProvider"/> that fix an entire solution.
	/// </summary>
	/// <typeparam name="TSyntax">The <see cref="SyntaxNode"/> type to fix.</typeparam>
	public abstract class SolutionCodeFixProvider<TSyntax> : CodeFixProvider where TSyntax : SyntaxNode
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.ToId());

		public sealed override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			SyntaxNode root =
				await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
			var additionalFile = GetAdditionalFileName();
			if (!string.IsNullOrEmpty(additionalFile))
			{
				AdditionalFileDocument = GetDocument(context.Document.Project, additionalFile);

				if (AdditionalFileDocument == null)
				{
					return;
				}
			}

			Diagnostic diagnostic = context.Diagnostics.First();
			TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

			if (root != null)
			{
				SyntaxNode token = root.FindToken(diagnosticSpan.Start).Parent;
				if (token != null)
				{
					TSyntax node = GetNode(token);

					context.RegisterCodeFix(
						CodeAction.Create(
							title: Title,
							createChangedSolution: c => ApplyFix(context.Document, node, c),
							equivalenceKey: Title),
						diagnostic);
				}
			}
		}

		protected TextDocument AdditionalFileDocument { get; private set; }

		protected abstract string Title { get; }

		protected abstract DiagnosticId DiagnosticId { get; }

		protected abstract Task<Solution> ApplyFix(Document document, TSyntax node, CancellationToken cancellationToken);

		protected virtual string GetAdditionalFileName()
		{
			return null;
		}

		protected virtual TSyntax GetNode(SyntaxNode token)
		{
			return token.AncestorsAndSelf().OfType<TSyntax>().First();
		}

		public CodeFixHelper Helper { get; } = new CodeFixHelper();

		private static TextDocument GetDocument(Project project, string fileName)
		{
			return project.AdditionalDocuments.FirstOrDefault(doc => doc.Name.Equals(fileName, StringComparison.Ordinal));
		}
	}
}
