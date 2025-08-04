// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

namespace Philips.CodeAnalysis.Test.Helpers
{
	internal sealed class AssertCodeHelper
	{
		private static readonly string TestTemplate = @"
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestApplication
{{
  [TestClass]
  class TestClass
  {{
    {0}

    {1}
    public void FooTest()
    {{
      {2};
    }}
  }}
}}";

		public string GetText(string methodBody, string otherClass, string attributes)
		{
			return string.Format(TestTemplate, otherClass, attributes, methodBody);
		}
	}
}
