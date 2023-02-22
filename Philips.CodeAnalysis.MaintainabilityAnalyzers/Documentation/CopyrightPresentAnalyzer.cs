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
	public class CopyrightPresentAnalyzer : SingleDiagnosticAnalyzer<CompilationUnitSyntax, CopyrightPresentSyntaxNodeAction>
	{
		public CopyrightPresentAnalyzer()
			: base(DiagnosticId.CopyrightPresent,
					@"Copyright Present",
					@"File should start with a copyright statement, containing the company name, the year and either © or 'Copyright'.",
					@"File should start with a comment containing the company name, the year and either © or 'Copyright'.",
					Categories.Documentation)
		{ }
	}

	public class CopyrightPresentSyntaxNodeAction : SyntaxNodeAction<CompilationUnitSyntax>
	{
		private static readonly Regex yearRegex = new(@"\d\d\d\d", RegexOptions.Singleline | RegexOptions.Compiled, TimeSpan.FromSeconds(1));

		public override void Analyze()
		{
			if (Helper.IsAssemblyInfo(Context) || Helper.HasAutoGeneratedComment(Node))
			{
				return;
			}

			if (Node.FindToken(0).IsKind(SyntaxKind.EndOfFileToken))
			{
				return;
			}

			Location location = GetSquiggleLocation(Node.SyntaxTree);
			SyntaxNodeOrToken nodeOrToken = FindFirstWithLeadingTrivia(Node);
			SyntaxTriviaList leadingTrivia = nodeOrToken.GetLeadingTrivia();

			if (!leadingTrivia.Any(SyntaxKind.SingleLineCommentTrivia) && !leadingTrivia.Any(SyntaxKind.RegionDirectiveTrivia))
			{
				ReportDiagnostic(location);
				return;
			}

			// Special case: there's a #region, and the Copyright is in the name of the region
			if (leadingTrivia[0].IsKind(SyntaxKind.RegionDirectiveTrivia) && CheckCopyrightStatement(leadingTrivia[0]))
			{
				return;
			}

			SyntaxTrivia syntaxTrivia = leadingTrivia.FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia));
			if (!CheckCopyrightStatement(syntaxTrivia))
			{
				ReportDiagnostic(location);
			}
		}

		private Location GetSquiggleLocation(SyntaxTree tree)
		{
			TextSpan span = tree.GetText().Lines[0].Span;
			var location = Location.Create(tree, span);
			return location;
		}

		private SyntaxNodeOrToken FindFirstWithLeadingTrivia(SyntaxNode root)
		{
			return root.DescendantNodesAndTokensAndSelf().FirstOrDefault(n =>
			{
				SyntaxTriviaList trivia = n.GetLeadingTrivia();
				return trivia.Any(SyntaxKind.SingleLineCommentTrivia) || trivia.Any(SyntaxKind.RegionDirectiveTrivia);
			});
		}

		private bool CheckCopyrightStatement(SyntaxTrivia trivia)
		{
			var comment = trivia.ToFullString();
			// Check the copyright mark itself
			var hasCopyright = comment.Contains('©') || comment.Contains("\uFFFD") || comment.Contains("Copyright");

			// Check the year
			var hasYear = yearRegex.IsMatch(comment);

			// Check the company name, only if it is configured.
			var additionalFilesHelper = new AdditionalFilesHelper(Context.Options, Context.Compilation);
			var companyName = additionalFilesHelper.GetValueFromEditorConfig(Rule.Id, @"company_name");
			var hasCompanyName = string.IsNullOrEmpty(companyName) || comment.Contains(companyName);

			return hasCopyright && hasYear && hasCompanyName;
		}
	}
}
