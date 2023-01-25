﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.Test.Maintainability.RuntimeFailure
{
	[TestClass]
	public class AvoidAssemblyVersionChangeAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new TestableAvoidAssemblyVersionChangeAnalyzer(CorrectReturnedVersion);
		}

		private const string TestCode = @"
class Foo 
{
}
";

		private const string ConfiguredVersion = "1.2.3.4";
		private const string CorrectReturnedVersion = ConfiguredVersion;

		protected override Dictionary<string, string> GetAdditionalAnalyzerConfigOptions()
		{
			Dictionary<string, string> options = new()
			{
				{ $@"dotnet_code_quality.{ Helper.ToDiagnosticId(DiagnosticIds.AvoidAssemblyVersionChange) }.assembly_version", ConfiguredVersion }
			};
			return options;
		}

		[TestMethod]
		public void HasExpectedVersionShouldNotTriggerDiagnostics()
		{
			VerifySuccessfulCompilation(TestCode);
		}
	}
}