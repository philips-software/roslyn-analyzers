// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;


namespace Philips.CodeAnalysis.DuplicateCodeAnalyzer
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidDuplicateCodeAnalyzer)), Shared]
	public class AvoidDuplicateCodeFixProvider : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(Helper.ToDiagnosticId(DiagnosticIds.AvoidDuplicateCode)); }
		}

		public sealed override FixAllProvider GetFixAllProvider()
		{
			return new AvoidDuplicateCodeFixAllProvider();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			Project project = context.Document.Project;

			TextDocument exceptionsDocument = project.AdditionalDocuments.FirstOrDefault(doc => doc.Name.Equals(AvoidDuplicateCodeAnalyzer.AllowedFileName, StringComparison.Ordinal));

			if (exceptionsDocument == null)
			{
				return;
			}

			SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			foreach (Diagnostic diagnostic in context.Diagnostics)
			{
				TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

				if (root != null)
				{
					SyntaxNode syntaxNode = root.FindToken(diagnosticSpan.Start).Parent;
					if (syntaxNode != null)
					{
						MethodDeclarationSyntax methodDeclarationSyntax =
							syntaxNode.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
						if (methodDeclarationSyntax != null)
						{
							string methodName = methodDeclarationSyntax.Identifier.ValueText;

							string title = $@"Add {methodName} to duplicate code exceptions list";
							context.RegisterCodeFix(
								CodeAction.Create(
									title: title,
									createChangedSolution: c => GetFix(exceptionsDocument, methodName, c),
									equivalenceKey: title),
								diagnostic);
						}
					}
				}
			}
		}

		private async Task<Solution> GetFix(TextDocument document, string methodName, CancellationToken cancellationToken)
		{
			SourceText sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
			var change = new TextChange(new TextSpan(sourceText.Length, 0), Environment.NewLine + methodName);
			SourceText newSourceText = sourceText.WithChanges(change);
			return document.Project.Solution.WithAdditionalDocumentText(document.Id, newSourceText);
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

					SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
					List<string> newMethods = GetMethodNames(grouping, root);
					newMethodNames.AddRange(newMethods);
				}

				StringBuilder appending = new();
				foreach (string methodName in newMethodNames)
				{
					appending.Append(Environment.NewLine);
					appending.Append(methodName);
				}

				// Strip last NewLine if it's there
				int rangeStart = sourceText.Length;
				if (sourceText.ToString().EndsWith(Environment.NewLine))
				{
					rangeStart -= Environment.NewLine.Length;
				}

				var change = new TextChange(new TextSpan(rangeStart, 0), appending.ToString());
				SourceText newSourceText = sourceText.WithChanges(change);

				duplicateExceptionsList.Add(new KeyValuePair<DocumentId, SourceText>(duplicateExceptionsDocument.Id, newSourceText));
			}

			Solution newSolution = _solution;
			foreach (KeyValuePair<DocumentId, SourceText> pair in duplicateExceptionsList)
			{
				newSolution = newSolution.WithAdditionalDocumentText(pair.Key, pair.Value);
			}
			return newSolution;
		}

		private List<string> GetMethodNames(IGrouping<SyntaxTree, Diagnostic> grouping, SyntaxNode root)
		{
			var newMethodNames = new List<string>();
			foreach (Diagnostic diagnostic in grouping)
			{
				TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;
				if (root != null)
				{
					SyntaxNode node = root.FindToken(diagnosticSpan.Start).Parent;
					if (node != null)
					{
						MethodDeclarationSyntax methodDeclarationSyntax = node.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
						if (methodDeclarationSyntax != null)
						{
							string methodName = methodDeclarationSyntax.Identifier.ValueText;
							newMethodNames.Add(methodName);
						}
					}
				}
			}
			return newMethodNames;
		}
	}
}
