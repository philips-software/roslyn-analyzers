﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class EnforceEditorConfigAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Missing .editorconfig";
		private const string MessageFormat = @"The project does not have an .editorconfig file.";
		private const string Description = @".editorconfig files help enforce and configure Analyzers";
		private const string Category = Categories.Maintainability;

		public static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.EnforceEditorConfig), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterCompilationAction(compilationContext =>
			{
				bool result = IsEditorConfigPresent(compilationContext.Options.AdditionalFiles);
				if (!result)
				{
					Diagnostic diagnostic = Diagnostic.Create(Rule, Location.None);
					compilationContext.ReportDiagnostic(diagnostic);
				}
			});
		}

		public virtual bool IsEditorConfigPresent(ImmutableArray<AdditionalText> additionalFiles)
		{
			foreach (AdditionalText additionalFile in additionalFiles)
			{
				string fileName = Path.GetFileName(additionalFile.Path);
				StringComparer comparer = StringComparer.OrdinalIgnoreCase;
				if (comparer.Equals(fileName, @".editorconfig"))
				{
					if (additionalFile.GetText() != null)
					{
						return true;
					}
				}
			}
			return false;
		}
	}
}
