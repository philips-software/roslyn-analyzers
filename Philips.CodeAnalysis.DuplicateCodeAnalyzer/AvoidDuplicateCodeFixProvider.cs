// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;


namespace Philips.CodeAnalysis.DuplicateCodeAnalyzer
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidDuplicateCodeFixProvider)), Shared]
	public class AvoidDuplicateCodeFixProvider : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticId.AvoidDuplicateCode)); }
		}

		public sealed override FixAllProvider GetFixAllProvider()
		{
			return new AvoidDuplicateCodeFixAllProvider();
		}

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			Project project = context.Document.Project;
			TextDocument exceptionsDocument = project.AdditionalDocuments.FirstOrDefault(doc => doc.Name.Equals(AvoidDuplicateCodeAnalyzer.AllowedFileName, StringComparison.Ordinal));
			if (exceptionsDocument == null)
			{
				return;
			}

			await ProcessGuiltyMethods(context.Document, context.Diagnostics, (name, registeredName, diagnostic) =>
			{
				string title = $@"Exempt {name} as duplicate";
				context.RegisterCodeFix(
					CodeAction.Create(
						title: title,
						createChangedSolution: c => GetFix(exceptionsDocument, registeredName, c),
						equivalenceKey: title),
					diagnostic);
			}, context.CancellationToken).ConfigureAwait(false);
		}

		private async Task<Solution> GetFix(TextDocument document, string registeredName, CancellationToken cancellationToken)
		{
			SourceText sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
			SourceText newSourceText = MakeNewSourceText(sourceText, registeredName);
			return document.Project.Solution.WithAdditionalDocumentText(document.Id, newSourceText);
		}

		public static SourceText MakeNewSourceText(SourceText original, string appending)
		{
			string newText = appending;

			// Add a Newline if necessary
			int rangeStart = original.Length;
			if (!original.ToString().EndsWith(Environment.NewLine))
			{
				newText = Environment.NewLine + newText;
			}

			var change = new TextChange(new TextSpan(rangeStart, 0), newText);
			return original.WithChanges(change);
		}

		public static async Task ProcessGuiltyMethods(Document document, ImmutableArray<Diagnostic> diagnostics, Action<string, string, Diagnostic> action, CancellationToken cancellationToken)
		{
			SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			if (root == null)
			{
				return;
			}

			SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
			foreach (Diagnostic diagnostic in diagnostics)
			{
				TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;
				SyntaxNode syntaxNode = root.FindToken(diagnosticSpan.Start).Parent;
				if (syntaxNode != null)
				{
					MethodDeclarationSyntax methodDeclarationSyntax = syntaxNode.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
					if (methodDeclarationSyntax != null)
					{
						string methodName = methodDeclarationSyntax.Identifier.ValueText;
						string registeredName = methodName;
						var symbol = semanticModel.GetDeclaredSymbol(methodDeclarationSyntax, cancellationToken);
						if (symbol is IMethodSymbol methodSymbol && methodSymbol.ContainingNamespace != null && methodSymbol.ContainingType != null)
						{
							registeredName = '~' + methodSymbol.GetDocumentationCommentId();
						}
						action(methodName, registeredName, diagnostic);
					}
				}
			}
		}
	}

	public class AvoidDuplicateCodeFixAllProvider : FixAllProvider
	{
		public override async Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
		{
			var diagnosticsToFix = new List<KeyValuePair<Project, ImmutableArray<Diagnostic>>>();
			string titleFormat = "Add all duplications in {0} {1} to the exceptions list";
			string title = null;
			switch (fixAllContext.Scope)
			{
				case FixAllScope.Document:
					{
						ImmutableArray<Diagnostic> diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(fixAllContext.Document).ConfigureAwait(false);
						diagnosticsToFix.Add(new KeyValuePair<Project, ImmutableArray<Diagnostic>>(fixAllContext.Project, diagnostics));
						title = string.Format(titleFormat, "document", fixAllContext.Document.Name);
						break;
					}
				case FixAllScope.Project:
					{
						Project project = fixAllContext.Project;
						ImmutableArray<Diagnostic> diagnostics = await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
						diagnosticsToFix.Add(new KeyValuePair<Project, ImmutableArray<Diagnostic>>(fixAllContext.Project, diagnostics));
						title = string.Format(titleFormat, "project", fixAllContext.Project.Name);
						break;
					}
				case FixAllScope.Solution:
					{
						foreach (Project project in fixAllContext.Solution.Projects)
						{
							ImmutableArray<Diagnostic> diagnostics = await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
							diagnosticsToFix.Add(new KeyValuePair<Project, ImmutableArray<Diagnostic>>(project, diagnostics));
						}
						title = "Add all items in the solution to the exceptions list";
						break;
					}
				default:
					break;
			}
			return new FixAllAdditionalDocumentChangeAction(title, fixAllContext.Solution, diagnosticsToFix);

		}
	}

	public class FixAllAdditionalDocumentChangeAction : CodeAction
	{
		private readonly List<KeyValuePair<Project, ImmutableArray<Diagnostic>>> _diagnosticsToFix;
		private readonly Solution _solution;

		public FixAllAdditionalDocumentChangeAction(string title, Solution solution, List<KeyValuePair<Project, ImmutableArray<Diagnostic>>> diagnosticsToFix)
		{
			Title = title;
			_solution = solution;
			_diagnosticsToFix = diagnosticsToFix;
		}

		public override string Title { get; }

		protected override async Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
		{
			var duplicateExceptionsList = new List<KeyValuePair<DocumentId, SourceText>>();
			foreach (KeyValuePair<Project, ImmutableArray<Diagnostic>> pair in _diagnosticsToFix)
			{
				Project project = pair.Key;
				ImmutableArray<Diagnostic> diagnostics = pair.Value;
				TextDocument duplicateExceptionsDocument = project.AdditionalDocuments.FirstOrDefault(doc => doc.Name.Equals(AvoidDuplicateCodeAnalyzer.AllowedFileName, StringComparison.Ordinal));
				if (duplicateExceptionsDocument == null)
				{
					continue;
				}

				SourceText sourceText = await duplicateExceptionsDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

				IEnumerable<IGrouping<SyntaxTree, Diagnostic>> groupedDiagnostics =
					diagnostics
						.Where(d => d.Location.IsInSource)
						.GroupBy(d => d.Location.SourceTree);

				var newMethodNames = new List<string>();
				foreach (IGrouping<SyntaxTree, Diagnostic> grouping in groupedDiagnostics)
				{
					Document document = project.GetDocument(grouping.Key);
					if (document == null)
					{
						continue;
					}
					var newMethods = new List<string>();
					var groupDiagnostics = grouping.ToImmutableArray();
					await AvoidDuplicateCodeFixProvider.ProcessGuiltyMethods(document, groupDiagnostics, (_, registeredName, _) => { newMethods.Add(registeredName); }, cancellationToken);
					newMethodNames.AddRange(newMethods);
				}

				StringBuilder appending = new();
				foreach (string methodName in newMethodNames)
				{
					_ = appending.Append(methodName);
					_ = appending.Append(Environment.NewLine);
				}

				SourceText newSourceText = AvoidDuplicateCodeFixProvider.MakeNewSourceText(sourceText, appending.ToString());
				duplicateExceptionsList.Add(new KeyValuePair<DocumentId, SourceText>(duplicateExceptionsDocument.Id, newSourceText));
			}

			Solution newSolution = _solution;
			foreach (KeyValuePair<DocumentId, SourceText> pair in duplicateExceptionsList)
			{
				newSolution = newSolution.WithAdditionalDocumentText(pair.Key, pair.Value);
			}
			return newSolution;
		}
	}
}
