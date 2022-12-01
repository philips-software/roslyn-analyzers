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
	public class XmlDocumentationShouldAddValueAnalyzer : DiagnosticAnalyzer
	{
		private const string EmptyTitle = @"Summary XML comments";
		private const string EmptyMessageFormat = @"Summary XML comments must be useful or non-existent.";
		private const string EmptyDescription = @"Summary XML comments for classes, methods, etc. must be useful or non-existent.";
		private const string ValueTitle = @"Documentation text should add value";
		private const string ValueMessageFormat = @"Summary XML comments must add more information then just repeating its name.";
		private const string ValueDescription = @"Summary XML comments for classes, methods, etc. must add more information then just repeating its name.";
		private const string Category = Categories.Documentation;

		private static readonly DiagnosticDescriptor ValueRule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.XmlDocumentationShouldAddValue), ValueTitle, ValueMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: false, description: ValueDescription);
		private static readonly DiagnosticDescriptor EmptyRule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.EmptyXmlComments), EmptyTitle, EmptyMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: false, description: EmptyDescription);

		private static readonly HashSet<string> UselessWords = 
			new HashSet<string>( new[]{ "get", "set", "the", "a", "an", "it", "i", "of", "to", "for", "on", "or", "and", "value", "indicate", "indicating", "instance", "raise", "raises", "fire", "event", "constructor", "ctor" });
		private HashSet<string> additionalUselessWords;

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(EmptyRule, ValueRule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction(ctx =>
			{
				var additionalFilesHelper = new AdditionalFilesHelper(ctx.Options, ctx.Compilation);
				var line = additionalFilesHelper.GetValueFromEditorConfig(ValueRule.Id, @"additional_useless_words");
				additionalUselessWords = new HashSet<string>(SplitFromConfig(line));
				ctx.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
				ctx.RegisterSyntaxNodeAction(AnalyzeConstructor, SyntaxKind.ConstructorDeclaration);
				ctx.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
				ctx.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
				ctx.RegisterSyntaxNodeAction(AnalyzeField, SyntaxKind.FieldDeclaration);
				ctx.RegisterSyntaxNodeAction(AnalyzeEvent, SyntaxKind.EventFieldDeclaration);
				ctx.RegisterSyntaxNodeAction(AnalyzeEnum, SyntaxKind.EnumDeclaration); 
				ctx.RegisterSyntaxNodeAction(AnalyzeEnumMember, SyntaxKind.EnumMemberDeclaration);
			});
		}

		private void AnalyzeClass(SyntaxNodeAnalysisContext context)
		{
			ClassDeclarationSyntax cls = context.Node as ClassDeclarationSyntax;
			string name = cls?.Identifier.Text;
			AnalyzeNamedNode(context, name);
		}

		private void AnalyzeConstructor(SyntaxNodeAnalysisContext context)
		{
			ConstructorDeclarationSyntax constructor = context.Node as ConstructorDeclarationSyntax;
			string name = constructor?.Identifier.Text;
			AnalyzeNamedNode(context, name);
		}

		private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
		{
			MethodDeclarationSyntax method = context.Node as MethodDeclarationSyntax;
			string name = method?.Identifier.Text;
			AnalyzeNamedNode(context, name);
		}

		private void AnalyzeProperty(SyntaxNodeAnalysisContext context)
		{
			PropertyDeclarationSyntax prop = context.Node as PropertyDeclarationSyntax;
			string name = prop?.Identifier.Text;
			AnalyzeNamedNode(context, name);
		}

		private void AnalyzeField(SyntaxNodeAnalysisContext context)
		{
			FieldDeclarationSyntax field = context.Node as FieldDeclarationSyntax;
			string name = field?.Declaration.Variables.FirstOrDefault()?.Identifier.Text;
			AnalyzeNamedNode(context, name);
		}

		private void AnalyzeEvent(SyntaxNodeAnalysisContext context)
		{
			EventFieldDeclarationSyntax evt = context.Node as EventFieldDeclarationSyntax;
			string name = evt?.Declaration.Variables.FirstOrDefault()?.Identifier.Text;
			AnalyzeNamedNode(context, name);
		}

		private void AnalyzeEnum(SyntaxNodeAnalysisContext context)
		{
			EnumDeclarationSyntax cls = context.Node as EnumDeclarationSyntax;
			string name = cls?.Identifier.Text;
			AnalyzeNamedNode(context, name);
		}

		private void AnalyzeEnumMember(SyntaxNodeAnalysisContext context)
		{
			EnumMemberDeclarationSyntax member = context.Node as EnumMemberDeclarationSyntax;
			string name = member?.Identifier.Text;
			AnalyzeNamedNode(context, name);
		}
		
		private void AnalyzeNamedNode(SyntaxNodeAnalysisContext context, string name)
		{
			if (string.IsNullOrEmpty(name))
				return;

			name = name.ToLowerInvariant();
			var xmlElements = context.Node.GetLeadingTrivia()
				.Select(i => i.GetStructure())
				.OfType<DocumentationCommentTriviaSyntax>()
				.SelectMany(n => n.ChildNodes().OfType<XmlElementSyntax>());
			foreach(var xmlElement in xmlElements)
			{
				if (xmlElement.StartTag.Name.LocalName.Text != @"summary")
					continue;

				string content = GetContent(xmlElement);

				if (string.IsNullOrWhiteSpace(content))
				{
					Diagnostic diagnostic = Diagnostic.Create(EmptyRule, xmlElement.GetLocation());
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
						.Where(s => !name.Contains(s));

				// We assume here that every remaining word adds value to the documentation text.
				if (!words.Any())
				{
					Diagnostic diagnostic = Diagnostic.Create(ValueRule, xmlElement.GetLocation());
					context.ReportDiagnostic(diagnostic);
				}
			}
		}

		private static string GetContent(XmlElementSyntax xmlElement)
		{
			return xmlElement.Content.ToString()
				.Replace("\r", "")
				.Replace("/", " ")
				.Replace("\n", " ")
				.Replace("\t", " ")
				.Replace("///", "");
		}

		private static IEnumerable<string> SplitFromConfig(string line)
		{
			return line.Split(',').Select(w => w.TrimEnd('s').ToLowerInvariant());
		}

		private static IEnumerable<string> SplitInWords(string input)
		{
			var pruned = input.Replace(',', ' ').Replace('.', ' ').ToLowerInvariant();
			return pruned.Split(new [] {' '}, StringSplitOptions.RemoveEmptyEntries).Select(w => w.TrimEnd('s'));
		}
	}
}
