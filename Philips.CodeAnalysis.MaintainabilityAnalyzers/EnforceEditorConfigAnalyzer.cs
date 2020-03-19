#region Header
// © 2019 Koninklijke Philips N.V.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior  written consent of 
// the owner.
// Author:      Brian.Collamore
// Date:        12/3/2019
#endregion

using System;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.Healthcare.RoslynAnalyzer
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
