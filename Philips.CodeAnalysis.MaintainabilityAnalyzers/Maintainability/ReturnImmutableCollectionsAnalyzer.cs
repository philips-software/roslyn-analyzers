// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

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

		private static readonly IReadOnlyList<string> MutableCollections = new List<string>() { "List", "Collection", "Dictionary", "IList", "ICollection", "IDictionary" };

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.MethodDeclaration);
		}
		
		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			var method = (MethodDeclarationSyntax)context.Node;

			if (!IsCallableFromOutsideClass(method))
			{
				// Private methods are allowed to return mutable collections.
				return;
			}

			var aliases = Helper.GetUsingAliases(method);
			var returnTypeName = Helper.GetFullName(method.ReturnType, aliases);
			if (method.ReturnType is GenericNameSyntax genericName)
			{
				var baseName = genericName.Identifier.Text;
				if (!aliases.TryGetValue(baseName, out returnTypeName))
				{
					returnTypeName = baseName;
				}
			}

			NamespaceIgnoringComparer comparer = new();
			if (MutableCollections.Any(m => comparer.Compare(m, returnTypeName) == 0))
			{
				var loc = method.ReturnType.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(Rule, loc, returnTypeName));
			}
		}

		private static bool IsCallableFromOutsideClass(MethodDeclarationSyntax method)
		{
			return method.Modifiers.Any(SyntaxKind.PublicKeyword) || method.Modifiers.Any(SyntaxKind.InternalKeyword) || method.Modifiers.Any(SyntaxKind.ProtectedKeyword);
		}
	}
}
