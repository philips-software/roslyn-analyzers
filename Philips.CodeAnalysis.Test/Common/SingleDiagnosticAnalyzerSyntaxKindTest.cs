// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Common
{
	[TestClass]
	public class SingleDiagnosticAnalyzerSyntaxKindTest : DiagnosticVerifier
	{

		[DiagnosticAnalyzer(LanguageNames.CSharp)]
		private sealed class UnknownSyntaxKindAnalyzer : SingleDiagnosticAnalyzer<CrefParameterListSyntax, UnknownSyntaxKindAnalyzer.UnknownSyntaxKindSyntaxNodeAction>
		{
			public UnknownSyntaxKindAnalyzer() : base(DiagnosticId.AssertAreEqual, "UnknownSyntaxKind", "", "", Categories.MsTest)
			{
			}

			public sealed class UnknownSyntaxKindSyntaxNodeAction : SyntaxNodeAction<CrefParameterListSyntax>
			{
				public override void Analyze()
				{
					Location loc = Node.GetLocation();
					Context.ReportDiagnostic(Diagnostic.Create(Rule, loc));
				}
			}
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task UnknownSyntaxKindThrowsException()
		{
			// Arrange
			var expectedResult = new DiagnosticResult() { Id = "AD0001", Location = new DiagnosticResultLocation(-1) };
			// Act
			// Assert
			AssertFailedException actualException = await Assert.ThrowsExactlyAsync<AssertFailedException>(
				() => VerifyDiagnostic("public class A {}", expectedResult));
			StringAssert.Contains(actualException.Message, "SyntaxKind");
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new UnknownSyntaxKindAnalyzer();
		}
	}
}
