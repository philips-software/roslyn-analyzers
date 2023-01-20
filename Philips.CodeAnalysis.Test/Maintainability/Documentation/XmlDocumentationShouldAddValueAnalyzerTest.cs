using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation;

namespace Philips.CodeAnalysis.Test.Maintainability.Documentation
{
	[TestClass]
	public class XmlDocumentationShouldAddValueAnalyzerTest : CodeFixVerifier
	{
		#region Non-Public Data Members

		private const string configuredAdditionalUselessWords = "dummy,roms";

		#endregion

		#region Non-Public Properties/Methods

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new XmlDocumentationShouldAddValueAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new XmlDocumentationCodeFixProvider();
		}

		protected override Dictionary<string, string> GetAdditionalAnalyzerConfigOptions()
		{
			Dictionary<string, string> options = new()
			{
				{ $@"dotnet_code_quality.{ Helper.ToDiagnosticId(DiagnosticIds.XmlDocumentationShouldAddValue) }.additional_useless_words", configuredAdditionalUselessWords  }
			};
			return options;
		}

		#endregion

		#region Public Interface
		[TestMethod]
		public void DefaultWhiteSpaceTest()
		{
			string content = $@"
/// <summary>
/// 
/// </summary>
public class Foo
{{
}}
";

			VerifyCSharpDiagnostic(content, DiagnosticResultHelper.CreateArray(DiagnosticIds.EmptyXmlComments));
		}

		[TestMethod]
		public void EmptyClassTest()
		{
			string content = $@"
/// <summary></summary>
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
	/// <summary></summary>
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
	/// <summary></summary>
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
	/// <summary></summary>
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
	/// <summary></summary>
	public event System.EventHandler Foo;
}}
";

			VerifyCSharpDiagnostic(content, DiagnosticResultHelper.CreateArray(DiagnosticIds.EmptyXmlComments));
		}

		[TestMethod]
		public void EmptyEnumTest()
		{
			string content = $@"
public enum TestEnumeration
{{
	/// <summary></summary>
	Foo = 1,
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
/// <summary>{text}</summary>
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
		[DataRow("Gets an dummy Foo", true)]
		[DataRow("Gets a rom", true)]
		[DataRow("Gets a roms", true)]
		[DataRow("Get an instance of Foo to please Bar", false)]
		[DataTestMethod]
		public void ValueAddMethodTests(string text, bool isError)
		{
			string content = $@"
public class TestClass
{{
	/// <summary>{text}</summary>
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
		[DataRow("Gets and sets the Foo", true)]
		[DataRow("Gets or sets the Foo", true)]
		[DataRow("Gets/sets the Foo", true)]
		[DataRow("Get an instance of Foo", true)]
		[DataRow("Gets an instance of Foo", true)]
		[DataRow("Get an instance of Foo to please Bar", false)]
		[DataTestMethod]
		public void ValueAddPropertyTests(string text, bool isError)
		{
			string content = $@"
public class TestClass
{{
	/// <summary>{text}</summary>
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
	/// <summary>{text}</summary>
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
	/// <summary>{text}</summary>
	public event System.EventHandler Foo;
}}
";

			VerifyCSharpDiagnostic(content, isError ? DiagnosticResultHelper.CreateArray(DiagnosticIds.XmlDocumentationShouldAddValue) : Array.Empty<DiagnosticResult>());
		}

		[DataRow("On Foo", true)]
		[DataRow("It is Foo", true)]
		[DataRow("When it is Foo", false)]
		[DataTestMethod]
		public void ValueAddEnumTests(string text, bool isError)
		{
			string content = $@"
/// <summary>{text}</summary>
public enum Foo
{{
	Member = 1,
}}
";

			VerifyCSharpDiagnostic(content, isError ? DiagnosticResultHelper.CreateArray(DiagnosticIds.XmlDocumentationShouldAddValue) : Array.Empty<DiagnosticResult>());
		}

		[DataRow("On Foo", true)]
		[DataRow("It is Foo", true)]
		[DataRow("When it is Foo", false)]
		[DataTestMethod]
		public void ValueAddEnumMemberTests(string text, bool isError)
		{
			string content = $@"
public enum TestEnumeration
{{
	/// <summary>{text}</summary>
	Foo = 1,
}}
";

			VerifyCSharpDiagnostic(content, isError ? DiagnosticResultHelper.CreateArray(DiagnosticIds.XmlDocumentationShouldAddValue) : Array.Empty<DiagnosticResult>());
		}

		[DataRow("event System.EventHandler Foo;")]
		[DataRow("void Foo() { }")]
		[DataRow("int field;")]
		[DataRow("int Property { get; }")]
		[DataTestMethod]
		public void CodeFixEmptyTests(string text)
		{
			string errorContent = $@"
public class TestClass
{{
	/// <summary>
	/// 
	/// </summary>
	public {text}
}}
";

			string fixedContent = $@"
public class TestClass
{{
  public {text}
}}
";

			VerifyCSharpDiagnostic(errorContent, DiagnosticResultHelper.CreateArray(DiagnosticIds.EmptyXmlComments));

			VerifyCSharpFix(errorContent, fixedContent);
		}

		[DataRow("event System.EventHandler Foo;")]
		[DataRow("void Foo() { }")]
		[DataRow("int field;")]
		[DataRow("int Property { get; }")]
		[DataTestMethod]
		public void CodeFixValueTests(string text)
		{
			string errorContent = $@"
public class TestClass
{{
	/// <summary>raise the.</summary>
	public {text}
}}
";

			string fixedContent = $@"
public class TestClass
{{
  public {text}
}}
";

			VerifyCSharpDiagnostic(errorContent, DiagnosticResultHelper.CreateArray(DiagnosticIds.XmlDocumentationShouldAddValue));

			VerifyCSharpFix(errorContent, fixedContent);
		}

		[TestMethod]
		public void InheritDocTests()
		{
			string content = $@"
public class TestClass
{{
	/// <inheritdoc />
	public override string ToString() 
	{{
		return ""Some string"";
	}}
}}
";

			VerifyCSharpDiagnostic(content, Array.Empty<DiagnosticResult>());
		}

		[TestMethod]
		public void RemarksOnlyTests()
		{
			string content = $@"
using System;

public class TestClass
{{
	/// <remarks>Random remarks</remarks>
	public override string ToString() 
	{{
		return ""Some string"";
	}}
}}
";

			VerifyCSharpDiagnostic(content, Array.Empty<DiagnosticResult>());
		}

		[TestMethod]
		public void ConstructorTest()
		{
			string content = $@"
public class Foo
{{
	/// <summary>
	/// Constructor
	/// </summary>
	public Foo()
	{{
	}}
}}
";

			VerifyCSharpDiagnostic(content, DiagnosticResultHelper.CreateArray(DiagnosticIds.XmlDocumentationShouldAddValue));
		}
		#endregion
	}
}
