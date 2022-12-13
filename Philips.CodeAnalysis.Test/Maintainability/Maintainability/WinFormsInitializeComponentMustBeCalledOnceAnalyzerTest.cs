#region Header
// © 2019 Koninklijke Philips N.V.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior  written consent of 
// the owner.
// Author:      George.Thissell
// Date:        February 2019
#endregion

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	/// <summary>
	/// InitializeComponentMustBeCalledOnceAnalyzerTest
	/// </summary>
	[TestClass]
	public class WinFormsInitializeComponentMustBeCalledOnceAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Properties/Methods

		/// <summary>
		/// GetCSharpDiagnosticAnalyzer
		/// </summary>
		/// <returns></returns>
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new WinFormsInitializeComponentMustBeCalledOnceAnalyzer();
		}

		/// <summary>
		/// CreateCode
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="param2"></param>
		/// <returns></returns>
		private string CreateCode(string param1, string param2)
		{
			string code = @"
namespace System.Windows.Forms
{{
class ContainerControl {{ }}
}}

using System.Windows.Forms;

public partial class Foo : ContainerControl
{{
  public Foo() : this (7)
  {{
	{0}
  }}
  public Foo(int i)
  {{
	{1}
  }}
}}
class ContainerControl 
{{
  public ContainerControl()
  {{
	InitializeComponent();
  }}
}}
";
			return string.Format(code, param1, param2);
		}

		/// <summary>
		/// CreateCodeWithOutConstructors
		/// </summary>
		/// <returns></returns>
		private string CreateCodeWithOutConstructors()
		{
			return @"
namespace System.Windows.Forms
{{
class ContainerControl {{ }}
}}

using System.Windows.Forms;

public partial class Foo : ContainerControl
{{
}}
class ContainerControl 
{{
  public ContainerControl()
  {{
	InitializeComponent();
  }}
}}
";
		}

		/// <summary>
		/// CreateCodeWithOutConstructors
		/// </summary>
		/// <returns></returns>
		private string CreateCodeWithDisjointConstructors()
		{
			return @"
namespace System.Windows.Forms
{{
class ContainerControl {{ }}
}}

using System.Windows.Forms;

public partial class Foo : ContainerControl
{{
  public Foo() : this (7)
  {{
  }}
  public Foo(int i)
  {{
	InitializeComponent();
  }}
  public Foo(float f)
  {{
	InitializeComponent();
  }}
}}
class ContainerControl 
{{
  public ContainerControl()
  {{
	InitializeComponent();
  }}
}}
";
		}

		/// <summary>
		/// CreateCodeWithStaticConstructor
		/// </summary>
		/// <returns></returns>
		private string CreateCodeWithStaticConstructor()
		{
			return @"
namespace System.Windows.Forms
{{
class ContainerControl {{ }}
}}

using System.Windows.Forms;

public partial class Foo : ContainerControl
{{
  static Foo()
  {{
	InitializeComponent();
  }}
}}
class ContainerControl 
{{
  public ContainerControl()
  {{
	InitializeComponent();
  }}
}}
";
		}



		private DiagnosticResult GetDiagnosticResult(int row, int col)
		{
			return new DiagnosticResult()
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.InitializeComponentMustBeCalledOnce),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", row,col),
				}
			};
		}

		/// <summary>
		/// VerifyNoDiagnostic
		/// </summary>
		/// <param name="file"></param>
		private void VerifyNoDiagnostic(string file)
		{
			VerifyCSharpDiagnostic(file);
		}

		/// <summary>
		/// VerifyDiagnosticOnFirst
		/// </summary>
		/// <param name="file"></param>
		private void VerifyDiagnosticOnFirst(string file)
		{
			DiagnosticResult diagnosticResult = GetDiagnosticResult(11, 16);
			DiagnosticResult[] expected = new DiagnosticResult[] { diagnosticResult };
			VerifyCSharpDiagnostic(file, expected);
		}

		/// <summary>
		/// VerifyDiagnosticOnSecond
		/// </summary>
		/// <param name="file"></param>
		private void VerifyDiagnosticOnSecond(string file)
		{
			DiagnosticResult diagnosticResult = GetDiagnosticResult(15, 3);
			DiagnosticResult[] expected = new DiagnosticResult[] { diagnosticResult };
			VerifyCSharpDiagnostic(file, expected);
		}

		/// <summary>
		/// VerifyDiagnosticeOnFirstAndSecond
		/// </summary>
		/// <param name="file"></param>
		private void VerifyDiagnosticeOnFirstAndSecond(string file)
		{
			DiagnosticResult diagnosticResult1 = GetDiagnosticResult(11, 16);
			DiagnosticResult diagnosticResult2 = GetDiagnosticResult(15, 3);
			DiagnosticResult[] expected = new DiagnosticResult[] { diagnosticResult1, diagnosticResult2 };
			VerifyCSharpDiagnostic(file, expected);
		}

		/// <summary>
		/// VerifyDoubleDiagnostic
		/// </summary>
		/// <param name="file"></param>
		private void VerifyDiagnosticOnClass(string file)
		{
			DiagnosticResult diagnosticResult = GetDiagnosticResult(9, 22);
			DiagnosticResult[] expected = new DiagnosticResult[] { diagnosticResult };
			VerifyCSharpDiagnostic(file, expected);
		}

		#endregion

		#region Public Interface

		/// <summary>
		/// WinFormsInitialComponentMustBeCalledOnceAnalyzers
		/// </summary>
		/// <param name="param"></param>
		[DataTestMethod]
		[DataRow(@"InitializeComponent();", @"InitializeComponent();", true, false)]
		[DataRow(@"", @"", true, true)]
		[DataRow(@"InitializeComponent();", @"", false, true)]
		[DataRow(@"", @"InitializeComponent();", false, false)]
		public void WinFormsInitialComponentMustBeCalledOnceAnalyzers(string param1, string param2, bool shouldGenerateDiagnosticOnFirst, bool shouldGenerateDiagnosticOnSecond)
		{
			string code = CreateCode(param1, param2);

			if (shouldGenerateDiagnosticOnFirst && !shouldGenerateDiagnosticOnSecond)
			{
				VerifyDiagnosticOnFirst(code);
			}
			else if (!shouldGenerateDiagnosticOnFirst & shouldGenerateDiagnosticOnSecond)
			{
				VerifyDiagnosticOnSecond(code);
			}
			else if (shouldGenerateDiagnosticOnFirst && shouldGenerateDiagnosticOnSecond)
			{
				VerifyDiagnosticeOnFirstAndSecond(code);
			}
			else
			{
				VerifyNoDiagnostic(code);
			}
		}

		/// <summary>
		/// WinFormsInitialComponentMustBeCalledOnceAnalyzerWithOutConstructors
		/// </summary>
		/// <param name="param"></param>
		[TestMethod]
		public void WinFormsInitialComponentMustBeCalledOnceAnalyzerWithOutConstructors()
		{
			string code = CreateCodeWithOutConstructors();
			VerifyDiagnosticOnClass(code);
		}

		/// <summary>
		/// WinFormsInitialComponentMustBeCalledOnceAnalyzerWithDisjointConstructors
		/// </summary>
		/// <param name="param"></param>
		[TestMethod]
		public void WinFormsInitialComponentMustBeCalledOnceAnalyzerWithDisjointConstructors()
		{
			string code = CreateCodeWithDisjointConstructors();
			VerifyNoDiagnostic(code);
		}

		/// <summary>
		/// WinFormsInitialComponentMustBeCalledOnceAnalyzerStaticClass
		/// </summary>
		/// <param name="param"></param>
		[TestMethod]
		public void WinFormsInitialComponentMustBeCalledOnceAnalyzerStaticClass()
		{
			string code = CreateCodeWithStaticConstructor();
			VerifyDiagnosticOnClass(code);
		}

		/// <summary>
		/// WinFormsInitialComponentMustBeCalledOnceAnalyzerIgnoreDesignerFile
		/// </summary>
		/// <param name="param"></param>
		[TestMethod]
		public void WinFormsInitialComponentMustBeCalledOnceAnalyzerIgnoreDesignerFile()
		{
			string code = CreateCode(@"", @"");
			VerifyCSharpDiagnostic(code, @"Test.Designer");
		}

		#endregion

	}
}
