// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidUnnecessaryWhereAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidUnnecessaryWhereAnalyzer();
		}

		[DataRow("_ = result.OfType<string>().Where(x => true).Count();")]
		[DataRow("result.OfType<string>().Where(x => true).Count();")]
		[DataRow("result.OfType<string>().Where(x => true).Any();")]
		[DataRow("result.OfType<string>().Where(x => true).First();")]
		[DataRow("result.OfType<string>().Where(x => true).FirstOrDefault();")]
		[DataRow("result.OfType<string>().Where(x => true).Last();")]
		[DataRow("result.OfType<string>().Where(x => true).LastOrDefault();")]
		[DataRow("result.OfType<string>().Where(x => true).Single();")]
		[DataRow("result.OfType<string>().Where(x => true).SingleOrDefault();")]
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidUnnecessaryWhereTest(string line)
		{
			var template = @"
using System.Linq;
class Foo
{{
  public void MyTest()
  {{
    var result = Array.Empty<string>();
    {0}
  }}
}}
";
			var testCode = string.Format(template, line);
			await VerifyDiagnostic(testCode).ConfigureAwait(false);
		}


		[DataRow("result.OfType<string>().Where((x => true)).Count(x => true);")]
		[DataRow("result.OfType<string>().Where(x => true).Count(x => true);")]
		[DataRow("result.OfType<string>().Where(x => true).Any(x => true);")]
		[DataRow("result.OfType<string>().Where(x => true).First(x => true);")]
		[DataRow("result.OfType<string>().Where(x => true).FirstOrDefault(x => true);")]
		[DataRow("result.OfType<string>().Where(x => true).Last(x => true);")]
		[DataRow("result.OfType<string>().Where(x => true).LastOrDefault(x => true);")]
		[DataRow("result.OfType<string>().Where(x => true).Single(x => true);")]
		[DataRow("result.OfType<string>().Where(x => true).SingleOrDefault(x => true);")]
		[DataRow("result.OfType<string>().Where(x => true).Contains(\"hi\");")]
		[DataRow("result.OfType<string>().First().Single(x => true);")]
		[DataRow("result.OfType<string>().First().Any();")]
		[DataRow("Console.WriteLine(\"ny\")")]
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidUnnecessaryWhereNoFindingTest(string line)
		{
			var template = @"
using System.Linq;
class Foo
{{
  public void MyTest()
  {{
    var result = Array.Empty<string>();
    {0}
  }}
}}
";
			var testCode = string.Format(template, line);
			await VerifySuccessfulCompilation(testCode).ConfigureAwait(false);
		}

		[DataRow("result.OfType<string>().Where((x => true)).Count();")]
		[DataRow("result.OfType<string>().Where(x => true).Count();")]
		[DataRow("result.OfType<string>().Where(x => true).Any();")]
		[DataRow("result.OfType<string>().Where(x => true).First();")]
		[DataRow("result.OfType<string>().Where(x => true).FirstOrDefault();")]
		[DataRow("result.OfType<string>().Where(x => true).Last();")]
		[DataRow("result.OfType<string>().Where(x => true).LastOrDefault();")]
		[DataRow("result.OfType<string>().Where(x => true).Single();")]
		[DataRow("result.OfType<string>().Where(x => true).SingleOrDefault();")]
		[DataRow("result.OfType<string>().First().Single();")]
		[DataRow("result.OfType<string>().First().Any();")]
		[DataRow("Console.WriteLine(\"ny\")")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task UnnecessaryWhereWithSecondExpression(string line)
		{
			var template = @"
using System.Linq;
class Foo
{{
  public void MyTest()
  {{
    var result = Array.Empty<string>();
    {0}
  }}
}}
";
			var testCode = string.Format(template, line);
			await VerifyDiagnostic(testCode).ConfigureAwait(false);
		}
	}
}

