﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.Test.Maintainability.RuntimeFailure
{
	[TestClass]
	public class AvoidAssemblyVersionChangeAnalyzerTest3 : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new TestableAvoidAssemblyVersionChangeAnalyzer(AnyVersion);
		}

		private const string TestCode = @"
class Foo 
{
}
";

		private const string AnyVersion = "5.6.3.4";
		private const string InvalidVersion = "1";

		protected override Dictionary<string, string> GetAdditionalAnalyzerConfigOptions()
		{
			Dictionary<string, string> options = new()
			{
				{ $@"dotnet_code_quality.{ Helper.ToDiagnosticId(DiagnosticIds.AvoidAssemblyVersionChange) }.assembly_version", InvalidVersion }
			};
			return options;
		}
		
		[TestMethod]
		public void InvalidVersionShouldTriggerTriggerDiagnostics()
		{
			var diagnostic = DiagnosticResultHelper.Create(DiagnosticIds.AvoidAssemblyVersionChange);
			VerifyDiagnostic(TestCode, diagnostic);
		}
	}
}