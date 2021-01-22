using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers
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
		private const string Title = @"Absolute paths must not be hardcoded";
		private const string MessageFormat = @"Absolute paths must not be hardcoded";
		private const string Description = @"Absolute paths must not be hardcoded";
		private const string Category = Categories.Maintainability;
		private Regex WindowsPattern = new Regex(@"^[a-zA-Z]:\\{1,2}(((?![<>:/\\|?*]).)+((?<![ .])\\{1,2})?)*$");
		private Regex LinuxPattern = new Regex(@"^\/$|(^(?=\/)|^\.|^\.\.)(\/(?=[^/\0])[^/\0]+)*\/?$");

		#endregion

		#region Non-Public Data Members
		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			LiteralExpressionSyntax stringLiteralExpressionNode = (LiteralExpressionSyntax)context.Node;
			// Get the text value of the string literal expression.
			string pathValue = stringLiteralExpressionNode.Token.ValueText;

			//if the character of the string do not match either of the characters : for windows and / for linux; no need to run regex, simply return.
			if (!pathValue[1].Equals(':') && !pathValue[0].Equals('/')) return;

			// If the pattern matches the text value, report the diagnostic.
			if (WindowsPattern.IsMatch(pathValue) || LinuxPattern.IsMatch(pathValue))
			{
				Diagnostic diagnostic = Diagnostic.Create(Rule, stringLiteralExpressionNode.GetLocation());
				context.ReportDiagnostic(diagnostic);
			}
		}
		#endregion

		#region Public Interface
		public static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.NoHardcodedPaths), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);
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
