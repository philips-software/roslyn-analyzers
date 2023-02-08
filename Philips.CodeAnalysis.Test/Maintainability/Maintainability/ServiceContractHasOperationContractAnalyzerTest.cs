// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class ServiceContractHasOperationContractAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new ServiceContractHasOperationContractAnalyzer();
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task IgnoresEmptyInterfaceTestAsync()
		{
			const string text = @"
public interface IFoo { }
";

			await VerifySuccessfulCompilation(text).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task IgnoresEmptyInterfaceWithAllOperationContractsTestAsync()
		{
			const string text = @"using System.ServiceModel;
[ServiceContract]
public interface IFoo { }
";

			await VerifySuccessfulCompilation(text).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task IgnoresEmptyInterfaceWithAllOperationContracts2TestAsync()
		{
			const string text = @"using System.ServiceModel;
[ServiceContract]
public interface IFoo
{
	[OperationContract]
	void Foo();
}
";

			await VerifySuccessfulCompilation(text).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task InterfaceWithNoOperationContractsTestAsync()
		{
			const string text = @"using System.ServiceModel;
[ServiceContract()]
public interface IFoo
{
	void Foo();
}
";

			await VerifyDiagnostic(text).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task InterfaceWithNoOperationContracts2TestAsync()
		{
			const string text = @"using System.ServiceModel;
[ServiceContract()]
public interface IFoo
{
	void Foo();

	[OperationContract]
	void Foo2();
}
";

			await VerifyDiagnostic(text).ConfigureAwait(false);
		}
	}
}
