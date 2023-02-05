// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Runtime.InteropServices;
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
	[TestClass]
	public class ServiceContractHasOperationContractAnalyzerTest : DiagnosticVerifier
	{
		#region Non-Public Data Members

		#endregion

		#region Non-Public Properties/Methods

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new ServiceContractHasOperationContractAnalyzer();
		}

		#endregion

		#region Public Interface

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void IgnoresEmptyInterfaceTest()
		{
			const string text = @"
public interface IFoo { }
";

			VerifySuccessfulCompilation(text);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void IgnoresEmptyInterfaceWithAllOperationContractsTest()
		{
			const string text = @"using System.ServiceModel;
[ServiceContract]
public interface IFoo { }
";

			VerifySuccessfulCompilation(text);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void IgnoresEmptyInterfaceWithAllOperationContracts2Test()
		{
			const string text = @"using System.ServiceModel;
[ServiceContract]
public interface IFoo
{
	[OperationContract]
	void Foo();
}
";

			VerifySuccessfulCompilation(text);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void InterfaceWithNoOperationContractsTest()
		{
			const string text = @"using System.ServiceModel;
[ServiceContract()]
public interface IFoo
{
	void Foo();
}
";

			VerifyDiagnostic(text, 
				new DiagnosticResult()
				{
					Id = Helper.ToDiagnosticId(DiagnosticId.ServiceContractsMustHaveOperationContractAttributes),
					Message = new Regex(".*"),
					Severity = DiagnosticSeverity.Error,
					Locations = new[]
					{
						new DiagnosticResultLocation("Test0.cs", 5, null),
					}
				});
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void InterfaceWithNoOperationContracts2Test()
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

			VerifyDiagnostic(text,
				new DiagnosticResult()
				{
					Id = Helper.ToDiagnosticId(DiagnosticId.ServiceContractsMustHaveOperationContractAttributes),
					Message = new Regex(".*"),
					Severity = DiagnosticSeverity.Error,
					Locations = new[]
					{
						new DiagnosticResultLocation("Test0.cs", 5, null),
					}
				});
		}

		#endregion
	}
}
