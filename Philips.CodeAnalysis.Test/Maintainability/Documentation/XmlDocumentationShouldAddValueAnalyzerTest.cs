// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.
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
		private const string configuredAdditionalUselessWords = "dummy,roms";

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

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task DefaultWhiteSpaceTestAsync()
		{
			var content = $@"
/// <summary>
/// 
/// </summary>
public class Foo
{{
}}
";

			var newContent = $@"
public class Foo
{{
}}
";

			await VerifyDiagnostic(content, DiagnosticId.EmptyXmlComments).ConfigureAwait(false);
			await VerifyFix(content, newContent);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task EmptyClassTestAsync()
		{
			var content = $@"
/// <summary></summary>
public class Foo
{{
}}
";
			var newContent = $@"
public class Foo
{{
}}
";

			await VerifyDiagnostic(content, DiagnosticId.EmptyXmlComments).ConfigureAwait(false);
			await VerifyFix(content, newContent);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task EmptyMethodTestAsync()
		{
			var content = $@"
public class TestClass
{{
	/// <summary></summary>
	public void Foo()
	{{
	}}
}}
";
			var newContent = $@"
public class TestClass
{{
	public void Foo()
	{{
	}}
}}
";

			await VerifyDiagnostic(content, DiagnosticId.EmptyXmlComments).ConfigureAwait(false);
			await VerifyFix(content, newContent);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task EmptyPropertyTestAsync()
		{
			var content = $@"
public class TestClass
{{
	/// <summary></summary>
	public int Foo {{ get; }}
}}
";
			var newContent = $@"
public class TestClass
{{
	public int Foo {{ get; }}
}}
";

			await VerifyDiagnostic(content, DiagnosticId.EmptyXmlComments).ConfigureAwait(false);
			await VerifyFix(content, newContent);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task EmptyFieldTestAsync()
		{
			var content = $@"
public class TestClass
{{
	/// <summary></summary>
	public int Foo;
}}
";
			var newContent = $@"
public class TestClass
{{
	public int Foo;
}}
";

			await VerifyDiagnostic(content, DiagnosticId.EmptyXmlComments).ConfigureAwait(false);
			await VerifyFix(content, newContent);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task EmptyEventTestAsync()
		{
			var content = $@"
public class TestClass
{{
	/// <summary></summary>
	public event System.EventHandler Foo;
}}
";

			await VerifyDiagnostic(content, DiagnosticId.EmptyXmlComments).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task EmptyEnumTestAsync()
		{
			var content = $@"
public enum TestEnumeration
{{
	/// <summary></summary>
	Foo = 1,
}}
";

			await VerifyDiagnostic(content, DiagnosticId.EmptyXmlComments).ConfigureAwait(false);
		}

		[DataRow("foo")]
		[DataRow("Gets Foo")]
		[DataRow("Gets the Foo")]
		[DataRow("Get an instance of Foo")]
		[DataRow("Gets an instance of Foo")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ValueAddClassInvalidTestsAsync(string text)
		{
			var content = $@"
/// <summary>{text}</summary>
public class Foo
{{
}}
";
			var newContent = $@"
public class Foo
{{
}}
";

			await VerifyDiagnostic(content, DiagnosticId.XmlDocumentationShouldAddValue).ConfigureAwait(false);
			await VerifyFix(content, newContent);
		}

		[DataRow("Get an instance of Foo to please Bar")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ValueAddClassValidTestsAsync(string text)
		{
			var content = $@"
/// <summary>{text}</summary>
public class Foo
{{
}}
";
			await VerifySuccessfulCompilation(content).ConfigureAwait(false);
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
		public async Task ValueAddMethodInvalidTestsAsync(string text)
		{
			var content = $@"
public class TestClass
{{
	/// <summary>{text}</summary>
	public void Foo()
	{{
	}}
}}
";

			await VerifyDiagnostic(content, DiagnosticId.XmlDocumentationShouldAddValue).ConfigureAwait(false);
		}

		[DataRow("Get an instance of Foo to please Bar")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ValueAddMethodValidTestsAsync(string text)
		{
			var content = $@"
public class TestClass
{{
	/// <summary>{text}</summary>
	public void Foo()
	{{
	}}
}}
";
			await VerifySuccessfulCompilation(content).ConfigureAwait(false);
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
		public async Task ValueAddPropertyInvalidTestsAsync(string text)
		{
			var content = $@"
public class TestClass
{{
	/// <summary>{text}</summary>
	public int Foo
	{{
		get;
	}}
}}
";

			await VerifyDiagnostic(content, DiagnosticId.XmlDocumentationShouldAddValue).ConfigureAwait(false);
		}

		[DataRow("Get an instance of Foo to please Bar")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ValueAddPropertyValidTestsAsync(string text)
		{
			var content = $@"
public class TestClass
{{
	/// <summary>{text}</summary>
	public int Foo
	{{
		get;
	}}
}}
";

			await VerifySuccessfulCompilation(content).ConfigureAwait(false);
		}



		[DataRow("foo")]
		[DataRow("Gets Foo")]
		[DataRow("Gets the Foo")]
		[DataRow("Get an instance of Foo")]
		[DataRow("Gets an instance of Foo")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ValueAddFieldInvalidTestsAsync(string text)
		{
			var content = $@"
public class TestClass
{{
	/// <summary>{text}</summary>
	public int Foo;
}}
";

			await VerifyDiagnostic(content, DiagnosticId.XmlDocumentationShouldAddValue).ConfigureAwait(false);
		}

		[DataRow("Get an instance of Foo to please Bar")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ValueAddFieldValidTestsAsync(string text)
		{
			var content = $@"
public class TestClass
{{
	/// <summary>{text}</summary>
	public int Foo;
}}
";
			await VerifySuccessfulCompilation(content).ConfigureAwait(false);
		}


		[DataRow("On Foo")]
		[DataRow("Fire Foo")]
		[DataRow("Fires the Foo")]
		[DataRow("Fires the Foo event")]
		[DataRow("Raise Foo")]
		[DataRow("Raises an instance of the Foo event")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ValueAddEventInvalidTestsAsync(string text)
		{
			var content = $@"
public class TestClass
{{
	/// <summary>{text}</summary>
	public event System.EventHandler Foo;
}}
";

			await VerifyDiagnostic(content, DiagnosticId.XmlDocumentationShouldAddValue).ConfigureAwait(false);
		}

		[DataRow("Raised when Foo happens")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ValueAddEventValidTestsAsync(string text)
		{
			var content = $@"
public class TestClass
{{
	/// <summary>{text}</summary>
	public event System.EventHandler Foo;
}}
";
			await VerifySuccessfulCompilation(content).ConfigureAwait(false);
		}

		[DataRow("On Foo")]
		[DataRow("It is Foo")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ValueAddEnumTestsAsync(string text)
		{
			var content = $@"
/// <summary>{text}</summary>
public enum Foo
{{
	Member = 1,
}}
";

			await VerifyDiagnostic(content, DiagnosticId.XmlDocumentationShouldAddValue).ConfigureAwait(false);
		}

		[DataRow("When it is Foo")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ValueAddEnumValidTestsAsync(string text)
		{
			var content = $@"
/// <summary>{text}</summary>
public enum Foo
{{
	Member = 1,
}}
";
			await VerifySuccessfulCompilation(content).ConfigureAwait(false);
		}


		[DataRow("On Foo")]
		[DataRow("It is Foo")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ValueAddEnumMemberInvalidTestsAsync(string text)
		{
			var content = $@"
public enum TestEnumeration
{{
	/// <summary>{text}</summary>
	Foo = 1,
}}
";
			await VerifyDiagnostic(content, DiagnosticId.XmlDocumentationShouldAddValue).ConfigureAwait(false);
		}

		[DataRow("When it is Foo")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ValueAddEnumMemberValidTestsAsync(string text)
		{
			var content = $@"
public enum TestEnumeration
{{
	/// <summary>{text}</summary>
	Foo = 1,
}}
";
			await VerifySuccessfulCompilation(content).ConfigureAwait(false);
		}


		[DataRow("event System.EventHandler Foo;")]
		[DataRow("void Foo() { }")]
		[DataRow("int field;")]
		[DataRow("int Property { get; }")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task CodeFixEmptyTests(string text)
		{
			var errorContent = $@"
public class TestClass
{{
	/// <summary>
	/// 
	/// </summary>
	public {text}
}}
";

			var fixedContent = $@"
public class TestClass
{{
  public {text}
}}
";

			await VerifyDiagnostic(errorContent, DiagnosticId.EmptyXmlComments).ConfigureAwait(false);
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
			var errorContent = $@"
public class TestClass
{{
	/// <summary>raise the.</summary>
	public {text}
}}
";

			var fixedContent = $@"
public class TestClass
{{
  public {text}
}}
";

			await VerifyDiagnostic(errorContent, DiagnosticId.XmlDocumentationShouldAddValue).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task InheritDocTestsAsync()
		{
			var content = $@"
public class TestClass
{{
	/// <inheritdoc />
	public override string ToString() 
	{{
		return ""Some string"";
	}}
}}
";

			await VerifySuccessfulCompilation(content).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task RemarksOnlyTestsAsync()
		{
			var content = $@"
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

			await VerifySuccessfulCompilation(content).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ConstructorTestAsync()
		{
			var content = $@"
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

			await VerifyDiagnostic(content, DiagnosticId.XmlDocumentationShouldAddValue).ConfigureAwait(false);
		}
	}
}
