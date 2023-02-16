﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.MsTest
{
	[TestClass]
	public class TestClassPublicMethodShouldBeTestMethodTest : DiagnosticVerifier
	{
		protected override ImmutableArray<(string name, string content)> GetAdditionalSourceCode()
		{
			string code = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

public class DerivedTestMethod : TestMethodAttribute
{
}

";

			return base.GetAdditionalSourceCode().Add(("DerivedTestMethod.cs", code));
		}

		[DataTestMethod]
		[DataRow(@"public", true)]
		[DataRow(@"private", false)]
		[DataRow(@"internal", false)]
		[DataRow(@"protected", false)]
		[DataRow(@"protected internal", false)]
		[DataRow(@"private protected", false)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MethodAccessModifierTestAsync(string given, bool isError)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
class Foo 
{{

  {0} void Foo()
  {{
  }}
}}
";

			await VerifyErrorAsync(baseline, given, isError).ConfigureAwait(false);

		}

		[DataTestMethod]
		[DataRow(@"[TestClass]", true)]
		[DataRow(@"", false)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ClassTypeTestAsync(string given, bool isError)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
{0}
class Foo
{{

  public void Foo()
  {{
  }}
}}
";
			await VerifyErrorAsync(baseline, given, isError).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow(@"[DerivedTestMethod]", false)]
		[DataRow(@"[TestMethod]", false)]
		[DataRow(@"[DataTestMethod]", false)]
		[DataRow(@"[AssemblyInitialize()]", false)]
		[DataRow(@"[TestCleanup()]", false)]
		[DataRow(@"[ClassInitialize()]", false)]
		[DataRow(@"[TestInitialize()]", false)]
		[DataRow(@"[ClassCleanup()]", false)]
		[DataRow(@"[ClassInitialize()", false)]
		[DataRow(@"", true)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task MethodTypeTestAsync(string given, bool isError)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
[TestClass]
class Foo 
{{
  {0}
  public void Foo()
  {{
  }}
}}
";
			await VerifyErrorAsync(baseline, given, isError).ConfigureAwait(false);

		}

		private async Task VerifyErrorAsync(string baseline, string given, bool isError)
		{
			string givenText = string.Format(baseline, given);
			if (isError)
			{
				var result = new DiagnosticResult()
				{
					Id = Helper.ToDiagnosticId(DiagnosticId.TestClassPublicMethodShouldBeTestMethod),
					Message = new Regex(TestClassPublicMethodShouldBeTestMethodAnalyzer.MessageFormat),
					Severity = DiagnosticSeverity.Error,
					Locations = new[]
					{
						new DiagnosticResultLocation("Test0.cs", 7, 3)
					}
				};
				await VerifyDiagnostic(givenText, result).ConfigureAwait(false);
			}
			else
			{
				await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
			}
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new TestClassPublicMethodShouldBeTestMethodAnalyzer();
		}
	}
}
