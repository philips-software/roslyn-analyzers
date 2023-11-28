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
using Microsoft.CodeAnalysis.Formatting;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidArrayListCodeFixProvider)), Shared]
	public class AvoidArrayListCodeFixProvider : SingleDiagnosticCodeFixProvider<VariableDeclarationSyntax>
	{
		private readonly SyntaxAnnotation _annotation = new("ReplaceArrayList");

		protected override string Title => "Replace ArrayList with List<T>";

		protected override DiagnosticId DiagnosticId => DiagnosticId.AvoidArrayList;

		protected override async Task<Document> ApplyFix(Document document, VariableDeclarationSyntax node, ImmutableDictionary<string, string> properties,
			CancellationToken cancellationToken)
		{
			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);

			VariableDeclarationSyntax variable = node;
			TypeSyntax type = variable?.Type;
			root = ReplaceTypeWithList(root, type);
			VariableDeclarationSyntax replacedVariable = root.GetAnnotatedNodes(_annotation).FirstOrDefault()?.Ancestors()
				.OfType<VariableDeclarationSyntax>().FirstOrDefault();
			if (replacedVariable?.Variables[0]?.Initializer?.Value is ObjectCreationExpressionSyntax creation)
			{
				root = ReplaceTypeWithList(root, creation.Type);
			}

			return document.WithSyntaxRoot(root);
		}

		private SyntaxNode ReplaceTypeWithList(SyntaxNode root, TypeSyntax existingType)
		{
			SyntaxNode newRoot = root;
			if (root != null && existingType != null)
			{
				TypeSyntax parameterType = SyntaxFactory.ParseTypeName("int")
					.WithAdditionalAnnotations(RenameAnnotation.Create(), _annotation);
				TypeSyntax list = CreateGenericTypeSyntax(StringConstants.List, parameterType).WithTriviaFrom(existingType).WithAdditionalAnnotations(Formatter.Annotation);

				newRoot = root.ReplaceNode(existingType, list);
			}

			return newRoot;
		}

		/// <summary>
		/// Used to generate a type with generic arguments. Taken over from: https://gist.github.com/frankbryce/a4ee2bf799ab3878ae91
		/// </summary>
		/// <param name="identifier">Name of the Generic Type</param>
		/// <param name="arguments">
		/// Types of the Generic Arguments, which themselves may be generic types
		/// </param>
		/// <returns>An instance of TypeSyntax from the Roslyn Model</returns>
		private static GenericNameSyntax CreateGenericTypeSyntax(string identifier, params TypeSyntax[] arguments)
		{
			System.Collections.Generic.IEnumerable<TypeSyntax> args = arguments.Select(
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
					return x;
				});

			return
				SyntaxFactory.GenericName(
					SyntaxFactory.Identifier(identifier),
					SyntaxFactory.TypeArgumentList(
						SyntaxFactory.SeparatedList(args)
					)
				);
		}
	}
}
