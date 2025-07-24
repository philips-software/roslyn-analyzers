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
	public class AvoidStringJoinInInterpolatedStringAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidStringJoinInInterpolatedStringAnalyzer();
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task StringJoinInInterpolatedStringTriggersWarning()
		{
			var code = @"
using System;
using System.Collections.Generic;

class Test
{
	public void Method()
	{
		var items = new List<string> { ""a"", ""b"", ""c"" };
		var result = $""Items: {string.Join("", "", items)}"";
	}
}
";
			await VerifyDiagnostic(code).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task StringJoinWithEnvironmentNewLineTriggersWarning()
		{
			var code = @"
using System;
using System.Collections.Generic;

class Test
{
	public string CommandType { get; set; }
	public List<string> ReasonList { get; set; }
	
	public string Method()
	{
		return $@""{CommandType}  Reasons:{string.Join(Environment.NewLine, ReasonList)}"";
	}
}
";
			await VerifyDiagnostic(code).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task InterpolatedStringWithoutStringJoinIsOk()
		{
			var code = @"
using System;
using System.Collections.Generic;

class Test
{
	public void Method()
	{
		var items = new List<string> { ""a"", ""b"", ""c"" };
		var result = $""Items: {items.Count}"";
	}
}
";
			await VerifySuccessfulCompilation(code).ConfigureAwait(false);
		}
	}
}
