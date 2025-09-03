// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{

	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ReturnImmutableCollectionsCodeFixProvider)), Shared]
	public class ReturnImmutableCollectionsCodeFixProvider : SingleDiagnosticCodeFixProvider<TypeSyntax>
	{
		private const string ReadonlyCollection = "IReadOnlyCollection";
		private const string ReadonlyDictionary = "IReadOnlyDictionary";
		private const string ReadonlyList = "IReadOnlyList";

		private static readonly Dictionary<string, string> CollectionsMap = new()
		{
			{StringConstants.List, ReadonlyList},
			{StringConstants.QueueClassName, ReadonlyCollection},
			{StringConstants.SortedListClassName, ReadonlyDictionary},
			{StringConstants.StackClassName, ReadonlyCollection},
			{StringConstants.DictionaryClassName, ReadonlyDictionary},
			{StringConstants.IListInterfaceName, ReadonlyList},
			{StringConstants.ICollectionInterfaceName, ReadonlyCollection},
			{StringConstants.IDictionaryInterfaceName, ReadonlyDictionary}
		};

		protected override string Title => "Return immutable collections";

		protected override DiagnosticId DiagnosticId => DiagnosticId.ReturnImmutableCollections;

		protected override TypeSyntax GetNode(SyntaxNode root, TextSpan diagnosticSpan)
		{
			return root.FindNode(diagnosticSpan) as TypeSyntax;
		}

		protected override async Task<Document> ApplyFix(Document document, TypeSyntax node, ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
		{
			SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			var origTypeName = properties[ReturnImmutableCollectionsAnalyzer.AnnotationsKey];
			string newTypeName;
			if (node is ArrayTypeSyntax arrayType)
			{
				var elementTypeName = arrayType.ElementType.ToString();
				newTypeName = $"IReadOnlyList<{elementTypeName}>";
			}
			else if (CollectionsMap.TryGetValue(origTypeName, out var newCollectionType))
			{
				var genericTypeNames = GetGenericTypeNames(node);
				newTypeName = $"{newCollectionType}{genericTypeNames}";

			}
			else
			{
				return document;
			}
			TypeSyntax newType = SyntaxFactory.ParseTypeName(newTypeName).WithTriviaFrom(node);
			rootNode = rootNode.ReplaceNode(node, newType);
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
