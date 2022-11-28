using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation;

namespace Philips.CodeAnalysis.Test.Maintainability.Documentation
{
	[TestClass]
	public class XmlDocumentationShouldAddValueAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Data Members

		private const string configuredAdditionalUselessWords = "dummy,tummy";

		#endregion

		#region Non-Public Properties/Methods

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new XmlDocumentationShouldAddValueAnalyzer();

		protected override Dictionary<string, string> GetAdditionalAnalyzerConfigOptions()
		{
			Dictionary<string, string> options = new Dictionary<string, string>
			{
				{ $@"dotnet_code_quality.{ Helper.ToDiagnosticId(DiagnosticIds.XmlDocumentationShouldAddValue) }.additional_useless_words", configuredAdditionalUselessWords  }
			};
			return options;
		}


		#endregion

		#region Public Interface
		[TestMethod]
		public void EmptyClassTest()
		{
			string content = $@"
/// <summary></summary
public class Foo
{{
}}
";

			VerifyCSharpDiagnostic(content, DiagnosticResultHelper.CreateArray(DiagnosticIds.EmptyXmlComments));
		}

		[TestMethod]
		public void EmptyMethodTest()
		{
			string content = $@"
public class TestClass
{{
	/// <summary></summary
	public void Foo()
	{{
	}}
}}
";

			VerifyCSharpDiagnostic(content, DiagnosticResultHelper.CreateArray(DiagnosticIds.EmptyXmlComments));
		}

		[TestMethod]
		public void EmptyPropertyTest()
		{
			string content = $@"
public class TestClass
{{
	/// <summary></summary
	public int Foo {{ get; }}
}}
";

			VerifyCSharpDiagnostic(content, DiagnosticResultHelper.CreateArray(DiagnosticIds.EmptyXmlComments));
		}

		[TestMethod]
		public void EmptyFieldTest()
		{
			string content = $@"
public class TestClass
{{
	/// <summary></summary
	public int Foo;
}}
";

			VerifyCSharpDiagnostic(content, DiagnosticResultHelper.CreateArray(DiagnosticIds.EmptyXmlComments));
		}

		[TestMethod]
		public void EmptyEventTest()
		{
			string content = $@"
public class TestClass
{{
	/// <summary></summary
	public event EventHandler Foo;
}}
";

			VerifyCSharpDiagnostic(content, DiagnosticResultHelper.CreateArray(DiagnosticIds.EmptyXmlComments));
		}

		[DataRow("foo", true)]
		[DataRow("Gets Foo", true)]
		[DataRow("Gets the Foo", true)]
		[DataRow("Get an instance of Foo", true)]
		[DataRow("Gets an instance of Foo", true)]
		[DataRow("Get an instance of Foo to please Bar", false)]
		[DataTestMethod]
		public void ValueAddClassTests(string text, bool isError)
		{
			string content = $@"
/// <summary>{text}</summary
public class Foo
{{
}}
";

			VerifyCSharpDiagnostic(content, isError ? DiagnosticResultHelper.CreateArray(DiagnosticIds.XmlDocumentationShouldAddValue) : Array.Empty<DiagnosticResult>());
		}

		[DataRow("foo", true)]
		[DataRow("Gets Foo", true)]
		[DataRow("Gets the Foo", true)]
		[DataRow("Get an instance of Foo", true)]
		[DataRow("Gets an instance of Foo", true)]
		[DataRow("Gets an tummy Foo", true)]
		[DataRow("Gets an dummy Foo", true)]
		[DataRow("Get an instance of Foo to please Bar", false)]
		[DataTestMethod]
		public void ValueAddMethodTests(string text, bool isError)
		{
			string content = $@"
public class TestClass
{{
	/// <summary>{text}</summary
	public void Foo()
	{{
	}}
}}
";

			VerifyCSharpDiagnostic(content, isError ? DiagnosticResultHelper.CreateArray(DiagnosticIds.XmlDocumentationShouldAddValue) : Array.Empty<DiagnosticResult>());
		}

		[DataRow("foo", true)]
		[DataRow("Gets Foo", true)]
		[DataRow("Gets the Foo", true)]
		[DataRow("Get an instance of Foo", true)]
		[DataRow("Gets an instance of Foo", true)]
		[DataRow("Get an instance of Foo to please Bar", false)]
		[DataTestMethod]
		public void ValueAddPropertyTests(string text, bool isError)
		{
			string content = $@"
public class TestClass
{{
	/// <summary>{text}</summary
	public int Foo
	{{
		get;
	}}
}}
";

			VerifyCSharpDiagnostic(content, isError ? DiagnosticResultHelper.CreateArray(DiagnosticIds.XmlDocumentationShouldAddValue) : Array.Empty<DiagnosticResult>());
		}


		[DataRow("foo", true)]
		[DataRow("Gets Foo", true)]
		[DataRow("Gets the Foo", true)]
		[DataRow("Get an instance of Foo", true)]
		[DataRow("Gets an instance of Foo", true)]
		[DataRow("Get an instance of Foo to please Bar", false)]
		[DataTestMethod]
		public void ValueAddFieldTests(string text, bool isError)
		{
			string content = $@"
public class TestClass
{{
	/// <summary>{text}</summary
	public int Foo;
}}
";

			VerifyCSharpDiagnostic(content, isError ? DiagnosticResultHelper.CreateArray(DiagnosticIds.XmlDocumentationShouldAddValue) : Array.Empty<DiagnosticResult>());
		}

		[DataRow("On Foo", true)]
		[DataRow("Fire Foo", true)]
		[DataRow("Fires the Foo", true)]
		[DataRow("Fires the Foo event", true)]
		[DataRow("Raise Foo", true)]
		[DataRow("Raises an instance of the Foo event", true)]
		[DataRow("Raised when Foo happens", false)]
		[DataTestMethod]
		public void ValueAddEventTests(string text, bool isError)
		{
			string content = $@"
public class TestClass
{{
	/// <summary>{text}</summary
	public event EventHandler Foo;
}}
";

			VerifyCSharpDiagnostic(content, isError ? DiagnosticResultHelper.CreateArray(DiagnosticIds.XmlDocumentationShouldAddValue) : Array.Empty<DiagnosticResult>());
		}
		#endregion
	}
}
