// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidAssemblyVersionChangeAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Avoid AssemblyVersion change";
		private const string MessageFormat = @"AssemblyVersion has changed. Actual: {0} Expected: {1}";
		private const string Category = Categories.RuntimeFailure;
		private static readonly string Description =
			"AssemblyVersion breaks compatibility.  If intentional, specify dotnet_code_quality." +
			Helper.ToDiagnosticId(DiagnosticId.AvoidAssemblyVersionChange) +
			".assembly_version in EditorConfig.";

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticId.AvoidAssemblyVersionChange), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		private const string InvalidExpectedVersionTitle = @"The assembly_version specified in the EditorConfig is invalid.";
		private const string InvalidExpectedVersionMessage = @"The assembly_version {0} specified in the EditorConfig is invalid.";
		private static readonly DiagnosticDescriptor InvalidExpectedVersionRule = new(Helper.ToDiagnosticId(DiagnosticId.AvoidAssemblyVersionChange), InvalidExpectedVersionTitle,
			InvalidExpectedVersionMessage, Category, DiagnosticSeverity.Error, true, Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule, InvalidExpectedVersionRule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterCompilationAction(Analyze);
		}

		private void Analyze(CompilationAnalysisContext context)
		{
			Version expectedVersion = new(@"1.0.0.0");
			var additionalFilesHelper = new AdditionalFilesHelper(context.Options, context.Compilation);
			string value = additionalFilesHelper.GetValueFromEditorConfig(Rule.Id, @"assembly_version");
			if (!string.IsNullOrWhiteSpace(value))
			{
				bool isParseSuccessful = Version.TryParse(value.ToString(), out Version parsedVersion);

				if (!isParseSuccessful)
				{
					Diagnostic diagnostic = Diagnostic.Create(InvalidExpectedVersionRule, null, value);
					context.ReportDiagnostic(diagnostic);
					return;
				}
				expectedVersion = parsedVersion;
			}

			Version actualVersion = GetCompilationAssemblyVersion(context.Compilation);
			if (actualVersion.CompareTo(expectedVersion) != 0)
			{
				Diagnostic diagnostic = Diagnostic.Create(Rule, null, actualVersion.ToString(), expectedVersion.ToString());
				context.ReportDiagnostic(diagnostic);
			}
		}

		/// <summary>
		/// To be overridden in test code only.
		/// </summary>
		protected virtual Version GetCompilationAssemblyVersion(Compilation compilation)
		{
			return compilation.Assembly.Identity.Version;
		}
	}
}
