// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
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
	public class ReturnImmutableCollectionsAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Return only immutable collections";
		private const string MessageFormat = @"Don't return the mutable collection {0}, use a ReadOnly interface or immutable collection instead";
		private const string Description = @"Return only immutable or readonly collections from a public method, otherwise these collections can be changed by the caller without the callee noticing.";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.ReturnImmutableCollections),
			Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
			description: Description);

		private static readonly IReadOnlyList<string> MutableCollections = new List<string>() { "List", "Queue", "SortedList", "Stack", "Dictionary", "IList", "ICollection", "IDictionary" };

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
		}
		
		private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
		{
			var method = (MethodDeclarationSyntax)context.Node;
			AssertType(context, method.ReturnType, method);
		}

		private static void AnalyzeProperty(SyntaxNodeAnalysisContext context)
		{
			var prop = (PropertyDeclarationSyntax)context.Node;
			AssertType(context, prop.Type, prop);
		}

		private static void AssertType(SyntaxNodeAnalysisContext context, TypeSyntax type, MemberDeclarationSyntax parent)
		{
			if(!Helper.IsCallableFromOutsideClass(parent))
			{
				// Private members are allowed to return mutable collections.
				return;
			}

			var typeName = GetTypeName(type);

			NamespaceIgnoringComparer comparer = new();
			if(type is ArrayTypeSyntax || MutableCollections.Any(m => comparer.Compare(m, typeName) == 0))
			{
				// Double check the type's namespace.
				var symbolType = context.SemanticModel.GetTypeInfo(type).Type;
				bool isArray = symbolType is IArrayTypeSymbol;
				var ns = symbolType?.ContainingNamespace?.ToString();
				if (symbolType != null && (isArray || ns is "System.Collections.Generic" or "<global namespace>"))
				{
					var loc = type.GetLocation();
					context.ReportDiagnostic(Diagnostic.Create(Rule, loc, typeName));
				}
			}
		}

		private static string GetTypeName(TypeSyntax type)
		{
			var aliases = Helper.GetUsingAliases(type);
			var typeName = Helper.GetFullName(type, aliases);
			if (type is GenericNameSyntax genericName)
			{
				var baseName = genericName.Identifier.Text;
				if(!aliases.TryGetValue(baseName, out typeName))
				{
					typeName = baseName;
				}
			}

			return typeName;
		}
	}
}
