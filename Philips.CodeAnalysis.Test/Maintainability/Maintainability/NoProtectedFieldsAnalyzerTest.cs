// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class NoProtectedFieldsAnalyzerTest : CodeFixVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new NoProtectedFieldsAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new NoProtectedFieldsCodeFixProvider();
		}

		[DataRow("protected", true)]
		[DataRow("public", false)]
		[DataRow("private", false)]
		[DataRow("internal", false)]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ProtectedFieldsRaiseErrorAsync(string modifiers, bool isError)
		{
			const string template = @"""
class Foo {{ {0} string _foo; }}
""";
			var code = string.Format(template, modifiers);
			if (isError)
			{
				await VerifyDiagnostic(code).ConfigureAwait(false);
			}
			else
			{
				await VerifySuccessfulCompilation(code).ConfigureAwait(false);
			}
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ProtectedFieldCodeFixSingleFieldAsync()
		{
			const string before = @"""
class Foo 
{ 
	protected string _name; 
}
""";
			const string after = @"""
class Foo 
{ 
	protected string Name { get; private set; } 
}
""";

			await VerifyDiagnostic(before).ConfigureAwait(false);
			await VerifyFix(before, after).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ProtectedFieldCodeFixMultipleFieldsAsync()
		{
			const string before = @"""
class Foo 
{ 
	protected string _name, _value; 
}
""";
			const string after = @"""
class Foo 
{ 
	protected string Name { get; private set; }
	protected string Value { get; private set; } 
}
""";

			await VerifyDiagnostic(before).ConfigureAwait(false);
			await VerifyFix(before, after).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ProtectedFieldCodeFixWithoutUnderscoreAsync()
		{
			const string before = @"""
class Foo 
{ 
	protected string name; 
}
""";
			const string after = @"""
class Foo 
{ 
	protected string Name { get; private set; } 
}
""";

			await VerifyDiagnostic(before).ConfigureAwait(false);
			await VerifyFix(before, after).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ProtectedFieldCodeFixPreservesTypeAsync()
		{
			const string before = @"""
class Foo 
{ 
	protected int _counter; 
}
""";
			const string after = @"""
class Foo 
{ 
	protected int Counter { get; private set; } 
}
""";

			await VerifyDiagnostic(before).ConfigureAwait(false);
			await VerifyFix(before, after).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ProtectedFieldCodeFixComplexTypeAsync()
		{
			const string before = @"""
class Foo 
{ 
	protected List<string> _items; 
}
""";
			const string after = @"""
class Foo 
{ 
	protected List<string> Items { get; private set; } 
}
""";

			await VerifyDiagnostic(before).ConfigureAwait(false);
			await VerifyFix(before, after).ConfigureAwait(false);
		}
	}
}
