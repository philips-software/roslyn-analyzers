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
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
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

			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.PositiveNaming));
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

			VerifyCSharpDiagnostic(template);
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

			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.PositiveNaming));
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

			VerifyCSharpDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticIds.PositiveNaming));
		}
	}
}