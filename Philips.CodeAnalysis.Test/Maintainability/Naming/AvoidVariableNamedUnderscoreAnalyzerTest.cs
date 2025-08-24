// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
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

		private const string CommonTestMethods = @"
	private void GetValue(out string value)
	{
		value = ""test"";
	}
	
	private bool TryParseHelper(string input, out int result)
	{
		result = 42;
		return true;
	}
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task UnnecessaryTypedDiscardShouldFlag()
		{
			var test = @"
using System;

internal class TestClass
	{
		public void TestMethod()
		{
			// Typed discard when anonymous discard would work
			GetValue(out string _);
			TryParseHelper(""123"", out int _);
		}" + CommonTestMethods + @"
}";

			private DiagnosticResult[] expected = new[]
			{
				new DiagnosticResult()
				{
					Id = DiagnosticId.AvoidUnnecessaryTypedDiscard.ToId(),
					Location = new DiagnosticResultLocation("Test0.cs", 9, 23),
					Message = new System.Text.RegularExpressions.Regex(".*"),
					Severity = DiagnosticSeverity.Error,
				},
				new DiagnosticResult()
				{
					Id = DiagnosticId.AvoidUnnecessaryTypedDiscard.ToId(),
					Location = new DiagnosticResultLocation("Test0.cs", 10, 33),
					Message = new System.Text.RegularExpressions.Regex(".*"),
					Severity = DiagnosticSeverity.Error,
				}
			};

	private await VerifyDiagnostic(test, expected).private ConfigureAwait(false);
}

[TestMethod]
[TestCategory(TestDefinitions.UnitTests)]
public static async Task NecessaryTypedDiscardForOverloadResolutionShouldNotFlag()
{
	var test = @"
using System;

class TestClass
{
	public void TestMethod()
	{
		// These typed discards are needed for overload resolution
		Parse(""123"", out int _);    // Disambiguates from Parse(string, out string)
		Parse(""test"", out string _); // Disambiguates from Parse(string, out int)
	}

	private bool Parse(string input, out int result)
	{
		result = 42;
		return true;
	}
	
	private bool Parse(string input, out string result)
	{
		result = input;
		return true;
	}
}";

	await VerifySuccessfulCompilation(test).ConfigureAwait(false);
}

[TestMethod]
[TestCategory(TestDefinitions.UnitTests)]
public static async Task UnnecessaryTypedDiscardWithNamedArgumentsShouldFlag()
{
	var test = @"
using System;

class TestClass
{
	public void TestMethod()
	{
		// Typed discard with named argument when anonymous discard would work
		GetValue(value: out string _);
		TryParseHelper(input: ""123"", result: out int _);
	}" + CommonTestMethods + @"
}";

	DiagnosticResult[] expected = new[]
	{
				new DiagnosticResult()
				{
					Id = DiagnosticId.AvoidUnnecessaryTypedDiscard.ToId(),
					Location = new DiagnosticResultLocation("Test0.cs", 9, 32),
					Message = new System.Text.RegularExpressions.Regex(".*"),
					Severity = DiagnosticSeverity.Error,
				},
				new DiagnosticResult()
				{
					Id = DiagnosticId.AvoidUnnecessaryTypedDiscard.ToId(),
					Location = new DiagnosticResultLocation("Test0.cs", 10, 50),
					Message = new System.Text.RegularExpressions.Regex(".*"),
					Severity = DiagnosticSeverity.Error,
				}
			};

	await VerifyDiagnostic(test, expected).ConfigureAwait(false);
}

[TestMethod]
[TestCategory(TestDefinitions.UnitTests)]
public static async Task NecessaryTypedDiscardWithNamedArgumentsForOverloadResolutionShouldNotFlag()
{
	var test = @"
using System;

class TestClass
{
	public void TestMethod()
	{
		// These typed discards with named arguments are needed for overload resolution
		Parse(input: ""123"", result: out int _);    // Disambiguates from Parse(string, out string)
		Parse(input: ""test"", result: out string _); // Disambiguates from Parse(string, out int)
	}

	private bool Parse(string input, out int result)
	{
		result = 42;
		return true;
	}
	
	private bool Parse(string input, out string result)
	{
		result = input;
		return true;
	}
}";

	await VerifySuccessfulCompilation(test).ConfigureAwait(false);
}
	}
}
