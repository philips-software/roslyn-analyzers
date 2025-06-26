// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation
{
	/// <summary>
	/// Analyzer that checks if the text of the XML code documentation contains more information compared to the obvious (name of Symbol and some low value words).
	/// Adding such low value comments doesn't add anything to the readability and just takes longer to read.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class XmlDocumentationShouldAddValueAnalyzer : DiagnosticAnalyzerBase
	{
		private const string EmptyTitle = @"Avoid empty Summary XML comments";
		private const string EmptyMessageFormat = @"Summary XML comments must be useful or non-existent.";
		private const string EmptyDescription = @"Summary XML comments for classes, methods, etc. must be useful or non-existent.";
		private const string ValueTitle = @"Documentation text should add value";
		private const string ValueMessageFormat = @"Summary XML comments must add more information then just repeating its name.";
		private const string ValueDescription = @"Summary XML comments for classes, methods, etc. must add more information then just repeating its name.";
		private const string Category = Categories.Documentation;

		private static readonly DiagnosticDescriptor ValueRule = new(DiagnosticId.XmlDocumentationShouldAddValue.ToId(), ValueTitle, ValueMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: ValueDescription);
		private static readonly DiagnosticDescriptor EmptyRule = new(DiagnosticId.EmptyXmlComments.ToId(), EmptyTitle, EmptyMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: EmptyDescription);

		private static readonly HashSet<string> UselessWords =
			[.. new[] { "get", StringConstants.Set, "the", "a", "an", "it", "i", "of", "to", "for", "on", "or", "and", StringConstants.Value, "indicate", "indicating", "instance", "raise", "raises", "fire", "event", "constructor", "ctor" }];
		private HashSet<string> additionalUselessWords;

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(EmptyRule, ValueRule);

		private static readonly char[] separator = [' '];

		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			var line = Helper.ForAdditionalFiles.GetValueFromEditorConfig(ValueRule.Id, @"additional_useless_words");
			additionalUselessWords = [.. SplitFromConfig(line)];
			context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeConstructor, SyntaxKind.ConstructorDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeField, SyntaxKind.FieldDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeEvent, SyntaxKind.EventFieldDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeEnum, SyntaxKind.EnumDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeEnumMember, SyntaxKind.EnumMemberDeclaration);
		}

		private void AnalyzeClass(SyntaxNodeAnalysisContext context)
		{
			var cls = context.Node as ClassDeclarationSyntax;
			AnalyzeNamedNode(context, () => cls?.Identifier.Text);
		}

		private void AnalyzeConstructor(SyntaxNodeAnalysisContext context)
		{
			var constructor = context.Node as ConstructorDeclarationSyntax;
			AnalyzeNamedNode(context, () => constructor?.Identifier.Text);
		}

		private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
		{
			var method = context.Node as MethodDeclarationSyntax;
			AnalyzeNamedNode(context, () => method?.Identifier.Text);
		}

		private void AnalyzeProperty(SyntaxNodeAnalysisContext context)
		{
			var prop = context.Node as PropertyDeclarationSyntax;
			AnalyzeNamedNode(context, () => prop?.Identifier.Text);
		}

		private void AnalyzeField(SyntaxNodeAnalysisContext context)
		{
			var field = context.Node as FieldDeclarationSyntax;
			AnalyzeNamedNode(context, () => field?.Declaration.Variables.FirstOrDefault()?.Identifier.Text);
		}

		private void AnalyzeEvent(SyntaxNodeAnalysisContext context)
		{
			var evt = context.Node as EventFieldDeclarationSyntax;
			AnalyzeNamedNode(context, () => evt?.Declaration.Variables.FirstOrDefault()?.Identifier.Text);
		}

		private void AnalyzeEnum(SyntaxNodeAnalysisContext context)
		{
			var cls = context.Node as EnumDeclarationSyntax;
			AnalyzeNamedNode(context, () => cls?.Identifier.Text);
		}

		private void AnalyzeEnumMember(SyntaxNodeAnalysisContext context)
		{
			var member = context.Node as EnumMemberDeclarationSyntax;
			AnalyzeNamedNode(context, () => member?.Identifier.Text);
		}

		private void AnalyzeNamedNode(SyntaxNodeAnalysisContext context, Func<string> nameFunc)
		{
			IEnumerable<XmlElementSyntax> xmlElements = context.Node.GetLeadingTrivia()
				.Select(i => i.GetStructure())
				.OfType<DocumentationCommentTriviaSyntax>()
				.SelectMany(n => n.ChildNodes().OfType<XmlElementSyntax>());
			if (xmlElements.Any())
			{
				var name = nameFunc();
				if (string.IsNullOrEmpty(name))
				{
					return;
				}

				var lowercaseName = name.ToLowerInvariant();

				foreach (XmlElementSyntax xmlElement in xmlElements)
				{
					if (xmlElement.StartTag.Name.LocalName.Text != @"summary")
					{
						continue;
					}

					var content = GetContent(xmlElement);

					if (string.IsNullOrWhiteSpace(content))
					{
						Location location = xmlElement.GetLocation();
						var diagnostic = Diagnostic.Create(EmptyRule, location);
						context.ReportDiagnostic(diagnostic);
						continue;
					}

					// Find the 'value' in the XML documentation content by:
					// 1. Splitting it into separate words.
					// 2. Filtering a predefined and a configurable list of words that add no value.
					// 3. Filter words that are part of the method name.
					// 4. Throw a Diagnostic if no words remain. This boils down to the content only containing 'low value' words.
					IEnumerable<string> words =
						SplitInWords(content)
							.Where(u => !additionalUselessWords.Contains(u) && !UselessWords.Contains(u))
							.Where(s => !lowercaseName.Contains(s));

					// We assume here that every remaining word adds value to the documentation text.
					if (!words.Any())
					{
						var loc = Location.Create(context.Node.SyntaxTree, xmlElement.Content.FullSpan);
						var diagnostic = Diagnostic.Create(ValueRule, loc);
						context.ReportDiagnostic(diagnostic);
					}
				}
			}
		}

		private static string GetContent(XmlElementSyntax xmlElement)
		{
			const string Space = " ";
			return xmlElement.Content.ToString()
				.Replace("\r", string.Empty)
				.Replace("/", Space)
				.Replace("\n", Space)
				.Replace("\t", Space)
				.Replace("///", string.Empty);
		}

		private static IEnumerable<string> SplitFromConfig(string line)
		{
			return line.Split(',').Select(w => w.TrimEnd('s').ToLowerInvariant());
		}

		private static IEnumerable<string> SplitInWords(string input)
		{
			var pruned = input.Replace(',', ' ').Replace('.', ' ').ToLowerInvariant();
			return pruned.Split(separator, StringSplitOptions.RemoveEmptyEntries).Select(w => w.TrimEnd('s'));
		}
	}
}
