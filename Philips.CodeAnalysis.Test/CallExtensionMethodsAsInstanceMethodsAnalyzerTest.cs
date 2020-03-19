using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers;

namespace Philips.CodeAnalysis.Test
{
	[TestClass]
	public class CallExtensionMethodsAsInstanceMethodsAnalyzerTest : CodeFixVerifier
	{
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new CallExtensionMethodsAsInstanceMethodsAnalyzer();
		}

		protected override CodeFixProvider GetCSharpCodeFixProvider()
		{
			return new CallExtensionMethodsAsInstanceMethodsCodeFixProvider();
		}
		protected override MetadataReference[] GetMetadataReferences()
		{
			string mockReference = typeof(Mock<>).Assembly.Location;
			MetadataReference reference = MetadataReference.CreateFromFile(mockReference);

			return base.GetMetadataReferences().Concat(new[] { reference }).ToArray();
		}

		[DataRow(true, "Foo.Bar(new object())", true, "new object().Bar()")]
		[DataRow(true, "new object().Bar()", false, "")]
		[DataRow(false, "Foo.Bar(new object())", false, "")]
		[DataRow(true, "Foo.Bar", false, "")]
		[DataRow(false, "Foo.Bar", false, "")]
		[DataTestMethod]
		public void ExtensionMethodErrors(bool isExtensionMethod, string call, bool isError, string fixedText)
		{
			const string Template = @"
using System;

public static class Foo
{{
  public static void Bar({0} object obj)
  {{
  }}
}}

public static class Program
{{
  public static void Main()
  {{
    {1};
  }}
}}
";

			string text = string.Format(Template, (isExtensionMethod ? "this" : ""), call);

			DiagnosticResult[] result = Array.Empty<DiagnosticResult>();
			if (isError)
			{
				result = new[] { DiagnosticResultHelper.Create(DiagnosticIds.ExtensionMethodsCalledLikeInstanceMethods) };
			}

			VerifyCSharpDiagnostic(text, result);

			if (!string.IsNullOrEmpty(fixedText))
			{
				string newText = string.Format(Template, (isExtensionMethod ? "this" : ""), fixedText);
				VerifyCSharpFix(text, newText);
			}
		}

		[TestMethod]
		public void ExtensionMethodCallSelfErrors()
		{
			const string Template = @"
using System;

public static class Foo
{{
  public static void Bar(this object obj)
  {{
    {0};
  }}

  public static void Bar(this object obj, object other)
  {{
  }}
}}
";
			var text = string.Format(Template, "Bar(obj, null)");

			DiagnosticResult[] result = new[] { DiagnosticResultHelper.Create(DiagnosticIds.ExtensionMethodsCalledLikeInstanceMethods) };

			VerifyCSharpDiagnostic(text, result);

			string newText = string.Format(Template, "obj.Bar(null)");
			VerifyCSharpFix(text, newText);
		}

		[TestMethod]
		public void ExtensionMethodCallSelfErrors2()
		{
			const string Template = @"
using System;
using System.Collections.Generic;
using System.Linq;

public static class Foo
{{
  public static void RemoveByKeys<TKey, TValue>(this IDictionary<TKey, TValue> dict, IEnumerable<TKey> keys)
  {{
    foreach (var item in keys)
      dict.Remove(item);
  }}

  public static void RemoveWhereValue<TKey, TValue>(this IDictionary<TKey, TValue> dict, Predicate<TValue> predicate)
  {{
    var items = new List<TKey>(dict.Keys);
    {0};
  }}
}}
";
			var text = string.Format(Template, "RemoveByKeys(dict, items)");

			DiagnosticResult[] result = new[] { DiagnosticResultHelper.Create(DiagnosticIds.ExtensionMethodsCalledLikeInstanceMethods) };

			VerifyCSharpDiagnostic(text, result);

			string newText = string.Format(Template, "dict.RemoveByKeys(items)");
			VerifyCSharpFix(text, newText);
		}

		[DataRow(@"
using System;

public static class ClassExtensions
{
  public static void Reset(this Foo foo)
  {
  }
}

public class Foo
{
  public void Reset()
  {
  }

  public void DoReset()
  {
    ClassExtensions.Reset(this);
  }
}
", false)]
		[DataRow(@"
using System;

public static class ClassExtensions
{
  public static void Reset(this Foo foo)
  {
  }
}

public class Foo
{
  public void Reset()
  {
  }
}

public class Bar : Foo
{
  public void DoReset()
  {
    ClassExtensions.Reset(this);
  }
}
", false)]
		[DataRow(@"
using System;
using Moq;

public class Foo
{
}

public class Bar : Mock<Foo>
{
  public void Reset()
  {
    MockExtensions.Reset(this);
  }
}
", false)]
		[DataRow(@"
using System;
using Moq;

public class Foo
{
}

public class Bar : Mock<Foo>
{
  public void Reset()
  {
    MockExtensions.Reset(this);
  }
}
", false)]
		[DataRow(@"
using System;
using Moq;

public class Foo
{
}

public class Bar : Mock<Foo>
{
  public void Reset()
  {
  }
}

public class Baz
{
  public void DoAThing()
  {
    MockExtensions.Reset(new Bar());
  }
}
", false)]
		[DataRow(@"
using System;
using Moq;

public class Foo
{
}

public class Bar : Mock<Foo>
{
}

public static class BarExtensions
{
  public static void Reset(this Bar bar)
  {
  }
}

public class Baz
{
  public void DoAThing()
  {
    MockExtensions.Reset(new Bar());
  }
}
", false)]
		[DataRow(@"
using System;
using Moq;

public class Foo
{
}

public class Bar : Mock<Foo>
{
}

public static class BarExtensions
{
  public static void Reset(this Bar bar, int i)
  {
  }
}

public class Baz
{
  public void DoAThing()
  {
    MockExtensions.Reset(new Bar());
  }
}
", true)]
		[DataTestMethod]
		public void ExtensionMethodsDontCallDifferentMethods(string template, bool isError)
		{
			DiagnosticResult[] result = Array.Empty<DiagnosticResult>();
			if (isError)
			{
				result = new[] { DiagnosticResultHelper.Create(DiagnosticIds.ExtensionMethodsCalledLikeInstanceMethods) };
			}

			VerifyCSharpDiagnostic(template, result);
		}
	}
}
