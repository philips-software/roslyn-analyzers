// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

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
		[TestCategory(TestDefinitions.UnitTests)]
		public void NegativeField()
		{
			const string template = @"
using System;
class Foo
{
	private bool ignoreWhitespace;
}
";

			VerifyDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticId.PositiveNaming));
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void PositiveField()
		{
			const string template = @"
using System;
class Foo
{
	private bool enableFeature;
}
";

			VerifySuccessfulCompilation(template);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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

			VerifyDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticId.PositiveNaming));
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void NegativeProperty()
		{
			const string template = @"
using System;
class Foo
{
	public bool featureMissing { get; }
}
";

			VerifyDiagnostic(template, DiagnosticResultHelper.Create(DiagnosticId.PositiveNaming));
		}
	}
}