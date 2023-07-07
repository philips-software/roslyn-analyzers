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
	 * For example: c:\users\Bin\example.xml - Windows & /home/kt/abc.sql - Linux
	 */
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class NoHardCodedPathsAnalyzer : SingleDiagnosticAnalyzer
	{
		private const string Title = @"Avoid hardcoded absolute paths";
		private const string MessageFormat = Title;
		private const string Description = Title;
		private readonly Regex WindowsPattern = new(@"^[a-zA-Z]:\\{1,2}(((?![<>:/\\|?*]).)+((?<![ .])\\{1,2})?)*$", RegexOptions.Singleline | RegexOptions.Compiled, TimeSpan.FromSeconds(1));
		private readonly TestHelper _helper = new();

		public NoHardCodedPathsAnalyzer()
			: base(DiagnosticId.NoHardcodedPaths, Title, MessageFormat, Description, Categories.Maintainability)
		{ }


		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			var stringLiteralExpressionNode = (LiteralExpressionSyntax)context.Node;

			if (_helper.IsInTestClass(context))
			{
				return;
			}

			// Get the text value of the string literal expression.
			var pathValue = stringLiteralExpressionNode.Token.ValueText;

			if (pathValue.Length < 2)
			{
				return;
			}

			//if the character of the string do not match either of the characters : for windows and / for linux; no need to run regex, simply return.
			if (!pathValue[1].Equals(':') && !pathValue[0].Equals('/'))
			{
				return;
			}

			// If the pattern matches the text value, report the diagnostic.
			if (WindowsPattern.IsMatch(pathValue))
			{
				Location location = stringLiteralExpressionNode.GetLocation();
				var diagnostic = Diagnostic.Create(Rule, location);
				context.ReportDiagnostic(diagnostic);
			}
		}

		protected override void InitializeAnalysis(CompilationStartAnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.StringLiteralExpression);
		}
	}
}
