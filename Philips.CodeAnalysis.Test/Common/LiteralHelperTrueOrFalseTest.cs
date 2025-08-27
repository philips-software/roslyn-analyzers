// © 2024 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Common
{
	[TestClass]
	public class LiteralHelperTrueOrFalseTest : DiagnosticVerifier
	{

		[DiagnosticAnalyzer(LanguageNames.CSharp)]
		private sealed class IsTrueOrFalseAnalyzer : DiagnosticAnalyzerBase
		{
			private static readonly DiagnosticDescriptor TrueRule = new("TRUE", string.Empty, string.Empty, string.Empty, DiagnosticSeverity.Error, true);
			private static readonly DiagnosticDescriptor FalseRule = new("FALSE", string.Empty, string.Empty, string.Empty, DiagnosticSeverity.Error, true);


			public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(TrueRule, FalseRule);

			protected override void InitializeCompilation(CompilationStartAnalysisContext context)
			{
				context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ExpressionStatement);
			}

			private void Analyze(SyntaxNodeAnalysisContext context)
			{
				var statement = (ExpressionStatementSyntax)context.Node;
				ExpressionSyntax expression = statement.Expression;
				Location loc = expression.GetLocation();

				context.ReportDiagnostic(Helper.ForLiterals.IsTrueOrFalse(expression)
					? Diagnostic.Create(TrueRule, loc)
					: Diagnostic.Create(FalseRule, loc));
			}
		}

		[TestMethod]
		[DataRow("true", true),
		 DataRow("false", true),
		 DataRow("typeof(bool)", false),
		 DataRow("typeof(System.DateTime)", false),
		 DataRow("null", false),
		 DataRow("a", false),
		 DataRow("int.MaxValue", false),
		 DataRow("1d", false)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CheckIsTrueOrFalse(string expression, bool expectedToBeLiteral)
		{
			// Arrange
			var testCode = $"public void MethodA() {{{expression};}}";
			DiagnosticResult expected = new()
			{
				Id = expectedToBeLiteral ? "TRUE" : "FALSE",
				Severity = DiagnosticSeverity.Error,
				Location = new DiagnosticResultLocation("Test0.cs", null, null)
			};
			await VerifyDiagnostic(testCode, expected);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new IsTrueOrFalseAnalyzer();
		}
	}
}
