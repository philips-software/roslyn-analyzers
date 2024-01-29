﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ReturnImmutableCollectionsAnalyzer : SingleDiagnosticAnalyzer
	{
		private const string Title = @"Return only immutable collections";
		private const string MessageFormat = @"Don't return the mutable collection {0}, use a ReadOnly interface or immutable collection instead";
		private const string Description = @"Return only immutable or readonly collections from a public method, otherwise these collections can be changed by the caller without the callee noticing.";
		public const string AnnotationsKey = "SimplifiedCollectionTypeName";

		public ReturnImmutableCollectionsAnalyzer()
			: base(DiagnosticId.ReturnImmutableCollections, Title, MessageFormat, Description, Categories.Maintainability, isEnabled: false)
		{ }

		private static readonly IReadOnlyList<string> MutableCollections = new List<string>() { StringConstants.List, StringConstants.QueueClassName, StringConstants.SortedListClassName, StringConstants.StackClassName, StringConstants.DictionaryClassName, StringConstants.IListInterfaceName, StringConstants.IDictionaryInterfaceName };

		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
		}

		private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
		{
			var method = (MethodDeclarationSyntax)context.Node;
			AssertType(context, method.ReturnType, method);
		}

		private void AnalyzeProperty(SyntaxNodeAnalysisContext context)
		{
			var prop = (PropertyDeclarationSyntax)context.Node;
			AssertType(context, prop.Type, prop);
		}

		private void AssertType(SyntaxNodeAnalysisContext context, TypeSyntax type, MemberDeclarationSyntax parent)
		{
			if (!parent.IsCallableFromOutsideClass())
			{
				// Private members are allowed to return mutable collections.
				return;
			}

			NamespaceResolver resolver = Helper.ForNamespaces.GetUsingAliases(type);
			var typeName = resolver.GetDealiasedName(type);

			NamespaceIgnoringComparer comparer = new();
			if (type is ArrayTypeSyntax || MutableCollections.Any(m => comparer.Compare(m, typeName) == 0))
			{
				// Double check the type's namespace.
				ITypeSymbol symbolType = context.SemanticModel.GetTypeInfo(type).Type;
				var isArray = symbolType is IArrayTypeSymbol;
				var ns = symbolType?.ContainingNamespace?.ToString();
				if (symbolType != null && (isArray || ns is "System.Collections.Generic" or "<global namespace>"))
				{
					ImmutableDictionary<string, string> properties = ImmutableDictionary<string, string>.Empty.Add(AnnotationsKey, typeName);
					Location loc = type.GetLocation();
					context.ReportDiagnostic(Diagnostic.Create(Rule, loc, properties, typeName));
				}
			}
		}
	}
}
