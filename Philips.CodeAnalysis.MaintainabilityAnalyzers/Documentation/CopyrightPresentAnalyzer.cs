﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class CopyrightPresentAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Copyright Present";
		private const string MessageFormat = 
			@"File should start with a copyright statement, containing the company name, the year and either © or 'Copyright'.";
		private const string Description =
			@"File should start with a comment containing the company name, the year and either © or 'Copyright'.";
		private const string Category = Categories.Documentation;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.CopyrightPresent), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);
		private static readonly Regex yearRegex = new(@"\d\d\d\d");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.CompilationUnit);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			CompilationUnitSyntax node = (CompilationUnitSyntax)context.Node;

			GeneratedCodeDetector generatedCodeDetector = new();
			if (generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			if (Helper.IsAssemblyInfo(context) || Helper.HasAutoGeneratedComment(node))
			{
				return;
			}

			if (node.FindToken(0).IsKind(SyntaxKind.EndOfFileToken))
			{
				return;
			}

			var location = GetSquiggleLocation(node.SyntaxTree);
			var leadingTrivia = node.GetLeadingTrivia();

			if (!leadingTrivia.Any(SyntaxKind.SingleLineCommentTrivia) && !leadingTrivia.Any(SyntaxKind.RegionDirectiveTrivia))
			{
				CreateDiagnostic(context, location);
				return;
			}

			// Special case: there's a #region, and the Copyright is in the name of the region
			if (leadingTrivia[0].IsKind(SyntaxKind.RegionDirectiveTrivia) && CheckCopyrightStatement(context, leadingTrivia[0]))
			{
				return;
			}

			SyntaxTrivia syntaxTrivia = leadingTrivia.FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia));
			if (!CheckCopyrightStatement(context, syntaxTrivia))
			{
				CreateDiagnostic(context, location);
			}
		}

		private Location GetSquiggleLocation(SyntaxTree tree)
		{
			TextSpan span = tree.GetText().Lines[0].Span;
			var location = Location.Create(tree, span);
			return location;
		}
			

		private void CreateDiagnostic(SyntaxNodeAnalysisContext context, Location location)
		{
			Diagnostic diagnostic = Diagnostic.Create(Rule, location);
			context.ReportDiagnostic(diagnostic);
		}

		private bool CheckCopyrightStatement(SyntaxNodeAnalysisContext context, SyntaxTrivia trivia) {
			var comment = trivia.ToFullString();
			// Check the copyright mark itself
			bool hasCopyright = comment.Contains("©") || comment.Contains("Copyright");
			
			// Check the year
			bool hasYear = yearRegex.IsMatch(comment);

			// Check the company name, only if it is configured.
			var additionalFilesHelper = new AdditionalFilesHelper(context.Options, context.Compilation);
			var companyName = additionalFilesHelper.GetValueFromEditorConfig(Rule.Id, @"company_name");
			var hasCompanyName = string.IsNullOrEmpty(companyName) || comment.Contains(companyName);

			return hasCopyright && hasYear && hasCompanyName;
		}
	}
}
