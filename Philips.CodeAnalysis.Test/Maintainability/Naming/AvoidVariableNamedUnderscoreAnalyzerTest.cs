// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Naming
{
	[TestClass]
	public class AvoidVariableNamedUnderscoreAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidVariableNamedUnderscoreAnalyzer();
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task LocalVariableNamedUnderscoreShouldFlag()
		{
			var test = @"
using System;

class TestClass
{
	public void TestMethod()
	{
		byte[] _ = new byte[10];
	}
}";

			await VerifyDiagnostic(test, DiagnosticId.AvoidVariableNamedUnderscore).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ForEachVariableNamedUnderscoreShouldFlag()
		{
			var test = @"
using System;
using System.Collections.Generic;

class TestClass
{
	public void TestMethod()
	{
		var items = new List<int> { 1, 2, 3 };
		foreach (var _ in items)
		{
		}
	}
}";

			await VerifyDiagnostic(test, DiagnosticId.AvoidVariableNamedUnderscore).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task OutParameterNamedUnderscoreShouldFlag()
		{
			var test = @"
using System;

class TestClass
{
	public void TestMethod()
	{
		TestHelper(out int _);
	}

	private void TestHelper(out int value)
	{
		value = 42;
	}
}";

			await VerifyDiagnostic(test, DiagnosticId.AvoidVariableNamedUnderscore).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ForLoopVariableNamedUnderscoreShouldFlag()
		{
			var test = @"
using System;

class TestClass
{
	public void TestMethod()
	{
		for (int _ = 0; _ < 10; _++)
		{
		}
	}
}";

			await VerifyDiagnostic(test, DiagnosticId.AvoidVariableNamedUnderscore).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task UsingStatementVariableNamedUnderscoreShouldFlag()
		{
			var test = @"
using System;
using System.IO;

class TestClass
{
	public void TestMethod()
	{
		using (var _ = new MemoryStream())
		{
		}
	}
}";

			await VerifyDiagnostic(test, DiagnosticId.AvoidVariableNamedUnderscore).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ValidVariableNamesShouldNotFlag()
		{
			var test = @"
using System;

class TestClass
{
	public void TestMethod()
	{
		byte[] data = new byte[10];
		var result = 42;
		int count = 0;
	}
}";

			await VerifySuccessfulCompilation(test).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task VariableStartingWithUnderscoreShouldNotFlag()
		{
			var test = @"
using System;

class TestClass
{
	public void TestMethod()
	{
		byte[] _data = new byte[10];
		var _result = 42;
		int _count = 0;
	}
}";

			await VerifySuccessfulCompilation(test).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task FieldVariableNamedUnderscoreShouldNotFlag()
		{
			var test = @"
using System;

class TestClass
{
	private byte[] _ = new byte[10];
}";

			await VerifySuccessfulCompilation(test).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task InParameterNamedUnderscoreShouldNotFlag()
		{
			var test = @"
using System;

class TestClass
{
	public void TestMethod()
	{
		TestHelper(1);
	}

	private void TestHelper(int _)
	{
	}
}";

			await VerifySuccessfulCompilation(test).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task ProperDiscardShouldNotFlag()
		{
			var test = @"
using System;
using System.Collections.Generic;

class TestClass
{
	public void TestMethod()
	{
		var dictionary = new Dictionary<string, int>();
		dictionary.TryGetValue(""key"", out _);  // This is a proper discard, should not be flagged
		
		// Other valid discard patterns  
		TryParseHelper(""123"", out _);
		GetValue(out _);
	}

	private bool TryParseHelper(string input, out int result)
	{
		result = 42;
		return true;
	}
	
	private void GetValue(out string value)
	{
		value = ""test"";
	}
}";

			await VerifySuccessfulCompilation(test).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task TypedDiscardShouldNotFlag()
		{
			var test = @"
using System;
using System.Net;

class TestClass
{
	public void TestMethod()
	{
		string adapterToFind = ""eth0"";
		MyNetwork myNetwork = new MyNetwork();
		
		// This is a typed discard used for overload resolution - should not be flagged
		myNetwork.GetIpV4(adapterToFind, out IPAddress addr, out _, out string _);
	}
}

class MyNetwork
{
	public void GetIpV4(string adapter, out IPAddress addr, out int mask, out string gateway)
	{
		addr = IPAddress.Parse(""192.168.1.1"");
		mask = 24;
		gateway = ""192.168.1.1"";
	}
	
	public void GetIpV4(string adapter, out IPAddress addr, out int mask, out byte[] gateway)
	{
		addr = IPAddress.Parse(""192.168.1.1"");
		mask = 24;
		gateway = new byte[] { 192, 168, 1, 1 };
	}
}";

			await VerifySuccessfulCompilation(test).ConfigureAwait(false);
		}
	}
}
