﻿using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;

namespace Philips.CodeAnalysis.Test
{
	[TestClass]
	public class TestHasTimeoutTest : AssertCodeFixVerifier
	{
		public TestHasTimeoutTest()
		{
			OtherClassSyntax = @"
class TestTimeouts
{ 
	public const int CiAppropriate = 1;
	public const int Integration = 2;
	public const int CiAcceptable = 3;
	public const int Smoke = 4;
}

class TestDefinitions
{
	public const string UnitTests = ""Unit"";
	public const string IntegrationTests = ""Integration"";
	public const string SmokeTests = ""Smoke"";
}";
		}

		protected override Dictionary<string, string> GetAdditionalAnalyzerConfigOptions() => new Dictionary<string, string>()
		{
			{  $"dotnet_code_quality.{TestHasTimeoutAttributeAnalyzer.Rule.Id}.Unit", "TestTimeouts.CiAppropriate,TestTimeouts.CiAcceptable" },
			{  $"dotnet_code_quality.{TestHasTimeoutAttributeAnalyzer.Rule.Id}.Integration", "TestTimeouts.Integration" },
			{  $"dotnet_code_quality.{TestHasTimeoutAttributeAnalyzer.Rule.Id}.Smoke", "TestTimeouts.Smoke" }
		};

		[DataTestMethod]
		[DataRow("[TestMethod]", "[TestMethod]\n    [Timeout(1000)]")]
		[DataRow("[TestMethod, Owner(\"\")]", "[TestMethod, Owner(\"\")]\n    [Timeout(1000)]")]
		[DataRow("[DataTestMethod]", "[DataTestMethod]\n    [Timeout(1000)]")]
		[DataRow("[TestMethod, TestCategory(TestDefinitions.UnitTests)]", "[TestMethod, TestCategory(TestDefinitions.UnitTests)]\n    [Timeout(TestTimeouts.CiAppropriate)]")]

		public void TimeoutAttributeNotPresent(string methodAttributes, string expectedMethodAttributes)
		{
			VerifyChange(string.Empty, string.Empty, methodAttributes, expectedMethodAttributes);
		}

		[DataTestMethod]
		[DataRow("[TestMethod]", "[TestMethod]\n    [Timeout(1000)]")]

		public void TimeoutAttributeNotPresentNoCategory(string methodAttributes, string expectedMethodAttributes)
		{
			VerifyChange(string.Empty, string.Empty, methodAttributes, expectedMethodAttributes);
		}

		[DataTestMethod]
		[DataRow("[TestMethod, TestCategory(\"foo\")]", "[TestMethod, TestCategory(\"foo\")]\n    [Timeout(1000)]")]

		public void TimeoutAttributeNotPresentUnknownCategory(string methodAttributes, string expectedMethodAttributes)
		{
			VerifyChange(string.Empty, string.Empty, methodAttributes, expectedMethodAttributes);
		}

		[DataTestMethod]
		[DataRow("[TestMethod, TestCategory(TestDefinitions.UnitTests), Timeout(TestTimeouts.Integration)]")]
		[DataRow("[TestMethod, TestCategory(TestDefinitions.IntegrationTests), Timeout(TestTimeouts.CiAppropriate)]")]
		[DataRow("[TestMethod, TestCategory(TestDefinitions.SmokeTests), Timeout(TestTimeouts.CiAppropriate)]")]
		public void TimeoutAttributeWrong(string methodAttributes)
		{
			VerifyError(string.Empty, methodAttributes);
		}

		[DataTestMethod]
		[DataRow("[TestMethod, TestCategory(TestDefinitions.UnitTests), Timeout(TestTimeouts.CiAppropriate)]")]
		[DataRow("[TestMethod, TestCategory(TestDefinitions.UnitTests), Timeout(TestTimeouts.CiAcceptable)]")]
		[DataRow("[TestMethod, TestCategory(TestDefinitions.IntegrationTests), Timeout(TestTimeouts.Integration)]")]
		[DataRow("[TestMethod, TestCategory(TestDefinitions.SmokeTests), Timeout(TestTimeouts.Smoke)]")]
		public void TimeoutAttributeCorrect(string methodAttributes)
		{
			VerifyNoChange(string.Empty, methodAttributes);
		}


		[DataTestMethod]
		[DataRow("[TestMethod][Timeout(1)]")]
		[DataRow("[Timeout(1)][TestMethod]")]
		[DataRow("[TestMethod, Timeout(1)]")]
		[DataRow("[TestMethod, Owner(\"\"), Timeout(1)]")]
		[DataRow("[DataTestMethod, Timeout(1)]")]
		[DataRow("[TestMethod, TestCategory(TestDefinitions.UnitTests)]\n [Timeout(TestTimeouts.CiAppropriate)]")]
		public void TimeoutAttributePresent(string methodAttributes)
		{
			VerifyNoChange(methodBody: string.Empty, methodAttributes: methodAttributes);
		}

		[DataTestMethod]
		[DataRow("[TestInitialize]")]
		[DataRow("[TestCleanup]")]
		[DataRow("[AssemblyInitialize]")]
		[DataRow("[DataRow]")]
		public void DoesNotApplyToNonTestMethods(string methodAttributes)
		{
			VerifyNoChange(methodBody: string.Empty, methodAttributes: methodAttributes);
		}

		[TestMethod]
		public void AttributesInMethodsDontCauseCrash()
		{
			const string body = @"
[TestMethod, Timeout(1)]
var foo = 4;
";

			VerifyNoChange(methodBody: body, methodAttributes: "[TestMethod, Timeout(1)]");
		}

		protected override DiagnosticResult GetExpectedDiagnostic(int expectedLineNumberErrorOffset = 0, int expectedColumnErrorOffset = 0)
		{
			return new DiagnosticResult
			{
				Id = Helper.ToDiagnosticId(DiagnosticIds.TestHasTimeoutAttribute),
				Message = new Regex(TestHasTimeoutAttributeAnalyzer.MessageFormat),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", null, null)
				}
			};
		}

		protected override CodeFixProvider GetCSharpCodeFixProvider()
		{
			return new TestHasTimeoutCodeFixProvider();
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new TestHasTimeoutAttributeAnalyzer();
		}
	}
}