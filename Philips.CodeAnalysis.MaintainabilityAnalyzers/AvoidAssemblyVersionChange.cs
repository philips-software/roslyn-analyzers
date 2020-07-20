// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidAssemblyVersionChangeAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Avoid AssemblyVersion change";
		private const string MessageFormat = @"AssemblyVersion has changed. Actual: {0} Expected: {1}";
		private const string Description = @"AssemblyVersion breaks compatibility.  If intentional, update EditorConfig.";
		private const string Category = Categories.RuntimeFailure;

		private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.AvoidAssemblyVersionChange), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.CompilationUnit);
		}

		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			var analyzerConfigOptions = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Compilation.SyntaxTrees.First());

			Version expectedVersion = new Version(@"1.0.0.0");
#nullable enable
			if (analyzerConfigOptions.TryGetValue(@"dotnet_code_quality.PH2075.assembly_version", out string? value))
			{
				try
				{
					expectedVersion = new Version(value.ToString());
				}
				catch (Exception)
				{
				}
			}
#nullable disable

			//			CompilationUnitSyntax node = (CompilationUnitSyntax)context.Node;
			Version actualVersion = context.Compilation.Assembly.Identity.Version;
			if (actualVersion.CompareTo(expectedVersion) != 0)
			{
				Diagnostic diagnostic = Diagnostic.Create(Rule, null, actualVersion.ToString(), expectedVersion.ToString());
				context.ReportDiagnostic(diagnostic);
			}
		}
	}
}
