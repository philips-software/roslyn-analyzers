// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class AvoidAttributeAnalyzerTest : DiagnosticVerifier
	{
		private const string allowedMethodName = @"Foo.AllowedInitializer()
Foo.AllowedInitializer(Bar)
Foo.WhitelistedFunction
";

		protected override ImmutableArray<(string name, string content)> GetAdditionalTexts()
		{
			return base.GetAdditionalTexts().Add(("NotFile.txt", "data")).Add((AvoidAttributeAnalyzer.AttributesWhitelist, allowedMethodName));
		}

		[DataTestMethod]
		[DataRow(@"[TestMethod, Ignore]", 16)]
		[DataRow(@"[Ignore]", 4)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidIgnoreAttributeTestAsync(string test, int expectedColumn)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo 
{{
  {0}
  public void Foo()
  {{
  }}
}}
";
			string givenText = string.Format(baseline, test);

			DiagnosticResult expected = new()
			{
				Id = Helper.ToDiagnosticId(DiagnosticId.AvoidIgnoreAttribute),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 5, expectedColumn)
				}
			};

			await VerifyDiagnostic(givenText, expected).ConfigureAwait(false);
		}


		[DataTestMethod]
		[DataRow(@"[TestMethod, Owner(""MK"")]", 16)]
		[DataRow(@"[Owner(""MK"")]", 4)]
		[DataRow(@"[TestMethod][Owner(""MK"")]", 16)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidOwnerAttributeTestAsync(string test, int expectedColumn)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo 
{{
  {0}
  public void Foo()
  {{
  }}
}}
";
			string givenText = string.Format(baseline, test);

			DiagnosticResult expected = new()
			{
				Id = Helper.ToDiagnosticId(DiagnosticId.AvoidOwnerAttribute),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 5, expectedColumn)
				}
			};

			await VerifyDiagnostic(givenText, expected).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow(@"[TestInitialize]", 4)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidTestInitializeMethodTestAsync(string test, int expectedColumn)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo 
{{
  {0}
  public void Foo()
  {{
  }}
}}
";
			string givenText = string.Format(baseline, test);

			DiagnosticResult expected = new()
			{
				Id = Helper.ToDiagnosticId(DiagnosticId.AvoidTestInitializeMethod),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 5, expectedColumn)
				}
			};

			await VerifyDiagnostic(givenText, expected).ConfigureAwait(false);
		}


		[DataTestMethod]
		[DataRow(@"[TestCleanup]", 4)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidTestCleanupMethodTestAsync(string test, int expectedColumn)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo 
{{
  {0}
  public void Foo()
  {{
  }}
}}
";
			string givenText = string.Format(baseline, test);

			DiagnosticResult expected = new()
			{
				Id = Helper.ToDiagnosticId(DiagnosticId.AvoidTestCleanupMethod),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 5, expectedColumn)
				}
			};

			await VerifyDiagnostic(givenText, expected).ConfigureAwait(false);
		}


		[DataTestMethod]
		[DataRow(@"[ClassInitialize]", 4)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidClassInitializeMethodTestAsync(string test, int expectedColumn)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo 
{{
  {0}
  public void Foo()
  {{
  }}
}}
";
			string givenText = string.Format(baseline, test);

			DiagnosticResult expected = new()
			{
				Id = Helper.ToDiagnosticId(DiagnosticId.AvoidClassInitializeMethod),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 5, expectedColumn)
				}
			};

			await VerifyDiagnostic(givenText, expected).ConfigureAwait(false);
		}


		[DataTestMethod]
		[DataRow(@"[ClassCleanup]", 4)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidClassCleanupMethodTestAsync(string test, int expectedColumn)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo 
{{
  {0}
  public void Foo()
  {{
  }}
}}
";
			string givenText = string.Format(baseline, test);

			DiagnosticResult expected = new()
			{
				Id = Helper.ToDiagnosticId(DiagnosticId.AvoidClassCleanupMethod),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 5, expectedColumn)
				}
			};

			await VerifyDiagnostic(givenText, expected).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow(@"[ClassCleanup]")]
		[DataRow(@"[ClassInitialize]")]
		[DataRow(@"[TestInitialize]")]
		[DataRow(@"[TestCleanup]")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhitelistIsAppliedAsync(string test)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo 
{{
  {0}
  public void AllowedInitializer()
  {{
  }}
}}
";
			string givenText = string.Format(baseline, test);

			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow(@"[ClassCleanup]")]
		[DataRow(@"[ClassInitialize]")]
		[DataRow(@"[TestInitialize]")]
		[DataRow(@"[TestCleanup]")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WhitelistIsAppliedUnresolvableAsync(string test)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

class Foo 
{{
  {0}
  public void AllowedInitializer(Bar b)
  {{
  }}
}}
";
			string givenText = string.Format(baseline, test);

			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidAttributeAnalyzer();
		}
	}
}