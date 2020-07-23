// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidAssemblyVersionChangeAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Avoid AssemblyVersion change";
		private const string MessageFormat = @"AssemblyVersion has changed. Actual: {0} Expected: {1}";
		private const string Description = @"AssemblyVersion breaks compatibility.  If intentional, specify assembly_version in EditorConfig.";
		private const string Category = Categories.RuntimeFailure;

		private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.AvoidAssemblyVersionChange), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterCompilationAction(Analyze);
		}

		private static void Analyze(CompilationAnalysisContext context)
		{
			Version expectedVersion = new Version(@"1.0.0.0");
			try
			{
				var additionalFilesHelper = new AdditionalFilesHelper(context.Options, context.Compilation);
				string value = additionalFilesHelper.GetValueFromEditorConfig(Rule.Id, @"assembly_version");
				if (!string.IsNullOrWhiteSpace(value))
				{
					expectedVersion = new Version(value.ToString());
				}
			}
			catch (Exception)
			{ }

			Version actualVersion = context.Compilation.Assembly.Identity.Version;
			if (actualVersion.CompareTo(expectedVersion) != 0)
			{
				Diagnostic diagnostic = Diagnostic.Create(Rule, null, actualVersion.ToString(), expectedVersion.ToString());
				context.ReportDiagnostic(diagnostic);
			}
		}
	}
}
