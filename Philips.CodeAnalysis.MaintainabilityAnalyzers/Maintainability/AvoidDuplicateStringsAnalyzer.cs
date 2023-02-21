// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Concurrent;
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
	public class AvoidDuplicateStringsAnalyzer : SingleDiagnosticAnalyzer<LiteralExpressionSyntax, AvoidDuplicateStringsSyntaxNodeAction>
	{
		private const string Title = @"Avoid Duplicate Strings";
		private const string MessageFormat = @"Duplicate string found, first location in file {0} at line {1}. Consider moving '{2}' into a constant.";
		private const string Description = @"Duplicate strings are less maintainable";

		public AvoidDuplicateStringsAnalyzer()
			: base(DiagnosticId.AvoidDuplicateStrings, Title, MessageFormat, Description, Categories.Maintainability)
		{ }

		public ConcurrentDictionary<string, Location> UsedLiterals { get; } = new();

		protected override SyntaxKind GetSyntaxKind()
		{
			return SyntaxKind.StringLiteralExpression;
		}
	}

	public class AvoidDuplicateStringsSyntaxNodeAction : SyntaxNodeAction<LiteralExpressionSyntax>
	{
		public override void Analyze()
		{
			if (Node.Ancestors().OfType<FieldDeclarationSyntax>().Any())
			{
				return;
			}

			TestHelper testHelper = new();
			if (testHelper.IsInTestClass(Context))
			{
				return;
			}

			SyntaxToken literal = Node.Token;
			string literalText = literal.Text.Trim('\\', '\"');
			if (string.IsNullOrWhiteSpace(literalText) || literalText.Length <= 2)
			{
				return;
			}

			Location location = literal.GetLocation();
			ConcurrentDictionary<string, Location> usedLiterals = ((AvoidDuplicateStringsAnalyzer)Analyzer).UsedLiterals;

			if (!usedLiterals.TryAdd(literalText, location))
			{
				_ = usedLiterals.TryGetValue(literalText, out Location firstLocation);
				string firstFilename = Path.GetFileName(firstLocation.SourceTree.FilePath);
				int firstLineNumber = firstLocation.GetLineSpan().StartLinePosition.Line + 1;
				ReportDiagnostic(location, firstFilename, firstLineNumber, literalText);
			}
		}
	}
}
