using System.Collections.Immutable;
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
	public class NoHardCodedPathsAnalyzer : DiagnosticAnalyzer
	{

		#region Non-Public Data Members
		private const string Title = @"Avoid hardcoded absolute paths";
		private const string MessageFormat = @"Avoid hardcoded absolute paths";
		private const string Description = @"Avoid hardcoded absolute paths";
		private const string Category = Categories.Maintainability;
		private readonly Regex WindowsPattern = new(@"^[a-zA-Z]:\\{1,2}(((?![<>:/\\|?*]).)+((?<![ .])\\{1,2})?)*$");

		#endregion

		#region Non-Public Data Members
		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			LiteralExpressionSyntax stringLiteralExpressionNode = (LiteralExpressionSyntax)context.Node;
			// Get the text value of the string literal expression.
			string pathValue = stringLiteralExpressionNode.Token.ValueText;

			if (pathValue.Length < 2) return;

			//if the character of the string do not match either of the characters : for windows and / for linux; no need to run regex, simply return.
			if (!pathValue[1].Equals(':') && !pathValue[0].Equals('/')) return;

			// If the pattern matches the text value, report the diagnostic.
			if (WindowsPattern.IsMatch(pathValue))
			{
				Diagnostic diagnostic = Diagnostic.Create(Rule, stringLiteralExpressionNode.GetLocation());
				context.ReportDiagnostic(diagnostic);
			}
		}
		#endregion

		#region Public Interface
		public readonly static DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.NoHardcodedPaths), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
		{
			get { return ImmutableArray.Create(Rule); }
		}
		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.StringLiteralExpression);
		}
		#endregion

	}
}
