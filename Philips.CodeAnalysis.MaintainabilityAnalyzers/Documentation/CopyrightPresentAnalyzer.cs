﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
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
	public class CopyrightPresentAnalyzer : SingleDiagnosticAnalyzer<CompilationUnitSyntax>
	{
		private static readonly Regex yearRegex = new(@"\d\d\d\d", RegexOptions.Singleline | RegexOptions.Compiled, TimeSpan.FromSeconds(1));

		public CopyrightPresentAnalyzer()
			: this(new Helper())
		{ }

		public CopyrightPresentAnalyzer(Helper helper)
			:base(DiagnosticId.CopyrightPresent,
					@"Copyright Present",
					@"File should start with a copyright statement, containing the company name, the year and either © or 'Copyright'.",
					@"File should start with a comment containing the company name, the year and either © or 'Copyright'.",
					Categories.Documentation, helper)
		{ }

		protected override void Analyze(SyntaxNodeAnalysisContext context, CompilationUnitSyntax node)
		{
			if (Helper.IsAssemblyInfo(context) || Helper.HasAutoGeneratedComment(node))
			{
				return;
			}

			if (node.FindToken(0).IsKind(SyntaxKind.EndOfFileToken))
			{
				return;
			}

			var location = GetSquiggleLocation(node.SyntaxTree);
			var nodeOrToken = FindFirstWithLeadingTrivia(node);
			var leadingTrivia = nodeOrToken.GetLeadingTrivia();
			
			if (!leadingTrivia.Any(SyntaxKind.SingleLineCommentTrivia) && !leadingTrivia.Any(SyntaxKind.RegionDirectiveTrivia))
			{
				ReportDiagnostic(context, location);
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
				ReportDiagnostic(context, location);
			}
		}

		private Location GetSquiggleLocation(SyntaxTree tree)
		{
			TextSpan span = tree.GetText().Lines[0].Span;
			var location = Location.Create(tree, span);
			return location;
		}

		private static SyntaxNodeOrToken FindFirstWithLeadingTrivia(SyntaxNode root)
		{
			return root.DescendantNodesAndTokensAndSelf().FirstOrDefault(n =>
			{
				var trivia = n.GetLeadingTrivia();
				return trivia.Any(SyntaxKind.SingleLineCommentTrivia) || trivia.Any(SyntaxKind.RegionDirectiveTrivia);
			});
		}

		private bool CheckCopyrightStatement(SyntaxNodeAnalysisContext context, SyntaxTrivia trivia) {
			var comment = trivia.ToFullString();
			// Check the copyright mark itself
			bool hasCopyright = comment.Contains('©') || comment.Contains("\uFFFD") || comment.Contains("Copyright");
			
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
