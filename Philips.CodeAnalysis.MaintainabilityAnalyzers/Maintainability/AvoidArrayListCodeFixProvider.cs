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
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidArrayListCodeFixProvider)), Shared]
	public class AvoidArrayListCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Replace ArrayList with List<T>";
		private readonly SyntaxAnnotation annotation = new("ReplaceArrayList");

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

			if (token.Parent?.Parent is VariableDeclarationSyntax variable)
			{
				var type = variable?.Type;
				root = ReplaceTypeWithList(root, type);
				var replacedVariable = root.GetAnnotatedNodes(annotation).FirstOrDefault()?.Ancestors()
					.OfType<VariableDeclarationSyntax>().FirstOrDefault();
				if (
					replacedVariable != null &&
					replacedVariable.Variables[0]?.Initializer?.Value is ObjectCreationExpressionSyntax creation)
				{
					root = ReplaceTypeWithList(root, creation.Type);
				}

				return document.WithSyntaxRoot(root);
			}

			return document;
		}

		private SyntaxNode ReplaceTypeWithList(SyntaxNode root, TypeSyntax existingType)
		{
			SyntaxNode newRoot = root;
			if (root != null && existingType != null)
			{
				var parameterType = SyntaxFactory.ParseTypeName("int")
					.WithAdditionalAnnotations(RenameAnnotation.Create(), annotation);
				var list = CreateGenericTypeSyntax("List", parameterType).WithTriviaFrom(existingType).WithAdditionalAnnotations(Formatter.Annotation);

				newRoot = root.ReplaceNode(existingType, list);
			}

			return newRoot;
		}

		public override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		/// <summary>
		/// Used to generate a type with generic arguments. Taken over from: https://gist.github.com/frankbryce/a4ee2bf799ab3878ae91
		/// </summary>
		/// <param name="identifier">Name of the Generic Type</param>
		/// <param name="arguments">
		/// Types of the Generic Arguments, which themselves may be generic types
		/// </param>
		/// <returns>An instance of TypeSyntax from the Roslyn Model</returns>
		private static TypeSyntax CreateGenericTypeSyntax(string identifier, params TypeSyntax[] arguments)
		{
			return
				SyntaxFactory.GenericName(
					SyntaxFactory.Identifier(identifier),
					SyntaxFactory.TypeArgumentList(
						SyntaxFactory.SeparatedList(
							arguments.Select(
								x =>
								{
									if (x is GenericNameSyntax generic)
									{
										return
											CreateGenericTypeSyntax(
												generic.Identifier.ToString(),
												generic.TypeArgumentList.Arguments.ToArray()
											);
									}
									else
									{
										return x;
									}
								}
							)
						)
					)
				);
		}
	}
}
