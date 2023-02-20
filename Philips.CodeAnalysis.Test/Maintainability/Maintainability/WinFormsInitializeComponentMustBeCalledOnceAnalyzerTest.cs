// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

#region Header
// © 2019 Koninklijke Philips N.V.  All rights reserved.
// Reproduction or transmission in whole or in part, in any form or by any means, 
// electronic, mechanical or otherwise, is prohibited without the prior  written consent of 
// the owner.
// Author:      George.Thissell
// Date:        February 2019
#endregion

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class WinFormsInitializeComponentMustBeCalledOnceAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Properties/Methods

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new WinFormsInitializeComponentMustBeCalledOnceAnalyzer();
		}

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

		private async Task VerifyDiagnosticOnFirstAsync(string file)
		{
			await VerifyDiagnostic(file, DiagnosticId.InitializeComponentMustBeCalledOnce, line: 11, column: 16).ConfigureAwait(false);
		}

		private async Task VerifyDiagnosticOnSecondAsync(string file)
		{
			await VerifyDiagnostic(file, DiagnosticId.InitializeComponentMustBeCalledOnce, line: 15, column: 3).ConfigureAwait(false);
		}

		private async Task VerifyDiagnosticeOnFirstAndSecondAsync(string file)
		{
			await VerifyDiagnostic(file, 2).ConfigureAwait(false);
		}

		private async Task VerifyDiagnosticOnClassAsync(string file)
		{
			await VerifyDiagnostic(file, DiagnosticId.InitializeComponentMustBeCalledOnce, line: 9, column: 22).ConfigureAwait(false);
		}

		#endregion

		#region Public Interface

		[DataTestMethod]
		[DataRow(@"InitializeComponent();", @"InitializeComponent();", true, false)]
		[DataRow(@"", @"", true, true)]
		[DataRow(@"InitializeComponent();", @"", false, true)]
		[DataRow(@"", @"InitializeComponent();", false, false)]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WinFormsInitialComponentMustBeCalledOnceAnalyzersAsync(string param1, string param2, bool shouldGenerateDiagnosticOnFirst, bool shouldGenerateDiagnosticOnSecond)
		{
			string code = CreateCode(param1, param2);

			if (shouldGenerateDiagnosticOnFirst && !shouldGenerateDiagnosticOnSecond)
			{
				await VerifyDiagnosticOnFirstAsync(code).ConfigureAwait(false);
			}
			else if (!shouldGenerateDiagnosticOnFirst & shouldGenerateDiagnosticOnSecond)
			{
				await VerifyDiagnosticOnSecondAsync(code).ConfigureAwait(false);
			}
			else if (shouldGenerateDiagnosticOnFirst && shouldGenerateDiagnosticOnSecond)
			{
				await VerifyDiagnosticeOnFirstAndSecondAsync(code).ConfigureAwait(false);
			}
			else
			{
				await VerifySuccessfulCompilation(code).ConfigureAwait(false);
			}
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WinFormsInitialComponentMustBeCalledOnceAnalyzerWithOutConstructorsAsync()
		{
			string code = CreateCodeWithOutConstructors();
			await VerifyDiagnosticOnClassAsync(code).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WinFormsInitialComponentMustBeCalledOnceAnalyzerWithDisjointConstructorsAsync()
		{
			string code = CreateCodeWithDisjointConstructors();
			await VerifySuccessfulCompilation(code).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WinFormsInitialComponentMustBeCalledOnceAnalyzerStaticClassAsync()
		{
			string code = CreateCodeWithStaticConstructor();
			await VerifyDiagnosticOnClassAsync(code).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task WinFormsInitialComponentMustBeCalledOnceAnalyzerIgnoreDesignerFileAsync()
		{
			string code = CreateCode(@"", @"");
			await VerifySuccessfulCompilation(code, @"Test.Designer").ConfigureAwait(false);
		}

		#endregion

	}
}
