// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
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

	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ReturnImmutableCollectionsCodeFixProvider)), Shared]
	public class ReturnImmutableCollectionsCodeFixProvider : CodeFixProvider
	{
		private const string Title = "Return immutable collections";
		private const string ReadonlyCollection = "IReadOnlyCollection";
		private const string ReadonlyDictionary = "IReadOnlyDictionary";
		private const string ReadonlyList = "IReadOnlyList";

		private static readonly IReadOnlyDictionary<string, string> CollectionsMap = new Dictionary<string, string>()
		{
			{"List", ReadonlyList},
			{StringConstants.QueueClassName, ReadonlyCollection},
			{StringConstants.SortedListClassName, ReadonlyDictionary},
			{StringConstants.StackClassName, ReadonlyCollection},
			{StringConstants.DictionaryClassName, ReadonlyDictionary},
			{StringConstants.IListInterfaceName, ReadonlyList},
			{"ICollection", ReadonlyCollection},
			{StringConstants.IDictionaryInterfaceName, ReadonlyDictionary}
		};

		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticId.ReturnImmutableCollections));
		
		public sealed override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			Diagnostic diagnostic = context.Diagnostics.First();
			TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

			var type = root.FindNode(diagnosticSpan) as TypeSyntax;

			// Register a code action that will invoke the fix.
			context.RegisterCodeFix(
				CodeAction.Create(
					title: Title,
					createChangedDocument: c => ReplaceType(context.Document, type, c),
					equivalenceKey: Title),
				diagnostic);
		}

		private async Task<Document> ReplaceType(Document document, TypeSyntax type, CancellationToken c)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(c).ConfigureAwait(false);
			string origTypeName = ReturnImmutableCollectionsAnalyzer.GetTypeName(type);
			string newTypeName;
			if (type is ArrayTypeSyntax arrayType)
			{
				var elementTypeName = arrayType.ElementType.ToString();
				newTypeName = $"IReadOnlyList<{elementTypeName}>";
			}
			else if (CollectionsMap.TryGetValue(origTypeName, out string newCollectionType))
			{
				var genericTypeNames = GetGenericTypeNames(type);
				newTypeName = $"{newCollectionType}{genericTypeNames}";

			}
			else
			{
				return document;
			}
			var newType = SyntaxFactory.ParseTypeName(newTypeName).WithTriviaFrom(type);
			rootNode = rootNode.ReplaceNode(type, newType);
			return document.WithSyntaxRoot(rootNode);
		}

		private static string GetGenericTypeNames(TypeSyntax type)
		{
			if (type is GenericNameSyntax genericType)
			{
				return genericType.TypeArgumentList.ToString();
			}

			return string.Empty;
		}
	}
}
