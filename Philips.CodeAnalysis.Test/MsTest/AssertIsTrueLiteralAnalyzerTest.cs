﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.MsTest
{

	[TestClass]
	public class AssertIsTrueLiteralAnalyzerTest : AssertDiagnosticVerifier
	{
		#region Non-Public Properties/Methods

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AssertIsTrueLiteralAnalyzer();
		}

		#endregion

		#region Public Interface

		[DataTestMethod]
		[DataRow("Assert.IsTrue(true)")]
		[DataRow("Assert.IsTrue(false)")]
		[DataRow("Assert.IsTrue(!false)")]
		[DataRow("Assert.IsFalse(true)")]
		[DataRow("Assert.IsFalse(false)")]
		[DataRow("Assert.IsFalse(!false)")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CheckLiteralAsync(string given)
		{
			await VerifyError(given, DiagnosticId.AssertIsTrueLiteral.ToId()).ConfigureAwait(false);
		}

		#endregion
	}
}
