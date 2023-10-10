// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.Test.Common
{
	/// <summary>
	/// The analyzer reports a <see cref="Diagnostic"/> on each of its AllowedSymbols. In production analyzers the allowed symbols is used reciprocally.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AllowedSymbolsTestAnalyzer : SolutionAnalyzer
	{
		public const string AllowedFileName = "AllowedSymbolsTest.Allowed.txt";

		private readonly bool _shouldCheckMethods;
		private Helper _helper;

		public AllowedSymbolsTestAnalyzer(bool shouldCheckMethods) : base(DiagnosticId.AssertAreEqual, "AllowedSymbols", "AllowedSymbolsFound", "", "", DiagnosticSeverity.Error, true)
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
			_helper = new Helper(context.Options, context.Compilation);
			var hasAdditionalFile = _helper.ForAllowedSymbols.Initialize(context.Options.AdditionalFiles, AllowedFileName);
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
			if (_helper.ForAllowedSymbols.IsAllowed(methodSymbol))
			{
				Location loc = methodSymbol.Locations[0];
				context.ReportDiagnostic(Diagnostic.Create(Rule, loc));
			}
		}

		private void AnalyzeType(SymbolAnalysisContext context)
		{
			var typeSymbol = context.Symbol as INamedTypeSymbol;
			if (_helper.ForAllowedSymbols.IsAllowed(typeSymbol))
			{
				Location loc = typeSymbol.Locations[0];
				context.ReportDiagnostic(Diagnostic.Create(Rule, loc));
			}
		}
	}
}
