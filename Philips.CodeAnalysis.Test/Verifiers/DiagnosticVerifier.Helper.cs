// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Test.Helpers;

namespace Philips.CodeAnalysis.Test.Verifiers
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
		private static readonly MetadataReference GeneratedCodeReference = MetadataReference.CreateFromFile(typeof(GeneratedCodeAttribute).Assembly.Location);

		internal static string DefaultFilePathPrefix = "Test";
		internal static string CSharpDefaultFileExt = "cs";
		internal static string TestProjectName = "TestProject";

		#region  Get Diagnostics

		/// <summary>
		/// Given classes in the form of strings, their language, and an IDiagnosticAnalyzer to apply to it, return the diagnostics found in the string after converting it to a document.
		/// </summary>
		/// <param name="sources">Classes in the form of strings</param>
		/// <param name="filenamePrefix">The name of the source file, without the extension</param>
		/// <param name="assemblyName">The name of the resulting assembly of the compilation, without the extension</param>
		/// <param name="analyzer">The analyzer to be run on the sources</param>
		/// <returns>An IEnumerable of Diagnostics that surfaced in the source code, sorted by Location</returns>
		private IEnumerable<Diagnostic> GetSortedDiagnostics(string[] sources, string filenamePrefix, string assemblyName,  DiagnosticAnalyzer analyzer)
		{
			var documents = GetDocuments(sources, filenamePrefix, assemblyName);
			return GetSortedDiagnosticsFromDocuments(analyzer, documents);
		}

		private sealed class TestAdditionalText : AdditionalText
		{
			private readonly SourceText _sourceText;

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

		private sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
		{
			private readonly ImmutableDictionary<string, string> _options;

			public TestAnalyzerConfigOptions(ImmutableDictionary<string, string> options)
			{
				_options = options;
			}

			public override bool TryGetValue(string key, [NotNullWhen(true)] out string value)
			{
				return _options.TryGetValue(key, out value);
			}
		}

		private sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
		{
			private readonly TestAnalyzerConfigOptions _configOptions;

			internal TestAnalyzerConfigOptionsProvider(ImmutableDictionary<string, string> options)
			{
				_configOptions = new TestAnalyzerConfigOptions(options);
			}

			public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
			{
				return _configOptions;
			}

			public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
			{
				return _configOptions;
			}
		}


        /// <summary>
        /// Given an analyzer and a document to apply it to, run the analyzer and gather an array of diagnostics found in it.
        /// The returned diagnostics are then ordered by location in the source document.
        /// </summary>
        /// <param name="analyzer">The analyzer to run on the documents</param>
        /// <param name="documents">The Documents that the analyzer will be run on</param>
        /// <returns>An IEnumerable of Diagnostics that surfaced in the source code, sorted by Location</returns>
        protected IEnumerable<Diagnostic> GetSortedDiagnosticsFromDocuments(DiagnosticAnalyzer analyzer, IEnumerable<Document> documents)
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

                var options = compilation.Options.WithSpecificDiagnosticOptions(specificOptions);
                var modified = compilation.WithOptions(options);

                List<AdditionalText> additionalTextsBuilder = new();
                foreach (TextDocument textDocument in project.AdditionalDocuments)
                {
                    SourceText contents = textDocument.GetTextAsync().Result;
                    additionalTextsBuilder.Add(new TestAdditionalText(textDocument.Name, contents));
                }

                var analyzerConfigOptions = GetAdditionalAnalyzerConfigOptions();
                var analyzerConfigOptionsProvider = new TestAnalyzerConfigOptionsProvider(analyzerConfigOptions);
                AnalyzerOptions analyzerOptions = new(ImmutableArray.ToImmutableArray(additionalTextsBuilder), analyzerConfigOptionsProvider);

                var compilationWithAnalyzers = modified.WithAnalyzers(ImmutableArray.Create(analyzer), options: analyzerOptions);

                var diags = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
                var ourDiagnostics = CollectOurDiagnostics(diags, documents);
                diagnostics.AddRange(ourDiagnostics);
            }

            var results = SortDiagnostics(diagnostics);
            diagnostics.Clear();
            return results;
        }

        private IReadOnlyList<Diagnostic> CollectOurDiagnostics(ImmutableArray<Diagnostic> diags, IEnumerable<Document> documents)
        {
            List<Diagnostic> diagnostics = new();
            foreach (var diag in diags)
            {
                if (diag.Location == Location.None || diag.Location.IsInMetadata)
                {
                    diagnostics.Add(diag);
                }
                else
                {
                    foreach (var document in documents)
                    {
                        var tree = document.GetSyntaxTreeAsync().Result;
                        if (tree == diag.Location.SourceTree)
                        {
                            diagnostics.Add(diag);
                        }
                    }
                }
            }
            return diagnostics;
        }

        /// <summary>
        /// Sort diagnostics by location in source document
        /// </summary>
        /// <param name="diagnostics">The list of Diagnostics to be sorted</param>
        /// <returns>An IEnumerable containing the Diagnostics in order of Location</returns>
        private static IEnumerable<Diagnostic> SortDiagnostics(IEnumerable<Diagnostic> diagnostics)
		{
			return diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
		}

		#endregion

		#region Set up compilation and documents
		/// <summary>
		/// Given an array of strings as sources and a language, turn them into a project and return the documents and spans of it.
		/// </summary>
		/// <param name="sources">Classes in the form of strings</param>
		/// <param name="filenamePrefix">The name of the source file, without the extension</param>
		/// <param name="assemblyName">The name of the resulting assembly of the compilation, without the extension</param>
		/// <returns>A Tuple containing the Documents produced from the sources and their TextSpans if relevant</returns>
		private IEnumerable<Document> GetDocuments(string[] sources, string filenamePrefix, string assemblyName)
		{
			var project = CreateProject(sources, filenamePrefix, assemblyName);
			var documents = project.Documents.ToArray();

			return documents;
		}

		/// <summary>
		/// Create a Document from a string through creating a project that contains it.
		/// </summary>
		/// <param name="source">Classes in the form of a string</param>
		/// <returns>A Document created from the source string</returns>
		protected Document CreateDocument(string source)
		{
			return CreateProject(new[] { source }).Documents.First();
		}

		protected virtual ImmutableArray<MetadataReference> GetMetadataReferences()
		{
			return ImmutableArray<MetadataReference>.Empty;
		}

		protected virtual ImmutableArray<(string name, string content)> GetAdditionalTexts()
		{
			return ImmutableArray<(string name, string content)>.Empty;
		}


		protected virtual ImmutableArray<DocumentInfo> GetAdditionalDocumentInfos(ProjectId projectId)
		{
			List<DocumentInfo> list = new();
			var details = GetAdditionalTexts();
			TestTextLoader textLoader = new();
			foreach ((string name, string content) in details)
			{
				var docId = DocumentId.CreateNewId(projectId);
				textLoader.Register(docId, content);
				var docInfo = DocumentInfo.Create(docId, name, null, SourceCodeKind.Regular, textLoader);
				list.Add(docInfo);
			}
			return ImmutableArray.Create(list.ToArray());
		}

		protected virtual ImmutableArray<(string name, string content)> GetAdditionalSourceCode()
		{
			return ImmutableArray<(string name, string content)>.Empty;
		}

		protected virtual ImmutableDictionary<string, string> GetAdditionalAnalyzerConfigOptions()
		{
			return ImmutableDictionary<string, string>.Empty;
		}

		/// <summary>
		/// Create a project using the inputted strings as sources.
		/// </summary>
		/// <param name="sources">Classes in the form of strings</param>
		/// <param name="filenamePrefix">The name of the source file, without the extension</param>
		/// <param name="assemblyName">The name of the resulting assembly of the compilation, without the extension</param>
		/// <returns>A Project created out of the Documents created from the source strings</returns>
		private Project CreateProject(string[] sources, string filenamePrefix = null, string assemblyName = null)
		{
			bool isCustomPrefix = filenamePrefix != null;
			filenamePrefix ??= DefaultFilePathPrefix;
			string fileExt = CSharpDefaultFileExt;
			string projectName = assemblyName ?? TestProjectName;

			var projectId = ProjectId.CreateNewId(debugName: TestProjectName);
			var documentInfos = GetAdditionalDocumentInfos(projectId);
			var adhocWorkspace = new AdhocWorkspace();
			var solution = adhocWorkspace
				.CurrentSolution
				.AddProject(projectId, TestProjectName, projectName, LanguageNames.CSharp)
				.AddAdditionalDocuments(documentInfos)
				.AddMetadataReference(projectId, CorlibReference)
				.AddMetadataReference(projectId, SystemCoreReference)
				.AddMetadataReference(projectId, CSharpSymbolsReference)
				.AddMetadataReference(projectId, CodeAnalysisReference)
				.AddMetadataReference(projectId, UnitTestingReference)
				.AddMetadataReference(projectId, GeneratedCodeReference)
				.AddMetadataReference(projectId, ThreadingReference);

			foreach (var testReferences in GetMetadataReferences())
			{
				solution = solution.AddMetadataReference(projectId, testReferences);
			}

			var trustedAssembliesPaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator);
			var neededAssemblies = new[]
			{
				"System.Runtime",
				"mscorlib",
			};
			foreach (var references in trustedAssembliesPaths.Where(p => neededAssemblies.Contains(Path.GetFileNameWithoutExtension(p)))
				.Select(p => MetadataReference.CreateFromFile(p)))
			{
				solution = solution.AddMetadataReference(projectId, references);
			}

			OptionSet newOptionSet = solution.Options.WithChangedOption(new OptionKey(FormattingOptions.IndentationSize, LanguageNames.CSharp), 2);
			Workspace workspace = solution.Workspace;
			var newSolution = workspace.CurrentSolution.WithOptions(newOptionSet);
			workspace.TryApplyChanges(newSolution);

			foreach (var m in solution.GetProject(projectId).MetadataReferences)
			{
				Trace.WriteLine($"{m.Display}: {m.Properties}");
			}

			int count = 0;
			var additionalSourceCode = GetAdditionalSourceCode();
			IEnumerable<(string name, string content)> data = sources.Select(x =>
			{
				var newFileName = string.Format("{0}{1}.{2}", filenamePrefix, count == 0 ? (isCustomPrefix ? string.Empty : count.ToString()) : count.ToString(), fileExt);

				count++;

				return (newFileName, x);

			}).Concat(additionalSourceCode);

			foreach ((string name, string content) in data)
			{
				var documentId = DocumentId.CreateNewId(projectId, debugName: name);
				solution = solution.AddDocument(documentId, name, SourceText.From(content));
			}

			return solution.GetProject(projectId);
		}
		#endregion
	}
}

