// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidNoWarnAnalyzerSuppressionAnalyzer : SolutionAnalyzer
	{
		private const string Title = @"Avoid NoWarn for analyzer suppression";
		private const string MessageFormat = @"Use .editorconfig instead of NoWarn project setting to suppress analyzer '{0}'. Configure with 'dotnet_diagnostic.{0}.severity = none'.";
		private const string Description = @"NoWarn project settings should be avoided for analyzer suppression. Use .editorconfig files for better maintainability and team consistency.";

		private static readonly char[] SeparatorChars = { ';', ',' };

		public AvoidNoWarnAnalyzerSuppressionAnalyzer()
			: base(DiagnosticId.AvoidNoWarnAnalyzerSuppression, Title, MessageFormat, Description, Categories.Maintainability, isEnabled: false)
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
			try
			{
				var projectFilePath = TryFindProjectFileFromSourcePaths(context);
				if (string.IsNullOrEmpty(projectFilePath) || !File.Exists(projectFilePath))
				{
					return;
				}

				AnalyzeProjectFile(context, projectFilePath);
			}
			catch (Exception)
			{
				// Silently ignore errors to avoid breaking compilation
				return;
			}
		}

		private void AnalyzeProjectFile(CompilationAnalysisContext context, string projectFilePath)
		{
			try
			{
				var content = File.ReadAllText(projectFilePath);
				var document = XDocument.Parse(content);

				IEnumerable<XElement> noWarnElements = document.Descendants()
					.Where(e => e.Name.LocalName.Equals("NoWarn", StringComparison.OrdinalIgnoreCase));

				foreach (XElement noWarnElement in noWarnElements)
				{
					var noWarnValue = noWarnElement.Value;
					if (string.IsNullOrWhiteSpace(noWarnValue))
					{
						continue;
					}

					// Find PH analyzer codes in NoWarn value
					List<string> phCodes = ExtractAnalyzerCodes(noWarnValue);
					foreach (var phCode in phCodes)
					{
						var diagnostic = Diagnostic.Create(Rule, Location.None, phCode);
						context.ReportDiagnostic(diagnostic);
					}
				}
			}
			catch (Exception)
			{
				// Silently ignore XML parsing errors
				return;
			}
		}

		private static List<string> ExtractAnalyzerCodes(string noWarnValue)
		{
			var codes = new List<string>();

			// Split by semicolons and commas, common separators in NoWarn values
			var parts = noWarnValue.Split(SeparatorChars,
											StringSplitOptions.RemoveEmptyEntries);

			foreach (var part in parts)
			{
				var trimmedPart = part.Trim();
				// Look for PH codes (PH followed by digits)
				if (Regex.IsMatch(trimmedPart, @"^PH\d+$", RegexOptions.IgnoreCase))
				{
					codes.Add(trimmedPart.ToUpperInvariant());
				}
			}

			return codes;
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