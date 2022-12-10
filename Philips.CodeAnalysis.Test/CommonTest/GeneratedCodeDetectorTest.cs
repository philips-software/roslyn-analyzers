// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;

namespace Philips.CodeAnalysis.Test.CommonTest
{
	[TestClass]
	public class GeneratedCodeDetectorTest : DiagnosticVerifier
	{
		private const string MyTestAttribute = @"MyTestAttribute";
		private const string MyFullTestAttribute = @"MyNamespace.MyTestAttribute";

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new AvoidRedundantSwitchStatementAnalyzer(new GeneratedCodeDetector(MyTestAttribute, MyFullTestAttribute));
		}

		[DataRow(@"Foo.Designer.cs")]
		[DataRow(@"Foo.designer.cs")]
		[DataRow(@"Foo.g.cs")]
		[DataTestMethod]
		public void IsGeneratedCodeTest(string text)
		{
		}

		[DataRow(@"Foo.DesigXner.cs")]
		[DataRow(@"Foo.cs")]
		[DataRow(@"Foo.x.cs")]
		[DataTestMethod]
		public void IsNotGeneratedCodeTest(string text)
		{
		}
	}
}
