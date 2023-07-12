// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Common
{
	[TestClass]
	public class SingleDiagnosticAnalyzerFullyQualifiedNameTest : DiagnosticVerifier
	{

		[DiagnosticAnalyzer(LanguageNames.CSharp)]
		private sealed class UnknownFullyQualifiedNameAnalyzer : SingleDiagnosticAnalyzer<ClassDeclarationSyntax, UnknownFullyQualifiedNameAnalyzer.UnknownFullyQualifiedNameSyntaxNodeAction>
		{
			public UnknownFullyQualifiedNameAnalyzer() : base(DiagnosticId.AssertAreEqual, "InvalidType", "", "", Categories.MsTest)
			{
				FullyQualifiedMetaDataName = "InvalidType";
			}

			public sealed class UnknownFullyQualifiedNameSyntaxNodeAction : SyntaxNodeAction<ClassDeclarationSyntax>
			{
				public override void Analyze()
				{
					Location loc = Node.GetLocation();
					Context.ReportDiagnostic(Diagnostic.Create(Rule, loc));
				}
			}
		}

		[TestMethod]
		public async Task InvalidQualifiedNameIsIgnored()
		{
			// Arrange
			// Act
			// Assert
			await VerifySuccessfulCompilation("public class A {}");
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new UnknownFullyQualifiedNameAnalyzer();
		}
	}
}
