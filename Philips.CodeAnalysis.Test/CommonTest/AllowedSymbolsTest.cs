// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.Test.CommonTest
{
	/// <summary>
	/// Test class for <see cref="AllowedSymbols"/>
	/// </summary>
	[TestClass]
	public class AllowedSymbolsTest : DiagnosticVerifier
	{
		private const string AllowedMethodContent = @"
# Comment line
// Another comment line
; Another comment line
AllowedMethodName
~N:AllowedNamespace # With comment on same line
~T:ANamespace.AllowedType ; With comment on same line
~M:ANamespace.AType.AllowedMethod() // With comment on same line
";

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new AllowedSymbolsTestAnalyzer();
		}

		protected override (string name, string content)[] GetAdditionalTexts()
		{
			return new [] { ("NotFile.txt", "data"), (AllowedSymbolsTestAnalyzer.AllowedFileName, AllowedMethodContent) };
		}

		/// <summary>
		/// The analyzer reports a <see cref="Diagnostic"/> on each of its AllowedSymbols. In production analyzers the allowed symbols is used reciprocally.
		/// </summary>
		[DiagnosticAnalyzer(LanguageNames.CSharp)]
		public class AllowedSymbolsTestAnalyzer : DiagnosticAnalyzer
		{
			public const string AllowedFileName = "AllowedSymbolsTest.Allowed.txt";

			private readonly AllowedSymbols _allowedSymbols = new();

			public override void Initialize(AnalysisContext context)
			{
				context.RegisterCompilationStartAction(AnalyzeCompilationStart);
				context.EnableConcurrentExecution();
				context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
				context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
			}

			[SuppressMessage("Build", "RS1012")]
			private void AnalyzeCompilationStart(CompilationStartAnalysisContext context)
			{
				foreach (var file in context.Options.AdditionalFiles)
				{
					if (file.Path.Contains(AllowedFileName))
					{
						var text = file.GetText();
						_allowedSymbols.LoadAllowedMethods(text, context.Compilation);
					}
				}
			}

			private void AnalyzeMethod(SymbolAnalysisContext context)
			{
				var methodSymbol = context.Symbol as IMethodSymbol;
				if (_allowedSymbols.IsAllowed(methodSymbol))
				{
					var loc = methodSymbol.Locations[0];
					context.ReportDiagnostic(Diagnostic.Create(Rule, loc));
				}
			}

			public static DiagnosticDescriptor Rule => 
				new("DUMMY0001", "AllowedSymbols", "AllowedSymbolsFound", "", DiagnosticSeverity.Error, true);

			public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create<DiagnosticDescriptor>(Rule);
		}

		[DataTestMethod]
		[DataRow("AllowedNamespace", "SomeType", "SomeMethod")]
		[DataRow("ANamespace", "AllowedType", "SomeMethod")]
		[DataRow("ANamespace", "AType", "AllowedMethod")]
		[DataRow("ANamespace", "SomeType", "AllowedMethodName")]
		public void AllowedSymbolShouldBeReportDiagnostics(string nsName, string typeName, string methodName)
		{
			var file = GenerateCodeFile(nsName, typeName, methodName);
			VerifyDiagnostic(file);
		}

		[DataTestMethod]
		[DataRow("SomeNamespace", "SomeType", "SomeMethod")]
		[DataRow("ANamespace", "AType", "AllowedMethod2")]
		public void NotAllowedSymbolShouldNotReportDiagnostics(string nsName, string typeName, string methodName)
		{
			var file = GenerateCodeFile(nsName, typeName, methodName);
			VerifyCSharpDiagnostic(file, Array.Empty<DiagnosticResult>());
		}

		private string GenerateCodeFile(string nsName, string typeName, string methodName)
		{
			return
				$"namespace {nsName} {{\npublic class {typeName}\n{{\nprivate void {methodName}()\n{{\nreturn;\n}}\n}}\n}}\n";
		}

		private void VerifyDiagnostic(string file)
		{
			VerifyCSharpDiagnostic(file,
				new DiagnosticResult()
				{
					Id = AllowedSymbolsTestAnalyzer.Rule.Id,
					Message = new Regex("AllowedSymbolsFound"),
					Severity = DiagnosticSeverity.Error,
					Locations = new[]
					{
						new DiagnosticResultLocation("Test0.cs", null, null)
					}
				}
			);
		}
	}
}
