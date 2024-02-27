// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	/*
	 * Analyzer for hardcoded absolute path. 
	 * Reports diagnostics if an absolute path is used,
	 * For example: c:\users\Bin\example.xml
	 *          or: \\server\\share\example.xml
	 *
	 * This Analyzer only reports diagnostics on Windows.
	 */
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class NoHardCodedPathsAnalyzer : SingleDiagnosticAnalyzer
	{
		private const int MaxStringLength = 259;
		private const string Title = @"Avoid hardcoded absolute paths";
		private const string MessageFormat = Title;
		private const string Description = Title;
		private readonly Regex _windowsPattern = new(@"^(([a-zA-Z]:)|\\)\\{1,2}(((?![<>:/\\|?*]).)+((?<![ .])\\{1,2})?)*$", RegexOptions.Singleline | RegexOptions.Compiled, TimeSpan.FromSeconds(1));

		public NoHardCodedPathsAnalyzer()
			: base(DiagnosticId.NoHardcodedPaths, Title, MessageFormat, Description, Categories.Maintainability)
		{ }


		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			var stringLiteralExpressionNode = (LiteralExpressionSyntax)context.Node;

			if (Helper.ForTests.IsInTestClass(context))
			{
				return;
			}

			// Get the text value of the string literal expression.
			var pathValue = stringLiteralExpressionNode.Token.ValueText;

			if (pathValue.Length < 2)
			{
				return;
			}

			// If the character of the string do not match either of the characters : or \\ ; no need to run regex, simply return.
			if (!pathValue.Contains(":") && !pathValue.Contains("\\"))
			{
				return;
			}

			// Limit to first MAX_PATH characters, to prevent long analysis times.
			if (pathValue.Length > MaxStringLength)
			{
				pathValue = pathValue.Substring(0, MaxStringLength);
			}

			// If the pattern matches the text value, report the diagnostic.
			if (_windowsPattern.IsMatch(pathValue))
			{
				Location location = stringLiteralExpressionNode.GetLocation();
				var diagnostic = Diagnostic.Create(Rule, location);
				context.ReportDiagnostic(diagnostic);
			}
		}

		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.StringLiteralExpression);
		}
	}
}
