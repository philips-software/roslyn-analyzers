// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.


using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Philips.CodeAnalysis.Test
{
	/// <summary>
	/// 
	/// </summary>
	public class AssertCodeHelper
	{
		#region Non-Public Data Members

		private static string TestTemplate = @"
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

		#endregion

		#region Non-Public Properties/Methods

		#endregion

		#region Public Interface

		public string GetText(string methodBody, string otherClass, string attributes)
		{
			return string.Format(TestTemplate, otherClass, attributes, methodBody);
		}

		public MetadataReference[] GetMetaDataReferences()
		{
			//add symbols for assert
			return new[] { MetadataReference.CreateFromFile(typeof(Assert).Assembly.Location), MetadataReference.CreateFromFile(typeof(TimeoutAttribute).Assembly.Location) };
		}

		#endregion
	}
}
