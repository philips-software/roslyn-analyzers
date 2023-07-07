// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.


using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AssertIsTrueLiteralAnalyzer : AssertIsTrueFalseDiagnosticAnalyzer
	{
		private const string IsTrueTitle = @"Assert.IsTrue(true) Usage";
		private const string IsTrueMessageFormat = @"Do not call IsTrue/IsFalse with a literal true/false";
		private const string IsTrueDescription = IsTrueMessageFormat;

		private const string Category = Categories.MsTest;

		private static readonly DiagnosticDescriptor IsTrueRule = new(Helper.ToDiagnosticId(DiagnosticId.AssertIsTrueLiteral), IsTrueTitle, IsTrueMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: IsTrueDescription);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(IsTrueRule); } }

		protected override Diagnostic Check(SyntaxNodeAnalysisContext context, SyntaxNode node, ExpressionSyntax test, bool isIsTrue)
		{
			Location location = test.GetLocation();
			return !Helper.ForLiterals.IsLiteralTrueFalse(test) ? null : Diagnostic.Create(IsTrueRule, location);
		}
	}
}
