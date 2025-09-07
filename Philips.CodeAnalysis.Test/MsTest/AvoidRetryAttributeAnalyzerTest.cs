// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	public class AvoidRetryAttributeAnalyzerTest : DiagnosticVerifier
	{
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidRetryAttributeTestAsync()
		{
			var givenText = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.VisualStudio.TestTools.UnitTesting
{
    public class RetryAttribute : System.Attribute
    {
        public RetryAttribute(int count) { }
    }
}

[TestClass]
class Foo 
{
  [TestMethod]
  [Retry(3)]
  public void TestMethod()
  {
    Assert.IsTrue(true);
  }
}
";

			DiagnosticResult expected = new()
			{
				Id = DiagnosticId.AvoidRetryAttribute.ToId(),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 16, 4)
				}
			};

			await VerifyDiagnostic(givenText, expected).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidCustomRetryAttributeTestAsync()
		{
			var givenText = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.VisualStudio.TestTools.UnitTesting
{
    public class RetryBaseAttribute : System.Attribute
    {
        public RetryBaseAttribute(int count) { }
    }
}

public class CustomRetryAttribute : Microsoft.VisualStudio.TestTools.UnitTesting.RetryBaseAttribute
{
    public CustomRetryAttribute(int count) : base(count) { }
}

[TestClass]
class Foo 
{
  [TestMethod]
  [CustomRetry(5)]
  public void TestMethod()
  {
    Assert.IsTrue(true);
  }
}
";

			DiagnosticResult expected = new()
			{
				Id = DiagnosticId.AvoidRetryAttribute.ToId(),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 21, 4)
				}
			};

			await VerifyDiagnostic(givenText, expected).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task NonRetryAttributesAreIgnoredAsync()
		{
			var givenText = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
class Foo 
{
  [TestMethod]
  [TestCategory(""Unit"")]
  public void TestMethod()
  {
    Assert.IsTrue(true);
  }
}
";

			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidAttributeAnalyzer();
		}
	}
}