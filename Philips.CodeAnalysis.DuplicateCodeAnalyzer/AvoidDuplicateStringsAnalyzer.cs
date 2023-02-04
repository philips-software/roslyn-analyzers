// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.DuplicateCodeAnalyzer
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidDuplicateStringsAnalyzer : SingleDiagnosticAnalyzer
	{
		private const string Title = @"Avoid Duplicate Strings";
		private const string MessageFormat = @"Duplicate string found, first location at line {0}. Consider moving '{1}' into a constant.";
		private const string Description = @"Duplicate code is less maintainable";
		private const string Category = Categories.Maintainability;

		public AvoidDuplicateStringsAnalyzer() : base(DiagnosticId.AvoidDuplicateStrings, Title, MessageFormat,
			Description, Category)
		{
		}

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration);
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.StructDeclaration);
		}

		public void Analyze(SyntaxNodeAnalysisContext context)
		{
			var typeDeclarationSyntax = (BaseTypeDeclarationSyntax)context.Node;

			Dictionary<string, Location> usedLiterals = new();
			foreach (var literal in typeDeclarationSyntax.DescendantTokens()
				         .Where(token => token.IsKind(SyntaxKind.StringLiteralToken)))
			{
				var literalText = literal.Text;
				if (string.IsNullOrWhiteSpace(literalText))
				{
					continue;
				}
				var location = literal.GetLocation();
				if (usedLiterals.TryGetValue(literalText, out Location firstLocation))
				{
					var firstLineNumber = firstLocation.GetLineSpan().StartLinePosition.Line + 1;
					var diagnostic = Diagnostic.Create(Rule, location, firstLineNumber, literalText);
					context.ReportDiagnostic(diagnostic);
				}
				else
				{
					usedLiterals.Add(literalText, location);
				}
			}
		}
	}
}
