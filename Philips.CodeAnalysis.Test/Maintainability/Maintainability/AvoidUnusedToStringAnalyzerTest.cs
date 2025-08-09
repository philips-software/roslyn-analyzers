// © 2024 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidUnusedToStringAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidUnusedToStringAnalyzer();
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidUnusedToStringAssignedToDiscard()
		{
			var test = @"
class TestClass
{
	public void TestMethod()
	{
		var node = ""test"";
		_ = node.ToString();
	}
}";

			await VerifyDiagnostic(test).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidUnusedToStringStandaloneExpression()
		{
			var test = @"
class TestClass
{
	public void TestMethod()
	{
		var node = ""test"";
		node.ToString();
	}
}";

			await VerifyDiagnostic(test).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidUnusedToStringComplexExpression()
		{
			var test = @"
class TestClass
{
	public void TestMethod()
	{
		var obj = new object();
		_ = obj.GetType().Name.ToString();
	}
}";

			await VerifyDiagnostic(test).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AllowToStringWhenAssignedToVariable()
		{
			var test = @"
class TestClass
{
	public void TestMethod()
	{
		var node = ""test"";
		var result = node.ToString();
		System.Console.WriteLine(result);
	}
}";

			await VerifySuccessfulCompilation(test).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AllowToStringWhenUsedInExpression()
		{
			var test = @"
class TestClass
{
	public void TestMethod()
	{
		var node = ""test"";
		System.Console.WriteLine(node.ToString());
	}
}";

			await VerifySuccessfulCompilation(test).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AllowToStringWhenReturned()
		{
			var test = @"
class TestClass
{
	public string TestMethod()
	{
		var node = ""test"";
		return node.ToString();
	}
}";

			await VerifySuccessfulCompilation(test).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AllowToStringWithParameters()
		{
			var test = @"
class TestClass
{
	public void TestMethod()
	{
		var number = 42;
		_ = number.ToString(""X"");
	}
}";

			await VerifySuccessfulCompilation(test).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidUnusedToStringChainedExpression()
		{
			var test = @"
class TestClass
{
	public string GetText() => ""test"";
	
	public void TestMethod()
	{
		GetText().ToString();
	}
}";

			await VerifyDiagnostic(test).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AllowToStringWhenUsedAsParameter()
		{
			var test = @"
class TestClass
{
	public void TestMethod()
	{
		var node = ""test"";
		SomeMethod(node.ToString());
	}
	
	private void SomeMethod(string value) { }
}";

			await VerifySuccessfulCompilation(test).ConfigureAwait(false);
		}
	}
}
