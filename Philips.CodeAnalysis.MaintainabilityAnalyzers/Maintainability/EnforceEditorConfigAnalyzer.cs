// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class EnforceEditorConfigAnalyzer : CompilationAnalyzer
	{
		private const string Title = @"Missing .editorconfig";
		private const string MessageFormat = @"The project does not have a local .editorconfig file.";
		private const string Description = @".editorconfig files help enforce and configure Analyzers";

		public EnforceEditorConfigAnalyzer()
			: base(DiagnosticId.EnforceEditorConfig, Title, MessageFormat, Description, Categories.Maintainability, isEnabled: false)
		{ }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterCompilationAction(compilationContext =>
			{
				var isPresent = IsLocalEditorConfigPresent(compilationContext.Options.AdditionalFiles);
				if (!isPresent)
				{
					var diagnostic = Diagnostic.Create(Rule, Location.None);
					compilationContext.ReportDiagnostic(diagnostic);
				}
			});
		}

		public virtual bool IsLocalEditorConfigPresent(ImmutableArray<AdditionalText> additionalFiles)
		{
			foreach (AdditionalText additionalFile in additionalFiles)
			{
				var fileName = Path.GetFileName(additionalFile.Path);
				StringComparer comparer = StringComparer.OrdinalIgnoreCase;
				if (comparer.Equals(fileName, @".editorconfig") && additionalFile.GetText() != null)
				{
					return true;
				}
			}
			return false;
		}
	}
}
