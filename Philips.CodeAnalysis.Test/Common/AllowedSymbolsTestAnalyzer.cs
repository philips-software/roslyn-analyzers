// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.Test.Common
{
	/// <summary>
	/// The analyzer reports a <see cref="Diagnostic"/> on each of its AllowedSymbols. In production analyzers the allowed symbols is used reciprocally.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AllowedSymbolsTestAnalyzer : DiagnosticAnalyzer
	{
		public const string AllowedFileName = "AllowedSymbolsTest.Allowed.txt";

		private AllowedSymbols _allowedSymbols;
		private readonly bool _shouldCheckMethods;

		public AllowedSymbolsTestAnalyzer(bool shouldCheckMethods)
		{
			_shouldCheckMethods = shouldCheckMethods;
		}

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterCompilationStartAction(AnalyzeCompilationStart);
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

		}

		private void AnalyzeCompilationStart(CompilationStartAnalysisContext context)
		{
			_allowedSymbols = new AllowedSymbols(context.Compilation);
			var hasAdditionalFile = _allowedSymbols.Initialize(context.Options.AdditionalFiles, AllowedFileName);
			if (!hasAdditionalFile)
			{
				throw new ArgumentException("AllowedFileName");
			}

			if (_shouldCheckMethods)
			{
				context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
			}
			else
			{
				context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
			}
		}

		private void AnalyzeMethod(SymbolAnalysisContext context)
		{
			var methodSymbol = context.Symbol as IMethodSymbol;
			if (_allowedSymbols.IsAllowed(methodSymbol))
			{
				Location loc = methodSymbol.Locations[0];
				context.ReportDiagnostic(Diagnostic.Create(Rule, loc));
			}
		}

		private void AnalyzeType(SymbolAnalysisContext context)
		{
			var typeSymbol = context.Symbol as INamedTypeSymbol;
			if (_allowedSymbols.IsAllowed(typeSymbol))
			{
				Location loc = typeSymbol.Locations[0];
				context.ReportDiagnostic(Diagnostic.Create(Rule, loc));
			}
		}

		public static DiagnosticDescriptor Rule =>
			new("DUMMY0001", "AllowedSymbols", "AllowedSymbolsFound", "", DiagnosticSeverity.Error, true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
	}
}
