// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class StringDictionaryNeedsComparerAnalyzer : DiagnosticAnalyzerBase
	{
		private const string Title = @"String-keyed collections should specify a StringComparer";
		public const string MessageFormat = @"{0} with a string key should explicitly specify a {1}";
		private const string Description = @"String-keyed collections should explicitly specify a StringComparer (or IComparer<string> for sorted collections) to avoid culture-dependent or case-sensitivity surprises.";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule = new(DiagnosticId.StringDictionaryNeedsComparer.ToId(), Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			var wellKnownTypes = new WellKnownTypes(context.Compilation);

			// Debug: Only proceed if we have the basic types
			if (wellKnownTypes.String == null || wellKnownTypes.Dictionary_T2 == null)
			{
				return;
			}

			context.RegisterOperationAction(ctx => AnalyzeObjectCreation(ctx, wellKnownTypes), OperationKind.ObjectCreation);
			context.RegisterOperationAction(ctx => AnalyzeInvocation(ctx, wellKnownTypes), OperationKind.Invocation);
		}

		private static void AnalyzeObjectCreation(OperationAnalysisContext context, WellKnownTypes types)
		{
			var creation = (IObjectCreationOperation)context.Operation;
			if (creation.Type is not INamedTypeSymbol constructed)
			{
				return;
			}

			// Determine if this is one of the supported collection types with string key
			if (!TryGetCollectionInfo(constructed, types, out _, out RequiredComparer requiresComparerType))
			{
				return;
			}

			// If a comparer argument is already provided, nothing to do
			IMethodSymbol ctor = creation.Constructor;
			if (ctor == null)
			{
				return;
			}

			if (HasRequiredComparerParameter(ctor, requiresComparerType, types))
			{
				return; // explicit comparer provided
			}

			// No comparer in the selected overload → report
			var collectionDisplay = constructed.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
			var comparerDisplay = requiresComparerType == RequiredComparer.IEqualityComparer
				? "StringComparer/IEqualityComparer<string>"
				: "StringComparer/IComparer<string>";

			context.ReportDiagnostic(Diagnostic.Create(
				Rule,
				creation.Syntax.GetLocation(),
				collectionDisplay,
				comparerDisplay));
		}

		private static void AnalyzeInvocation(OperationAnalysisContext context, WellKnownTypes types)
		{
			var invocation = (IInvocationOperation)context.Operation;
			IMethodSymbol targetMethod = invocation.TargetMethod;

			// ImmutableDictionary.Create<TKey, TValue>(...) without comparer
			if (types.ImmutableDictionary != null && targetMethod.ContainingType.Equals(types.ImmutableDictionary, SymbolEqualityComparer.Default))
			{
				if (targetMethod.Name is "Create" or "CreateRange")
				{
					ImmutableArray<ITypeSymbol> typeArgs = targetMethod.TypeArguments;
					if (typeArgs.Length == 2 && SymbolEqualityComparer.Default.Equals(typeArgs[0], types.String))
					{
						if (!HasRequiredComparerParameter(targetMethod, RequiredComparer.IEqualityComparer, types))
						{
							var collectionDisplay = $"ImmutableDictionary<{typeArgs[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}, {typeArgs[1].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}>";
							context.ReportDiagnostic(Diagnostic.Create(
								Rule,
								invocation.Syntax.GetLocation(),
								collectionDisplay,
								"StringComparer/IEqualityComparer<string>"));
						}
					}
				}
			}
		}

		private enum RequiredComparer { IEqualityComparer, IComparer }

		private static bool TryGetCollectionInfo(INamedTypeSymbol constructed, WellKnownTypes types, out string kind, out RequiredComparer requires)
		{
			kind = default;
			requires = default;

			// HashSet<string>
			if (types.HashSet_T != null && constructed.ConstructedFrom.Equals(types.HashSet_T, SymbolEqualityComparer.Default))
			{
				if (constructed.TypeArguments.Length == 1 && SymbolEqualityComparer.Default.Equals(constructed.TypeArguments[0], types.String))
				{
					kind = "HashSet";
					requires = RequiredComparer.IEqualityComparer;
					return true;
				}
				return false;
			}

			// Dictionary<string, TValue>
			if (types.Dictionary_T2 != null && constructed.ConstructedFrom.Equals(types.Dictionary_T2, SymbolEqualityComparer.Default))
			{
				if (constructed.TypeArguments.Length == 2 && SymbolEqualityComparer.Default.Equals(constructed.TypeArguments[0], types.String))
				{
					kind = "Dictionary";
					requires = RequiredComparer.IEqualityComparer;
					return true;
				}
				return false;
			}

			// ConcurrentDictionary<string, TValue>
			if (types.ConcurrentDictionary_T2 != null && constructed.ConstructedFrom.Equals(types.ConcurrentDictionary_T2, SymbolEqualityComparer.Default))
			{
				if (constructed.TypeArguments.Length == 2 && SymbolEqualityComparer.Default.Equals(constructed.TypeArguments[0], types.String))
				{
					kind = "ConcurrentDictionary";
					requires = RequiredComparer.IEqualityComparer;
					return true;
				}
				return false;
			}

			// SortedDictionary<string, TValue>
			if (types.SortedDictionary_T2 != null && constructed.ConstructedFrom.Equals(types.SortedDictionary_T2, SymbolEqualityComparer.Default))
			{
				if (constructed.TypeArguments.Length == 2 && SymbolEqualityComparer.Default.Equals(constructed.TypeArguments[0], types.String))
				{
					kind = "SortedDictionary";
					requires = RequiredComparer.IComparer;
					return true;
				}
				return false;
			}

			// SortedSet<string>
			if (types.SortedSet_T != null && constructed.ConstructedFrom.Equals(types.SortedSet_T, SymbolEqualityComparer.Default))
			{
				if (constructed.TypeArguments.Length == 1 && SymbolEqualityComparer.Default.Equals(constructed.TypeArguments[0], types.String))
				{
					kind = "SortedSet";
					requires = RequiredComparer.IComparer;
					return true;
				}
				return false;
			}

			// ImmutableDictionary<string, TValue>
			if (types.ImmutableDictionary_T2 != null && constructed.ConstructedFrom.Equals(types.ImmutableDictionary_T2, SymbolEqualityComparer.Default))
			{
				if (constructed.TypeArguments.Length == 2 && SymbolEqualityComparer.Default.Equals(constructed.TypeArguments[0], types.String))
				{
					kind = "ImmutableDictionary";
					requires = RequiredComparer.IEqualityComparer;
					return true;
				}
				return false;
			}

			return false;
		}

		private static bool HasRequiredComparerParameter(IMethodSymbol method, RequiredComparer requires, WellKnownTypes types)
		{
			foreach (IParameterSymbol param in method.Parameters)
			{
				if (requires == RequiredComparer.IEqualityComparer &&
					types.IEqualityComparer_T != null &&
					param.Type is INamedTypeSymbol p &&
					p.OriginalDefinition.Equals(types.IEqualityComparer_T, SymbolEqualityComparer.Default) &&
					p.TypeArguments.Length == 1 && SymbolEqualityComparer.Default.Equals(p.TypeArguments[0], types.String))
				{
					return true;
				}

				if (requires == RequiredComparer.IComparer &&
					types.IComparer_T != null &&
					param.Type is INamedTypeSymbol p2 &&
					p2.OriginalDefinition.Equals(types.IComparer_T, SymbolEqualityComparer.Default) &&
					p2.TypeArguments.Length == 1 && SymbolEqualityComparer.Default.Equals(p2.TypeArguments[0], types.String))
				{
					return true;
				}
			}

			return false;
		}

		private sealed class WellKnownTypes
		{
			public INamedTypeSymbol String { get; }
			public INamedTypeSymbol IEqualityComparer_T { get; }
			public INamedTypeSymbol IComparer_T { get; }
			public INamedTypeSymbol Dictionary_T2 { get; }
			public INamedTypeSymbol HashSet_T { get; }
			public INamedTypeSymbol ConcurrentDictionary_T2 { get; }
			public INamedTypeSymbol SortedDictionary_T2 { get; }
			public INamedTypeSymbol SortedSet_T { get; }
			public INamedTypeSymbol ImmutableDictionary_T2 { get; }
			public INamedTypeSymbol ImmutableDictionary { get; }

			public WellKnownTypes(Compilation compilation)
			{
				String = compilation.GetSpecialType(SpecialType.System_String);
				IEqualityComparer_T = compilation.GetTypeByMetadataName("System.Collections.Generic.IEqualityComparer`1");
				IComparer_T = compilation.GetTypeByMetadataName("System.Collections.Generic.IComparer`1");
				Dictionary_T2 = compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2");
				HashSet_T = compilation.GetTypeByMetadataName("System.Collections.Generic.HashSet`1");
				ConcurrentDictionary_T2 = compilation.GetTypeByMetadataName("System.Collections.Concurrent.ConcurrentDictionary`2");
				SortedDictionary_T2 = compilation.GetTypeByMetadataName("System.Collections.Generic.SortedDictionary`2");
				SortedSet_T = compilation.GetTypeByMetadataName("System.Collections.Generic.SortedSet`1");
				ImmutableDictionary_T2 = compilation.GetTypeByMetadataName("System.Collections.Immutable.ImmutableDictionary`2");
				ImmutableDictionary = compilation.GetTypeByMetadataName("System.Collections.Immutable.ImmutableDictionary");
			}
		}
	}
}