// © 2024 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
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
	public class LiteralHelperTest : DiagnosticVerifier
	{

		[DiagnosticAnalyzer(LanguageNames.CSharp)]
		private sealed class IsLiteralAnalyzer : DiagnosticAnalyzerBase
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

				context.ReportDiagnostic(Helper.ForLiterals.IsLiteral(expression, context.SemanticModel)
					? Diagnostic.Create(TrueRule, loc)
					: Diagnostic.Create(FalseRule, loc));
			}
		}

		[DataTestMethod]
		[DataRow("typeof(bool)", true),
		 DataRow("typeof(System.DateTime)", false),
		 DataRow("null", true),
		 DataRow("a", false),
		 DataRow("int.MaxValue", true),
		 DataRow("1d", true)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CheckIsLiteral(string expression, bool expectedToBeLiteral)
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

		[DataTestMethod]
		[DataRow(typeof(int), false)]
		[DataRow(typeof(int?), true)]
		[DataRow(typeof(string), true)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TryGetLiteralValueNullCasesTypeSpecific(Type type, bool expectedResult)
		{
			const string expression = "null";
			object expectedValue = null;

			var testCode = $"public class C {{ public void M() {{ var x = {expression}; }} }}";
			Document document = CreateDocument(testCode);
			SyntaxNode root = await document.GetSyntaxRootAsync();
			SemanticModel semanticModel = await document.GetSemanticModelAsync();
			VariableDeclaratorSyntax varDecl = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
			ExpressionSyntax valueExpr = varDecl.Initializer.Value;

			CodeFixHelper helper = new();

			bool result;
			object value;

			if (type == typeof(int))
			{
				result = helper.ForLiterals.TryGetLiteralValue<int>(valueExpr, semanticModel, out var v);
				value = v;
				expectedValue = 0;
			}
			else if (type == typeof(int?))
			{
				result = helper.ForLiterals.TryGetLiteralValue<int?>(valueExpr, semanticModel, out var v);
				value = v;
			}
			else if (type == typeof(string))
			{
				result = helper.ForLiterals.TryGetLiteralValue<string>(valueExpr, semanticModel, out var v);
				value = v;
			}
			else
			{
				Assert.Fail("Unsupported type for test.");
				return;
			}

			Assert.AreEqual(expectedResult, result);
			Assert.AreEqual(expectedValue, value);
		}

		[DataTestMethod]
		[DataRow("42", true, 42)]
		[DataRow("\"hello\"", true, "hello")]
		[DataRow("1.5", true, 1.5)]
		[DataRow("true", true, true)]
		[DataRow("a", false, null)]
		[DataRow("typeof(int)", false, null)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TryGetLiteralValueWorksAsExpected(string expression, bool expectedResult, object expectedValue)
		{
			var testCode = $"public class C {{ public void M() {{ var x = {expression}; }} }}";
			Document document = CreateDocument(testCode);
			SyntaxNode root = await document.GetSyntaxRootAsync();
			SemanticModel semanticModel = await document.GetSemanticModelAsync();
			VariableDeclaratorSyntax varDecl = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
			ExpressionSyntax valueExpr = varDecl.Initializer.Value;

			CodeFixHelper helper = new();
			var result = helper.ForLiterals.TryGetLiteralValue(valueExpr, semanticModel, out object value);

			Assert.AreEqual(expectedResult, result);
			if (expectedResult)
			{
				Assert.AreEqual(expectedValue, value);
			}
			else
			{
				Assert.IsNull(value);
			}
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new IsLiteralAnalyzer();
		}
	}
}
