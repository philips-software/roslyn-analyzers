// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming;

namespace Philips.CodeAnalysis.Test.Maintainability.Naming
{
	[TestClass]
	public class PositiveNamingAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new PositiveNamingAnalyzer();
		}

		[TestMethod]
		public void NegativeField()
		{
			const string template = @"
using System;
class Foo
{
	private bool ignoreWhitespace;
}
";

			VerifyDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.PositiveNaming));
		}

		[TestMethod]
		public void PositiveField()
		{
			const string template = @"
using System;
class Foo
{
	private bool enableFeature;
}
";

			VerifyDiagnostic(template);
		}

		[TestMethod]
		public void NegativeLocalVariable()
		{
			const string template = @"
using System;
class Foo
{
	public void Test(int a)
	{
		bool disableFeature;
	}
}
";

			VerifyDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.PositiveNaming));
		}

		[TestMethod]
		public void NegativeProperty()
		{
			const string template = @"
using System;
class Foo
{
	public bool featureMissing { get; }
}
";

			VerifyDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.PositiveNaming));
		}
	}
}