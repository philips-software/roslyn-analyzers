// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidNoWarnAnalyzerSuppressionAnalyzer : SolutionAnalyzer
	{
		private const string Title = @"Avoid NoWarn for analyzer suppression";
		private const string MessageFormat = @"Use .editorconfig instead of NoWarn project setting to suppress diagnostics. Configure with 'dotnet_diagnostic.{id}.severity = none'.";
		private const string Description = @"NoWarn project settings should be avoided for diagnostic suppression. Use .editorconfig files for better maintainability and team consistency.";

		public AvoidNoWarnAnalyzerSuppressionAnalyzer()
			: base(DiagnosticId.AvoidNoWarnAnalyzerSuppression, Title, MessageFormat, Description, Categories.Maintainability, isEnabled: true)
		{
		}

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterCompilationAction(AnalyzeCompilation);
		}

		private void AnalyzeCompilation(CompilationAnalysisContext context)
		{
			var projectFilePath = TryFindProjectFileFromSourcePaths(context);
			if (string.IsNullOrEmpty(projectFilePath) || !File.Exists(projectFilePath))
			{
				return;
			}

			AnalyzeProjectFile(context, projectFilePath);
		}

		private void AnalyzeProjectFile(CompilationAnalysisContext context, string projectFilePath)
		{
			var content = File.ReadAllText(projectFilePath);

			// Simple check for NoWarn elements - avoid XDocument overhead
			var lowerContent = content.ToLowerInvariant();
			if (lowerContent.Contains("<nowarn>") || lowerContent.Contains("<nowarn "))
			{
				var diagnostic = Diagnostic.Create(Rule, Location.None);
				context.ReportDiagnostic(diagnostic);
			}
		}

		private string TryFindProjectFileFromSourcePaths(CompilationAnalysisContext context)
		{
			List<string> sourceDirectories = GetSourceDirectories(context);

			foreach (var sourceDir in sourceDirectories)
			{
				var foundPath = SearchForProjectFile(sourceDir);
				if (foundPath != null)
				{
					return foundPath;
				}
			}

			return null;
		}

		private static List<string> GetSourceDirectories(CompilationAnalysisContext context)
		{
			return context.Compilation.SyntaxTrees
				.Where(tree => !string.IsNullOrEmpty(tree.FilePath))
				.Select(tree => Path.GetDirectoryName(tree.FilePath))
				.Where(dir => !string.IsNullOrEmpty(dir))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		private static string SearchForProjectFile(string startDirectory)
		{
			if (string.IsNullOrEmpty(startDirectory) || !Directory.Exists(startDirectory))
			{
				return null;
			}

			var currentDir = new DirectoryInfo(startDirectory);

			// Search up the directory tree for project files
			while (currentDir != null)
			{
				IEnumerable<FileInfo> projectFiles = currentDir.GetFiles("*.csproj").Concat(currentDir.GetFiles("*.vbproj"));
				FileInfo projectFile = projectFiles.FirstOrDefault();

				if (projectFile != null)
				{
					return projectFile.FullName;
				}

				currentDir = currentDir.Parent;
			}

			return null;
		}
	}
}