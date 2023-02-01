// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Serialization;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using Philips.CodeAnalysis.Test.Verifiers;
using Philips.CodeAnalysis.Test.Helpers;

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
		[DataTestMethod]
		public void AvoidUnnecessaryWhereTest(string line)
		{
			string template = @"
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
			string testCode = string.Format(template, line);
			VerifyDiagnostic(testCode, DiagnosticResultHelper.Create(DiagnosticIds.AvoidUnnecessaryWhere));
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
		[DataRow("result.OfType<string>().Where(x => true).Any")]
		[DataRow("result.OfType<string>().First().Single(x => true);")]
		[DataRow("result.OfType<string>().First.Any());)")]
		[DataRow("Console.WriteLine(\"ny\")")]
		[DataTestMethod]
		public void AvoidUnnecessaryWhereNoFindingTest(string line)
		{
			string template = @"
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
			string testCode = string.Format(template, line);
			VerifySuccessfulCompilation(testCode);
		}
	}
}

