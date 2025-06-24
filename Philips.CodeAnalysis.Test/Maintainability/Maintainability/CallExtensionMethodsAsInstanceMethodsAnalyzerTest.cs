// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class CallExtensionMethodsAsInstanceMethodsAnalyzerTest : CodeFixVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new CallExtensionMethodsAsInstanceMethodsAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new CallExtensionMethodsAsInstanceMethodsCodeFixProvider();
		}
		protected override ImmutableArray<MetadataReference> GetMetadataReferences()
		{
			var mockReference = typeof(Mock<>).Assembly.Location;
			MetadataReference reference = MetadataReference.CreateFromFile(mockReference);
			return base.GetMetadataReferences().Add(reference);
		}

		[DataRow(true, "Foo.Bar(new object())", true, "new object().Bar()")]
		[DataRow(true, "new object().Bar()", false, "")]
		[DataRow(false, "Foo.Bar(new object())", false, "")]
		[DataRow(true, "Foo.Bar", false, "")]
		[DataRow(false, "Foo.Bar", false, "")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ExtensionMethodErrors(bool isExtensionMethod, string call, bool isError, string fixedText)
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

			var text = string.Format(Template, isExtensionMethod ? "this" : "", call);
			if (isError)
			{
				await VerifyDiagnostic(text, DiagnosticId.ExtensionMethodsCalledLikeInstanceMethods).ConfigureAwait(false);
			}
			else
			{
				await VerifySuccessfulCompilation(text).ConfigureAwait(false);
			}

			if (!string.IsNullOrEmpty(fixedText))
			{
				var newText = string.Format(Template, isExtensionMethod ? "this" : "", fixedText);
				await VerifyFix(text, newText).ConfigureAwait(false);
			}
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ExtensionMethodCallSelfErrors()
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
			await VerifyDiagnostic(text, DiagnosticId.ExtensionMethodsCalledLikeInstanceMethods).ConfigureAwait(false);

			var newText = string.Format(Template, "obj.Bar(null)");
			await VerifyFix(text, newText).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ExtensionMethodCallSelfErrors2()
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
			await VerifyDiagnostic(text, DiagnosticId.ExtensionMethodsCalledLikeInstanceMethods).ConfigureAwait(false);

			var newText = string.Format(Template, "dict.RemoveByKeys(items)");
			await VerifyFix(text, newText).ConfigureAwait(false);
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
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ExtensionMethodsDontCallDifferentMethodsAsync(string template, bool isError)
		{
			if (isError)
			{
				await VerifyDiagnostic(template, DiagnosticId.ExtensionMethodsCalledLikeInstanceMethods).ConfigureAwait(false);
			}
			else
			{
				await VerifySuccessfulCompilation(template).ConfigureAwait(false);
			}
		}
	}
}
