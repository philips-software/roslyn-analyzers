// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;

namespace Philips.CodeAnalysis.Test.MsTest
{
	[TestClass]
	public class AvoidAttributeAnalyzerTest : DiagnosticVerifier
	{
		private const string allowedMethodName = @"Foo.AllowedInitializer()
Foo.AllowedInitializer(Bar)
Foo.WhitelistedFunction
";

		protected override (string name, string content)[] GetAdditionalTexts()
		{
			return new[] { ("NotFile.txt", "data"), (AvoidAttributeAnalyzer.AvoidAttributesWhitelist, allowedMethodName) };
		}

		[DataTestMethod]
		[DataRow(@"[TestMethod, Ignore]", 16)]
		[DataRow(@"[Ignore]", 4)]
		public void AvoidIgnoreAttributeTest(string test, int expectedColumn)
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
				Id = Helper.ToDiagnosticId(DiagnosticIds.AvoidIgnoreAttribute),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 5, expectedColumn)
				}
			};

			VerifyCSharpDiagnostic(givenText, expected);
		}


		[DataTestMethod]
		[DataRow(@"[TestMethod, Owner(""MK"")]", 16)]
		[DataRow(@"[Owner(""MK"")]", 4)]
		[DataRow(@"[TestMethod][Owner(""MK"")]", 16)]
		public void AvoidOwnerAttributeTest(string test, int expectedColumn)
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
				Id = Helper.ToDiagnosticId(DiagnosticIds.AvoidOwnerAttribute),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 5, expectedColumn)
				}
			};

			VerifyCSharpDiagnostic(givenText, expected);
		}

		[DataTestMethod]
		[DataRow(@"[TestInitialize]", 4)]
		public void AvoidTestInitializeMethodTest(string test, int expectedColumn)
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
				Id = Helper.ToDiagnosticId(DiagnosticIds.AvoidTestInitializeMethod),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 5, expectedColumn)
				}
			};

			VerifyCSharpDiagnostic(givenText, expected);
		}


		[DataTestMethod]
		[DataRow(@"[TestCleanup]", 4)]
		public void AvoidTestCleanupMethodTest(string test, int expectedColumn)
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
				Id = Helper.ToDiagnosticId(DiagnosticIds.AvoidTestCleanupMethod),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 5, expectedColumn)
				}
			};

			VerifyCSharpDiagnostic(givenText, expected);
		}


		[DataTestMethod]
		[DataRow(@"[ClassInitialize]", 4)]
		public void AvoidClassInitializeMethodTest(string test, int expectedColumn)
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
				Id = Helper.ToDiagnosticId(DiagnosticIds.AvoidClassInitializeMethod),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 5, expectedColumn)
				}
			};

			VerifyCSharpDiagnostic(givenText, expected);
		}


		[DataTestMethod]
		[DataRow(@"[ClassCleanup]", 4)]
		public void AvoidClassCleanupMethodTest(string test, int expectedColumn)
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
				Id = Helper.ToDiagnosticId(DiagnosticIds.AvoidClassCleanupMethod),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 5, expectedColumn)
				}
			};

			VerifyCSharpDiagnostic(givenText, expected);
		}

		[DataTestMethod]
		[DataRow(@"[ClassCleanup]")]
		[DataRow(@"[ClassInitialize]")]
		[DataRow(@"[TestInitialize]")]
		[DataRow(@"[TestCleanup]")]
		public void WhitelistIsApplied(string test)
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

			VerifyCSharpDiagnostic(givenText);
		}

		[DataTestMethod]
		[DataRow(@"[ClassCleanup]")]
		[DataRow(@"[ClassInitialize]")]
		[DataRow(@"[TestInitialize]")]
		[DataRow(@"[TestCleanup]")]
		public void WhitelistIsAppliedUnresolvable(string test)
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

			VerifyCSharpDiagnostic(givenText);
		}
		
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidAttributeAnalyzer();
		}
	}
}