﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

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

		protected override MetadataReference[] GetMetadataReferences()
		{
			//JPM yikes.  For some reason the nuget package doesn't work in the semantic analyzer, so we directly reference the full framework DLL
			return new[] { MetadataReference.CreateFromFile(@"C:\WINDOWS\Microsoft.Net\assembly\GAC_MSIL\System.ServiceModel\v4.0_4.0.0.0__b77a5c561934e089\System.ServiceModel.dll") };
		}

		private void VerifyDiagnosticOnWindows(string source, params DiagnosticResult[] expected)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				VerifyDiagnostic(source, null, expected);
			}
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

			VerifyDiagnosticOnWindows(text);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void IgnoresEmptyInterfaceWithAllOperationContractsTest()
		{
			const string text = @"using System.ServiceModel;
[ServiceContract]
public interface IFoo { }
";

			VerifyDiagnosticOnWindows(text);
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

			VerifyDiagnosticOnWindows(text);
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

			VerifyDiagnosticOnWindows(text, new[]
			{
				new DiagnosticResult()
				{
					Id = Helper.ToDiagnosticId(DiagnosticIds.ServiceContractsMustHaveOperationContractAttributes),
					Message = new Regex(".*"),
					Severity = DiagnosticSeverity.Error,
					Locations = new[]
					{
						new DiagnosticResultLocation("Test0.cs", 5, null),
					}
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

			VerifyDiagnosticOnWindows(text, new[]
			{
				new DiagnosticResult()
				{
					Id = Helper.ToDiagnosticId(DiagnosticIds.ServiceContractsMustHaveOperationContractAttributes),
					Message = new Regex(".*"),
					Severity = DiagnosticSeverity.Error,
					Locations = new[]
					{
						new DiagnosticResultLocation("Test0.cs", 5, null),
					}
				}
			});
		}

		#endregion
	}
}
