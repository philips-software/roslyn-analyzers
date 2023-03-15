// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	[TestClass]
	public class PreventUnnecessaryRangeChecksAnalyzerTest : CodeFixVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new PreventUnnecessaryRangeChecksAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new PreventUnnecessaryRangeChecksCodeFixProvider();
		}

		[DataRow("int[] data = new int[0]", "Length")]
		[DataRow("int[] data = new int[0]", "Count()")]
		[DataRow("List<int> data = new List<int>()", "Count")]
		[DataRow("List<int> data = new List<int>()", "Count()")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CheckArrayRange(string declaration, string countLengthMethod)
		{
			const string template = @"
class Foo
{{
  public void test()
  {{
    {0};
    // comment
    if(data.{1} > 0)
    {{
      foreach (int i in data)
      {{
      }}
      //middle comment
    }}
    // end comment
  }}
}}
";

			const string fixedTemplate = @"
class Foo
{{
  public void test()
  {{
    {0};
    // comment
    foreach (int i in data)
    {{
    }}
    //middle comment
    // end comment
  }}
}}
";

			var errorCode = string.Format(template, declaration, countLengthMethod);

			await VerifyDiagnostic(errorCode).ConfigureAwait(false);
			await VerifyFix(errorCode, string.Format(fixedTemplate, declaration)).ConfigureAwait(false);
		}

		[DataRow("int[] data = new int[0]", "Length")]
		[DataRow("int[] data = new int[0]", "Count()")]
		[DataRow("List<int> data = new List<int>()", "Count")]
		[DataRow("List<int> data = new List<int>()", "Count()")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CheckElseClause(string declaration, string countLengthMethod)
		{
			const string template = @"
class Foo
{{
  public void test()
  {{
    {0};
    // comment
    if(data.{1} > 0)
    {{
      foreach (int i in data)
      {{
      }}
      //middle comment
    }}
    else
    {{ }}
    // end comment
  }}
}}
";
			var errorCode = string.Format(template, declaration, countLengthMethod);
			await VerifySuccessfulCompilation(errorCode).ConfigureAwait(false);
		}


		[DataRow("int[] data = new int[0]", "Length")]
		[DataRow("int[] data = new int[0]", "Count()")]
		[DataRow("List<int> data = new List<int>()", "Count")]
		[DataRow("List<int> data = new List<int>()", "Count()")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CheckNestedRange(string declaration, string countLengthMethod)
		{
			const string template = @"
public class Container
{{
  public {0};
}}

class Foo
{{
  public void test()
  {{
    Container container = new Container();
    // comment
    if(container.data.{1} > 0)
    {{
      foreach (int i in container.data)
      {{
      }}
      //middle comment
    }}
    // end comment
  }}
}}
";

			const string fixedTemplate = @"
public class Container
{{
  public {0};
}}

class Foo
{{
  public void test()
  {{
    Container container = new Container();
    // comment
    foreach (int i in container.data)
    {{
    }}
    //middle comment
    // end comment
  }}
}}
";

			var errorCode = string.Format(template, declaration, countLengthMethod);

			await VerifyDiagnostic(errorCode).ConfigureAwait(false);
			await VerifyFix(errorCode, string.Format(fixedTemplate, declaration)).ConfigureAwait(false);
		}

		[DataRow("int[] data = new int[0]", "Length")]
		[DataRow("int[] data = new int[0]", "Count()")]
		[DataRow("List<int> data = new List<int>()", "Count")]
		[DataRow("List<int> data = new List<int>()", "Count()")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CheckNestedRange2(string declaration, string countLengthMethod)
		{
			const string template = @"
public class Container
{{
  public {0};
}}

class Foo
{{
  public void test()
  {{
    Container container1 = new Container();
    Container container2 = new Container();
    // comment
    if(container1.data.{1} > 0)
    {{
      foreach (int i in container2.data)
      {{
      }}
      //middle comment
    }}
    // end comment
  }}
}}
";
			var errorCode = string.Format(template, declaration, countLengthMethod);

			await VerifySuccessfulCompilation(errorCode).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CheckNestedRange2a()
		{
			const string template = @"
public class Container
{{
  public List<int> Data {{ get; set; }}
}}

class Foo
{{
  public void test()
  {{
    Container container1 = new Container();
    Container container2 = new Container();

    List<string> other = new List<string>();
    // comment
    if(other.Count > 0)
    {{
      foreach (int i in container2.data)
      {{
      }}
      //middle comment
    }}
    // end comment
  }}
}}
";

			await VerifySuccessfulCompilation(template).ConfigureAwait(false);
		}

		[DataRow("int[] data = new int[0]", "Length")]
		[DataRow("int[] data = new int[0]", "Count()")]
		[DataRow("List<int> data = new List<int>()", "Count")]
		[DataRow("List<int> data = new List<int>()", "Count()")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CheckNestedRange3(string declaration, string countLengthMethod)
		{
			const string template = @"
public class OuterContainer
{{
  public Container container;
}}

public class Container
{{
  public {0};
}}

class Foo
{{
  public void test()
  {{
    OuterContainer container1 = new OuterContainer();
    OuterContainer container2 = new OuterContainer();
    // comment
    if(container1.container.data.{1} > 0)
    {{
      foreach (int i in container2.container.data)
      {{
      }}
      //middle comment
    }}
    // end comment
  }}
}}
";
			var errorCode = string.Format(template, declaration, countLengthMethod);

			await VerifySuccessfulCompilation(errorCode).ConfigureAwait(false);
		}

		[DataRow("int[] data = new int[0]", "Length")]
		[DataRow("int[] data = new int[0]", "Count()")]
		[DataRow("List<int> data = new List<int>()", "Count")]
		[DataRow("List<int> data = new List<int>()", "Count()")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CheckArrayRangeBraces(string declaration, string countLengthMethod)
		{
			const string template = @"
class Foo
{{
  public void test()
  {{
    {0};
    // comment
    if(data.{1} > 0)
      foreach (int i in data)
      {{
      }}
    // end comment
  }}
}}
";

			const string fixedTemplate = @"
class Foo
{{
  public void test()
  {{
    {0};
    // comment
    foreach (int i in data)
    {{
    }}
    // end comment
  }}
}}
";

			var errorCode = string.Format(template, declaration, countLengthMethod);

			await VerifyDiagnostic(errorCode).ConfigureAwait(false);
			await VerifyFix(errorCode, string.Format(fixedTemplate, declaration)).ConfigureAwait(false);
		}

		[DataRow("int[] data = new int[0]", "Length")]
		[DataRow("int[] data = new int[0]", "Count()")]
		[DataRow("List<int> data = new List<int>()", "Count")]
		[DataRow("List<int> data = new List<int>()", "Count()")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CheckArrayRangeNoBraces(string declaration, string countLengthMethod)
		{
			const string template = @"
class Foo
{{
  public void test()
  {{
    {0};
    // comment
    if(data.{1} > 0)
      foreach (int i in data)
        i.ToString();
    // end comment
  }}
}}
";

			const string fixedTemplate = @"
class Foo
{{
  public void test()
  {{
    {0};
    // comment
    foreach (int i in data)
      i.ToString();
    // end comment
  }}
}}
";

			var errorCode = string.Format(template, declaration, countLengthMethod);

			await VerifyDiagnostic(errorCode).ConfigureAwait(false);
			await VerifyFix(errorCode, string.Format(fixedTemplate, declaration)).ConfigureAwait(false);
		}

		[DataRow("int[] data = new int[0]", "Length")]
		[DataRow("int[] data = new int[0]", "Count()")]
		[DataRow("List<int> data = new List<int>()", "Count")]
		[DataRow("List<int> data = new List<int>()", "Count()")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CheckArrayRange2(string declaration, string countLengthMethod)
		{
			const string template = @"
using System.Linq;
using System;
using System.Collections.Generic;

class Foo {{
public void test()
{{
	{0};
	if(data.{1} > 0)
	{{
		foreach(int i in data)
		{{
		}}

		Console.WriteLine(data.{1}.ToString());
	}}
}}
}}
";

			await VerifySuccessfulCompilation(string.Format(template, declaration, countLengthMethod)).ConfigureAwait(false);
		}

		[DataRow("int[] data = new int[0]", "Length")]
		[DataRow("int[] data = new int[0]", "Count()")]
		[DataRow("List<int> data = new List<int>()", "Count")]
		[DataRow("List<int> data = new List<int>()", "Count()")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CheckArrayRange3(string declaration, string countLengthMethod)
		{
			const string template = @"
using System.Linq;
using System;
using System.Collections.Generic;

class Foo {{
public void test()
{{
	{0};
	if(data.{1} != 0)
	{{
		foreach(int i in data)
		{{
		}}
	}}
}}
}}
";

			await VerifyDiagnostic(string.Format(template, declaration, countLengthMethod)).ConfigureAwait(false);
		}

		[DataRow("int[] data = new int[0]", "Length")]
		[DataRow("int[] data = new int[0]", "Count()")]
		[DataRow("List<int> data = new List<int>()", "Count")]
		[DataRow("List<int> data = new List<int>()", "Count()")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CheckArrayRange4(string declaration, string countLengthMethod)
		{
			const string template = @"
using System.Linq;
using System;
using System.Collections.Generic;

class Foo {{
public void test()
{{
	{0};
	if(data.{1} != 0)
	{{
		foreach(int i in data)
		{{
		}}

		Console.WriteLine(data.{1}.ToString());
	}}
}}
}}
";

			await VerifySuccessfulCompilation(string.Format(template, declaration, countLengthMethod)).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CheckMissingCondition()
		{
			const string givenText = @"
using System;

class Foo {{
public void test(string data)
{{
	if()
	{{
		foreach(int i in data)
		{{
		}}
		Console.WriteLine(data.ToString());
	}}
}}
}}
";

			await VerifySuccessfulCompilation(givenText).ConfigureAwait(false);
		}

		[DataTestMethod]
		[DataRow("data == null")]
		[DataRow("data == data")]
		[DataRow("data.Length == 4")]
		[DataRow("data.Clone() == data")]
		[DataRow("data.Length == 4 || data.Length == 5")]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CheckBooleanCondition(string condition)
		{
			const string template = @"
using System;

class Foo {{
public void test(string data)
{{
	if({0})
	{{
		foreach(int i in data)
		{{
		}}
		Console.WriteLine(data.ToString());
	}}
}}
}}
"
			;

			await VerifySuccessfulCompilation(string.Format(template, condition)).ConfigureAwait(false);
		}
	}
}
