// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Philips.CodeAnalysis.Test
{
	/// <summary>
	/// Class for turning strings into documents and getting the diagnostics on them
	/// All methods are static
	/// </summary>
	public abstract partial class DiagnosticVerifier
	{
		private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
		private static readonly MetadataReference SystemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
		private static readonly MetadataReference CSharpSymbolsReference = MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location);
		private static readonly MetadataReference CodeAnalysisReference = MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location);
		private static readonly MetadataReference UnitTestingReference = MetadataReference.CreateFromFile(typeof(DescriptionAttribute).Assembly.Location);
		private static readonly MetadataReference ThreadingReference = MetadataReference.CreateFromFile(typeof(Thread).Assembly.Location);

		internal static string DefaultFilePathPrefix = "Test";
		internal static string CSharpDefaultFileExt = "cs";
		internal static string VisualBasicDefaultExt = "vb";
		internal static string TestProjectName = "TestProject";

		#region  Get Diagnostics

		/// <summary>
		/// Given classes in the form of strings, their language, and an IDiagnosticAnalyzer to apply to it, return the diagnostics found in the string after converting it to a document.
		/// </summary>
		/// <param name="sources">Classes in the form of strings</param>
		/// <param name="language">The language the source classes are in</param>
		/// <param name="analyzer">The analyzer to be run on the sources</param>
		/// <returns>An IEnumerable of Diagnostics that surfaced in the source code, sorted by Location</returns>
		private Diagnostic[] GetSortedDiagnostics(string[] sources, string filenamePrefix, string language, DiagnosticAnalyzer analyzer)
		{
			return GetSortedDiagnosticsFromDocuments(analyzer, GetDocuments(sources, filenamePrefix, language));
		}

		private class TestAdditionalText : AdditionalText
		{
			private SourceText _sourceText;

			public override string Path { get; }

			public TestAdditionalText(string path, SourceText sourceText)
			{
				Path = path;
				_sourceText = sourceText;
			}

			public override SourceText GetText(CancellationToken cancellationToken = default)
			{
				return _sourceText;
			}
		}

		/// <summary>
		/// Given an analyzer and a document to apply it to, run the analyzer and gather an array of diagnostics found in it.
		/// The returned diagnostics are then ordered by location in the source document.
		/// </summary>
		/// <param name="analyzer">The analyzer to run on the documents</param>
		/// <param name="documents">The Documents that the analyzer will be run on</param>
		/// <returns>An IEnumerable of Diagnostics that surfaced in the source code, sorted by Location</returns>
		protected Diagnostic[] GetSortedDiagnosticsFromDocuments(DiagnosticAnalyzer analyzer, Document[] documents)
		{
			var projects = new HashSet<Project>();
			foreach (var document in documents)
			{
				projects.Add(document.Project);
			}

			var diagnostics = new List<Diagnostic>();
			foreach (var project in projects)
			{
				var compilation = project.GetCompilationAsync().Result;

				var specificOptions = compilation.Options.SpecificDiagnosticOptions;

				foreach (var diagnostic in analyzer.SupportedDiagnostics)
				{
					if (!diagnostic.IsEnabledByDefault)
					{
						specificOptions = specificOptions.Add(diagnostic.Id, ReportDiagnostic.Error);
					}
				}

				var modified = compilation.WithOptions(compilation.Options.WithSpecificDiagnosticOptions(specificOptions));

				List<AdditionalText> additionalTextsBuilder = new List<AdditionalText>();
				foreach (var (name, content) in GetAdditionalTexts())
				{
					additionalTextsBuilder.Add(new TestAdditionalText(name, SourceText.From(content)));
				}

				AnalyzerOptions analyzerOptions = new AnalyzerOptions(ImmutableArray.ToImmutableArray(additionalTextsBuilder));

				var compilationWithAnalyzers = modified.WithAnalyzers(ImmutableArray.Create(analyzer), options: analyzerOptions);

				var diags = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
				foreach (var diag in diags)
				{
					if (diag.Location == Location.None || diag.Location.IsInMetadata)
					{
						diagnostics.Add(diag);
					}
					else
					{
						for (int i = 0; i < documents.Length; i++)
						{
							var document = documents[i];
							var tree = document.GetSyntaxTreeAsync().Result;
							if (tree == diag.Location.SourceTree)
							{
								diagnostics.Add(diag);
							}
						}
					}
				}
			}

			var results = SortDiagnostics(diagnostics);
			diagnostics.Clear();
			return results;
		}

		/// <summary>
		/// Sort diagnostics by location in source document
		/// </summary>
		/// <param name="diagnostics">The list of Diagnostics to be sorted</param>
		/// <returns>An IEnumerable containing the Diagnostics in order of Location</returns>
		private static Diagnostic[] SortDiagnostics(IEnumerable<Diagnostic> diagnostics)
		{
			return diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
		}

		#endregion

		#region Set up compilation and documents
		/// <summary>
		/// Given an array of strings as sources and a language, turn them into a project and return the documents and spans of it.
		/// </summary>
		/// <param name="sources">Classes in the form of strings</param>
		/// <param name="language">The language the source code is in</param>
		/// <returns>A Tuple containing the Documents produced from the sources and their TextSpans if relevant</returns>
		private Document[] GetDocuments(string[] sources, string filenamePrefix, string language)
		{
			if (language != LanguageNames.CSharp && language != LanguageNames.VisualBasic)
			{
				throw new ArgumentException("Unsupported Language");
			}

			var project = CreateProject(sources, filenamePrefix, language);
			var documents = project.Documents.ToArray();

			if (sources.Length != documents.Length)
			{
				throw new InvalidOperationException("Amount of sources did not match amount of Documents created");
			}

			return documents;
		}

		/// <summary>
		/// Create a Document from a string through creating a project that contains it.
		/// </summary>
		/// <param name="source">Classes in the form of a string</param>
		/// <param name="language">The language the source code is in</param>
		/// <returns>A Document created from the source string</returns>
		protected Document CreateDocument(string source, string language = LanguageNames.CSharp)
		{
			return CreateProject(new[] { source }, language).Documents.First();
		}

		protected virtual MetadataReference[] GetMetadataReferences()
		{
			return Array.Empty<MetadataReference>();
		}

		protected virtual (string name, string content)[] GetAdditionalTexts()
		{
			return Array.Empty<(string name, string content)>();
		}

		/// <summary>
		/// Create a project using the inputted strings as sources.
		/// </summary>
		/// <param name="sources">Classes in the form of strings</param>
		/// <param name="language">The language the source code is in</param>
		/// <returns>A Project created out of the Documents created from the source strings</returns>
		private Project CreateProject(string[] sources, string fileNamePrefix = null, string language = LanguageNames.CSharp)
		{
			bool isCustomPrefix = fileNamePrefix != null;
			fileNamePrefix = fileNamePrefix ?? DefaultFilePathPrefix;
			string fileExt = language == LanguageNames.CSharp ? CSharpDefaultFileExt : VisualBasicDefaultExt;

			var projectId = ProjectId.CreateNewId(debugName: TestProjectName);

			var solution = new AdhocWorkspace()
				.CurrentSolution
				.AddProject(projectId, TestProjectName, TestProjectName, language)
				.AddMetadataReference(projectId, CorlibReference)
				.AddMetadataReference(projectId, SystemCoreReference)
				.AddMetadataReference(projectId, CSharpSymbolsReference)
				.AddMetadataReference(projectId, CodeAnalysisReference)
				.AddMetadataReference(projectId, UnitTestingReference)
				.AddMetadataReference(projectId, ThreadingReference);

			foreach (MetadataReference testReferences in GetMetadataReferences())
			{
				solution = solution.AddMetadataReference(projectId, testReferences);
			}

			var trustedAssembliesPaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator);
			var neededAssemblies = new[]
			{
	"System.Runtime",
	"mscorlib",
};
			foreach (MetadataReference references in trustedAssembliesPaths.Where(p => neededAssemblies.Contains(Path.GetFileNameWithoutExtension(p)))
				.Select(p => MetadataReference.CreateFromFile(p)))
			{
				solution = solution.AddMetadataReference(projectId, references);
			}

			foreach (var m in solution.GetProject(projectId).MetadataReferences)
			{
				Trace.WriteLine($"{m.Display}: {m.Properties}");
			}

			int count = 0;
			foreach (var source in sources)
			{
				var newFileName = string.Format("{0}{1}.{2}", fileNamePrefix, count == 0 ? (isCustomPrefix ? string.Empty : count.ToString()) : count.ToString(), fileExt);
				var documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
				solution = solution.AddDocument(documentId, newFileName, SourceText.From(source));
				count++;
			}
			return solution.GetProject(projectId);
		}
		#endregion
	}
}

