// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;

namespace Philips.CodeAnalysis.Test
{
	[TestClass]
	public class AvoidAttributeAnalyzerTest : DiagnosticVerifier
	{
		private const string allowedMethodName = @"Foo.AllowedInitializer()
Foo.AllowedInitializer(Bar)
";

		protected override (string name, string content)[] GetAdditionalTexts()
		{
			return new[] { ("NotFile.txt", "data"), (AvoidAttributeAnalyzer.AvoidAttributesWhitelist, allowedMethodName) };
		}

		[DataTestMethod]
		[DataRow(@"[TestMethod, Ignore]", 16)]
		[DataRow(@"[Ignore]", 4)]
		public void AvoidIgnoreAttributeTest(string test, int expectedColumn)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo 
{{
  {0}
  public void Foo()
  {{
  }}
}}
";
			string givenText = string.Format(baseline, test);

			DiagnosticResult expected = new DiagnosticResult
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.AvoidIgnoreAttribute),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 5, expectedColumn)
				}
			};

			VerifyCSharpDiagnostic(givenText, expected);
		}


		[DataTestMethod]
		[DataRow(@"[TestMethod, Owner(""MK"")]", 16)]
		[DataRow(@"[Owner(""MK"")]", 4)]
		[DataRow(@"[TestMethod][Owner(""MK"")]", 16)]
		public void AvoidOwnerAttributeTest(string test, int expectedColumn)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo 
{{
  {0}
  public void Foo()
  {{
  }}
}}
";
			string givenText = string.Format(baseline, test);

			DiagnosticResult expected = new DiagnosticResult
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.AvoidOwnerAttribute),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 5, expectedColumn)
				}
			};

			VerifyCSharpDiagnostic(givenText, expected);
		}

		[DataTestMethod]
		[DataRow(@"[TestInitialize]", 4)]
		public void AvoidTestInitializeMethodTest(string test, int expectedColumn)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo 
{{
  {0}
  public void Foo()
  {{
  }}
}}
";
			string givenText = string.Format(baseline, test);

			DiagnosticResult expected = new DiagnosticResult
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.AvoidTestInitializeMethod),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 5, expectedColumn)
				}
			};

			VerifyCSharpDiagnostic(givenText, expected);
		}


		[DataTestMethod]
		[DataRow(@"[TestCleanup]", 4)]
		public void AvoidTestCleanupMethodTest(string test, int expectedColumn)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo 
{{
  {0}
  public void Foo()
  {{
  }}
}}
";
			string givenText = string.Format(baseline, test);

			DiagnosticResult expected = new DiagnosticResult
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.AvoidTestCleanupMethod),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 5, expectedColumn)
				}
			};

			VerifyCSharpDiagnostic(givenText, expected);
		}


		[DataTestMethod]
		[DataRow(@"[ClassInitialize]", 4)]
		public void AvoidClassInitializeMethodTest(string test, int expectedColumn)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo 
{{
  {0}
  public void Foo()
  {{
  }}
}}
";
			string givenText = string.Format(baseline, test);

			DiagnosticResult expected = new DiagnosticResult
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.AvoidClassInitializeMethod),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 5, expectedColumn)
				}
			};

			VerifyCSharpDiagnostic(givenText, expected);
		}


		[DataTestMethod]
		[DataRow(@"[ClassCleanup]", 4)]
		public void AvoidClassCleanupMethodTest(string test, int expectedColumn)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo 
{{
  {0}
  public void Foo()
  {{
  }}
}}
";
			string givenText = string.Format(baseline, test);

			DiagnosticResult expected = new DiagnosticResult
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.AvoidClassCleanupMethod),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 5, expectedColumn)
				}
			};

			VerifyCSharpDiagnostic(givenText, expected);
		}

		[DataTestMethod]
		[DataRow(@"[ClassCleanup]")]
		[DataRow(@"[ClassInitialize]")]
		[DataRow(@"[TestInitialize]")]
		[DataRow(@"[TestCleanup]")]
		public void WhitelistIsApplied(string test)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Foo 
{{
  {0}
  public void AllowedInitializer()
  {{
  }}
}}
";
			string givenText = string.Format(baseline, test);

			VerifyCSharpDiagnostic(givenText);
		}

		[DataTestMethod]
		[DataRow(@"[ClassCleanup]")]
		[DataRow(@"[ClassInitialize]")]
		[DataRow(@"[TestInitialize]")]
		[DataRow(@"[TestCleanup]")]
		public void WhitelistIsAppliedUnresolvable(string test)
		{
			string baseline = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

class Foo 
{{
  {0}
  public void AllowedInitializer(Bar b)
  {{
  }}
}}
";
			string givenText = string.Format(baseline, test);

			VerifyCSharpDiagnostic(givenText);
		}

		[TestMethod]
		public void AvoidSuppressMessageNotRaisedInGeneratedCode()
		{
			string baseline = @"
#pragma checksum ""..\..\BedPosOverlayWindow.xaml"" ""{ ff1816ec - aa5e - 4d10 - 87f7 - 6f4963833460}"" ""B42AD704B6EC2B9E4AC053991400023FA2213654""
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

			using System;
			using System.Diagnostics;
			using System.Windows;
			using System.Windows.Automation;
			using System.Windows.Controls;
			using System.Windows.Controls.Primitives;
			using System.Windows.Data;
			using System.Windows.Documents;
			using System.Windows.Forms.Integration;
			using System.Windows.Ink;
			using System.Windows.Input;
			using System.Windows.Markup;
			using System.Windows.Media;
			using System.Windows.Media.Animation;
			using System.Windows.Media.Effects;
			using System.Windows.Media.Imaging;
			using System.Windows.Media.Media3D;
			using System.Windows.Media.TextFormatting;
			using System.Windows.Navigation;
			using System.Windows.Shapes;
			using System.Windows.Shell;
namespace WpfApp1 {
	
	
	/// <summary>
	/// WpfOverlayWindow
	/// </summary>
	public partial class WpfOverlayWindow : System.Windows.Window, System.Windows.Markup.IComponentConnector, System.Windows.Markup.IStyleConnector {

		#line 4 ""..\..\BedPosOverlayWindow.xaml""
		[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute(""Microsoft.Performance"", ""CA1823:AvoidUnusedPrivateFields"")]

		internal WpfApp1.WpfOverlayWindow bedLocationWindow;
	}
}
			";

			VerifyCSharpDiagnostic(baseline, "BedPosOverlayWindow.g.i");
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new AvoidAttributeAnalyzer();
		}
	}
}