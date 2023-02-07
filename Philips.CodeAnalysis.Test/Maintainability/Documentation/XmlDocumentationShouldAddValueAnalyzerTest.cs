// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

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

		protected override ImmutableDictionary<string, string> GetAdditionalAnalyzerConfigOptions()
		{
			return base.GetAdditionalAnalyzerConfigOptions().Add($@"dotnet_code_quality.{Helper.ToDiagnosticId(DiagnosticId.XmlDocumentationShouldAddValue)}.additional_useless_words", configuredAdditionalUselessWords);
		}

		#endregion

		#region Public Interface
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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

			VerifyDiagnostic(content, DiagnosticId.EmptyXmlComments);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void EmptyClassTest()
		{
			string content = $@"
/// <summary></summary>
public class Foo
{{
}}
";

			VerifyDiagnostic(content, DiagnosticId.EmptyXmlComments);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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

			VerifyDiagnostic(content, DiagnosticId.EmptyXmlComments);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void EmptyPropertyTest()
		{
			string content = $@"
public class TestClass
{{
	/// <summary></summary>
	public int Foo {{ get; }}
}}
";

			VerifyDiagnostic(content, DiagnosticId.EmptyXmlComments);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void EmptyFieldTest()
		{
			string content = $@"
public class TestClass
{{
	/// <summary></summary>
	public int Foo;
}}
";

			VerifyDiagnostic(content, DiagnosticId.EmptyXmlComments);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void EmptyEventTest()
		{
			string content = $@"
public class TestClass
{{
	/// <summary></summary>
	public event System.EventHandler Foo;
}}
";

			VerifyDiagnostic(content, DiagnosticId.EmptyXmlComments);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void EmptyEnumTest()
		{
			string content = $@"
public enum TestEnumeration
{{
	/// <summary></summary>
	Foo = 1,
}}
";

			VerifyDiagnostic(content, DiagnosticId.EmptyXmlComments);
		}

		[DataRow("foo")]
		[DataRow("Gets Foo")]
		[DataRow("Gets the Foo")]
		[DataRow("Get an instance of Foo")]
		[DataRow("Gets an instance of Foo")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ValueAddClassInvalidTests(string text)
		{
			string content = $@"
/// <summary>{text}</summary>
public class Foo
{{
}}
";

			VerifyDiagnostic(content, DiagnosticId.XmlDocumentationShouldAddValue);
		}

		[DataRow("Get an instance of Foo to please Bar")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ValueAddClassValidTests(string text)
		{
			string content = $@"
/// <summary>{text}</summary>
public class Foo
{{
}}
";
			VerifySuccessfulCompilation(content);
		}


		[DataRow("foo")]
		[DataRow("Gets Foo")]
		[DataRow("Gets the Foo")]
		[DataRow("Get an instance of Foo")]
		[DataRow("Gets an instance of Foo")]
		[DataRow("Gets an dummy Foo")]
		[DataRow("Gets a rom")]
		[DataRow("Gets a roms")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ValueAddMethodInvalidTests(string text)
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

			VerifyDiagnostic(content, DiagnosticId.XmlDocumentationShouldAddValue);
		}

		[DataRow("Get an instance of Foo to please Bar")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ValueAddMethodValidTests(string text)
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
			VerifySuccessfulCompilation(content);
		}

		[DataRow("foo")]
		[DataRow("Gets Foo")]
		[DataRow("Gets the Foo")]
		[DataRow("Gets and sets the Foo")]
		[DataRow("Gets or sets the Foo")]
		[DataRow("Gets/sets the Foo")]
		[DataRow("Get an instance of Foo")]
		[DataRow("Gets an instance of Foo")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ValueAddPropertyInvalidTests(string text)
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

			VerifyDiagnostic(content, DiagnosticId.XmlDocumentationShouldAddValue);
		}

		[DataRow("Get an instance of Foo to please Bar")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ValueAddPropertyValidTests(string text)
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

			VerifySuccessfulCompilation(content);
		}



		[DataRow("foo")]
		[DataRow("Gets Foo")]
		[DataRow("Gets the Foo")]
		[DataRow("Get an instance of Foo")]
		[DataRow("Gets an instance of Foo")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ValueAddFieldInvalidTests(string text)
		{
			string content = $@"
public class TestClass
{{
	/// <summary>{text}</summary>
	public int Foo;
}}
";

			VerifyDiagnostic(content, DiagnosticId.XmlDocumentationShouldAddValue);
		}

		[DataRow("Get an instance of Foo to please Bar")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ValueAddFieldValidTests(string text)
		{
			string content = $@"
public class TestClass
{{
	/// <summary>{text}</summary>
	public int Foo;
}}
";
			VerifySuccessfulCompilation(content);
		}


		[DataRow("On Foo")]
		[DataRow("Fire Foo")]
		[DataRow("Fires the Foo")]
		[DataRow("Fires the Foo event")]
		[DataRow("Raise Foo")]
		[DataRow("Raises an instance of the Foo event")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ValueAddEventInvalidTests(string text)
		{
			string content = $@"
public class TestClass
{{
	/// <summary>{text}</summary>
	public event System.EventHandler Foo;
}}
";

			VerifyDiagnostic(content, DiagnosticId.XmlDocumentationShouldAddValue);
		}

		[DataRow("Raised when Foo happens")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ValueAddEventValidTests(string text)
		{
			string content = $@"
public class TestClass
{{
	/// <summary>{text}</summary>
	public event System.EventHandler Foo;
}}
";
			VerifySuccessfulCompilation(content);
		}

		[DataRow("On Foo")]
		[DataRow("It is Foo")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ValueAddEnumTests(string text)
		{
			string content = $@"
/// <summary>{text}</summary>
public enum Foo
{{
	Member = 1,
}}
";

			VerifyDiagnostic(content, DiagnosticId.XmlDocumentationShouldAddValue);
		}

		[DataRow("When it is Foo")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ValueAddEnumValidTests(string text)
		{
			string content = $@"
/// <summary>{text}</summary>
public enum Foo
{{
	Member = 1,
}}
";
			VerifySuccessfulCompilation(content);
		}


		[DataRow("On Foo")]
		[DataRow("It is Foo")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ValueAddEnumMemberInvalidTests(string text)
		{
			string content = $@"
public enum TestEnumeration
{{
	/// <summary>{text}</summary>
	Foo = 1,
}}
";
			VerifyDiagnostic(content, DiagnosticId.XmlDocumentationShouldAddValue);
		}

		[DataRow("When it is Foo")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ValueAddEnumMemberValidTests(string text)
		{
			string content = $@"
public enum TestEnumeration
{{
	/// <summary>{text}</summary>
	Foo = 1,
}}
";
			VerifySuccessfulCompilation(content);
		}


		[DataRow("event System.EventHandler Foo;")]
		[DataRow("void Foo() { }")]
		[DataRow("int field;")]
		[DataRow("int Property { get; }")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CodeFixEmptyTests(string text)
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

			VerifyDiagnostic(errorContent, DiagnosticId.EmptyXmlComments);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[DataRow("event System.EventHandler Foo;")]
		[DataRow("void Foo() { }")]
		[DataRow("int field;")]
		[DataRow("int Property { get; }")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CodeFixValueTests(string text)
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

			VerifyDiagnostic(errorContent, DiagnosticId.XmlDocumentationShouldAddValue);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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

			VerifySuccessfulCompilation(content);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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

			VerifySuccessfulCompilation(content);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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

			VerifyDiagnostic(content, DiagnosticId.XmlDocumentationShouldAddValue);
		}
		#endregion
	}
}
