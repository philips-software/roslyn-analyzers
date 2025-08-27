// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidPublicMemberVariableAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidPublicMemberVariableAnalyzer();
		}

		/// <summary>
		/// Verify that member variables are initialized 
		/// Ignore strunct / static / const
		/// </summary>
		/// <param name="content"></param>
		/// <param name="isError"></param>
		[TestMethod]
		[DataRow("", false)]
		[DataRow("public const int InitialCount = 1;", false)]
		[DataRow("public int i = 1;", true)]
		[DataRow("static int i = 1;", false)]
		[DataRow("public static int i = 1;", false)]
		[DataRow("static readonly int i = 1;", false)]
		[DataRow("private int i = 1;", false)]
		[DataRow("private struct testStruct { public int i; }", false)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidPublicMemberVariablesTestAsync(string content, bool isError)
		{
			const string template = @"public class C {{    {0}       }}";
			var classContent = string.Format(template, content);
			if (isError)
			{
				await VerifyDiagnostic(classContent).ConfigureAwait(false);
			}
			else
			{
				await VerifySuccessfulCompilation(classContent).ConfigureAwait(false);
			}
		}
	}
}
