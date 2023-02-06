// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidDuplicateStringsAnalyzer : SingleDiagnosticAnalyzer
	{
		private const string Title = @"Avoid Duplicate Strings";
		private const string MessageFormat = @"Duplicate string found, first location in file {0} at line {1}. Consider moving '{2}' into a constant.";
		private const string Description = @"Duplicate strings are less maintainable";

		public AvoidDuplicateStringsAnalyzer()
			: base(DiagnosticId.AvoidDuplicateStrings, Title, MessageFormat, Description, Categories.Maintainability)
		{ }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction(compilationContext =>
			{
				var compilationAnalyzer = new CompilationAnalyzer(Rule);
				compilationContext.RegisterSyntaxNodeAction(compilationAnalyzer.Analyze, SyntaxKind.ClassDeclaration);
				compilationContext.RegisterSyntaxNodeAction(compilationAnalyzer.Analyze, SyntaxKind.StructDeclaration);
			});

		}

		private sealed class CompilationAnalyzer
		{
			private readonly Dictionary<string, Location> _usedLiterals = new();
			private readonly DiagnosticDescriptor _rule;

			public CompilationAnalyzer(DiagnosticDescriptor rule)
			{
				_rule = rule;
			}

			public void Analyze(SyntaxNodeAnalysisContext context)
			{
				var typeDeclarationSyntax = (BaseTypeDeclarationSyntax)context.Node;

				GeneratedCodeDetector detector = new();
				if(detector.IsGeneratedCode(context))
				{
					return;
				}

				TestHelper testHelper = new();
				if(testHelper.IsInTestClass(context))
				{
					return;
				}

				foreach(var literal in typeDeclarationSyntax.DescendantTokens()
							 .Where(token => token.IsKind(SyntaxKind.StringLiteralToken)))
				{
					var literalText = literal.Text.Trim('\\', '\"');
					if(string.IsNullOrWhiteSpace(literalText) || literalText.Length <= 2)
					{
						continue;
					}
					var location = literal.GetLocation();
					if(_usedLiterals.TryGetValue(literalText, out Location firstLocation))
					{
						var firstFilename = Path.GetFileName(firstLocation.SourceTree.FilePath);
						var firstLineNumber = firstLocation.GetLineSpan().StartLinePosition.Line + 1;
						var diagnostic = Diagnostic.Create(_rule, location, firstFilename, firstLineNumber, literalText);
						context.ReportDiagnostic(diagnostic);
					}
					else
					{
						_usedLiterals.Add(literalText, location);
					}
				}
			}
		}
	}
}
