// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

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
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

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
		/// GetDiagnosticAnalyzer
		/// </summary>
		/// <returns></returns>
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
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
				Id = Helper.ToDiagnosticId(DiagnosticId.InitializeComponentMustBeCalledOnce),
				Message = new Regex(".*"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", row,col),
				}
			};
		}

		/// <summary>
		/// VerifyDiagnosticOnFirst
		/// </summary>
		/// <param name="file"></param>
		private void VerifyDiagnosticOnFirst(string file)
		{
			DiagnosticResult expected = GetDiagnosticResult(11, 16);
			VerifyDiagnostic(file, expected);
		}

		/// <summary>
		/// VerifyDiagnosticOnSecond
		/// </summary>
		/// <param name="file"></param>
		private void VerifyDiagnosticOnSecond(string file)
		{
			DiagnosticResult expected = GetDiagnosticResult(15, 3);
			VerifyDiagnostic(file, expected);
		}

		/// <summary>
		/// VerifyDiagnosticeOnFirstAndSecond
		/// </summary>
		/// <param name="file"></param>
		private void VerifyDiagnosticeOnFirstAndSecond(string file)
		{
			VerifyDiagnostic(file, 2);
		}

		/// <summary>
		/// VerifyDoubleDiagnostic
		/// </summary>
		/// <param name="file"></param>
		private void VerifyDiagnosticOnClass(string file)
		{
			DiagnosticResult expected = GetDiagnosticResult(9, 22);
			VerifyDiagnostic(file, expected);
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
		[TestCategory(TestDefinitions.UnitTests)]
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
				VerifySuccessfulCompilation(code);
			}
		}

		/// <summary>
		/// WinFormsInitialComponentMustBeCalledOnceAnalyzerWithOutConstructors
		/// </summary>
		/// <param name="param"></param>
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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
		[TestCategory(TestDefinitions.UnitTests)]
		public void WinFormsInitialComponentMustBeCalledOnceAnalyzerWithDisjointConstructors()
		{
			string code = CreateCodeWithDisjointConstructors();
			VerifySuccessfulCompilation(code);
		}

		/// <summary>
		/// WinFormsInitialComponentMustBeCalledOnceAnalyzerStaticClass
		/// </summary>
		/// <param name="param"></param>
		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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
		[TestCategory(TestDefinitions.UnitTests)]
		public void WinFormsInitialComponentMustBeCalledOnceAnalyzerIgnoreDesignerFile()
		{
			string code = CreateCode(@"", @"");
			VerifySuccessfulCompilation(code, @"Test.Designer");
		}

		#endregion

	}
}
