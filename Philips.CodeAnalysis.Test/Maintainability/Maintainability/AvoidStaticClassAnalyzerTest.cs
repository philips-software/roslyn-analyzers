﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidStaticClassAnalyzerTest2 : AvoidStaticClassAnalyzerTest
	{
		private readonly Mock<AvoidStaticClassesAnalyzer> _mock = new() { CallBase = true };

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return _mock.Object;
		}

		[TestMethod]
		public void AvoidStaticClassesShouldWhitelistTest()
		{
			HashSet<string> exceptions = new()
			{
				KnownWhitelistClassNamespace + "." + KnownWhitelistClassClassName
			};
			_mock.Setup(c => c.CreateCompilationAnalyzer(It.IsAny<HashSet<string>>(), It.IsAny<bool>())).Returns(new AvoidStaticClassesCompilationAnalyzer(exceptions, false));
			var file = CreateFunction("static", KnownWhitelistClassNamespace, KnownWhitelistClassClassName);
			VerifySuccessfulCompilation(file);
		}
	}

	/// <summary>
	/// 
	/// </summary>
	[TestClass]
	public class AvoidStaticClassAnalyzerTest : DiagnosticVerifier
	{
		public const string KnownWhitelistClassNamespace = "Philips.Monitoring.Common";
		public const string KnownWhitelistClassClassName = "SerializationHelper";
		public const string KnownWildcardClassName = "AssemblyInitialize";
		public const string AnotherKnownWildcardClassName = "Program";

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidStaticClassesAnalyzer();
		}

		private string CreateField(string modifiers, string name)
		{
			return $@"
				public {modifiers} string {name} = ""{name}"";
";
		}

		protected string CreateFunction(string staticModifier, string nameSpace = "Sweet", string className = "Caroline", bool isExtension = false, bool hasNonExtensionMethods = true)
		{
			string extensionMethod = isExtension ? $@"public {staticModifier} IServiceCollection BaBaBummmm(this IServiceCollection services)
					{{
						return services;
					}}" : string.Empty;

			string method = hasNonExtensionMethods ? $@"public {staticModifier} IServiceCollection BaBaBa(IServiceCollection services)
					{{
						return services;
					}}" : string.Empty;

			return $@"
			namespace {nameSpace} {{
				{staticModifier} class {className}
				{{
					{extensionMethod}
					{method}

					private {staticModifier} void SomethingAboutTheGoodTimes()
					{{
						return;
					}}
				}}
			}}";
		}

		[TestMethod]
		public void AvoidStaticClassesOnlyConstFieldTest()
		{
			string testClass = $@"
			namespace MyNamespace {{
			public static class TestClass {{
				{ CreateField("const", "F1")}
			}}}}";

			VerifySuccessfulCompilation(testClass);
		}

		[TestMethod]
		public void AvoidStaticClassesViolatingFieldTest()
		{
			string testClass = $@"
			namespace MyNamespace {{
			public static class TestClass {{
				{CreateField("const", "F1")}
				{CreateField("", "ViolatingField")}
			}}}}";

			VerifyDiagnostic(testClass);
		}

		[TestMethod]
		public void AvoidStaticClassesMixFieldTest()
		{
			string testClass = $@"
			namespace MyNamespace {{
			public static class TestClass {{
				{CreateField("const", "F1")}
				{CreateField("static readonly", "F2")}
				{CreateField("const", "F3")}
				{CreateField("static readonly", "F4")}
				{CreateField("const", "F5")}
			}}}}";

			VerifySuccessfulCompilation(testClass);
		}

		[TestMethod]
		public void AvoidStaticClassesMixViolationTest()
		{
			string testClass = $@"
			namespace MyNamespace {{
			public static class TestClass {{
				{CreateField("const", "F1")}
				{CreateField("static readonly", "F2")}
				{CreateField("const", "F3")}
				{CreateField("static", "ViolatingField")}
				{CreateField("const", "F5")}
			}}}}";

			VerifyDiagnostic(testClass);
		}

		[TestMethod]
		public void AvoidStaticClassesRogueMethodTest()
		{
			string testClass = $@"
			namespace MyNamespace {{
			public static class TestClass {{
				{CreateField("const", "F1")}
				{CreateField("const", "F2")}
				{CreateField("const", "F3")}
				public static void Foo();
			}}}}";

			VerifyDiagnostic(testClass);
		}


		[TestMethod]
		public void AvoidStaticClassesTest()
		{
			var file = CreateFunction("static");
			VerifyDiagnostic(file);
		}


		[TestMethod]
		public void AvoidStaticClassesShouldNotWhitelistWhenNamespaceUnmatchedTest()
		{
			var file = CreateFunction("static", "IAmSooooooNotWhitelisted", KnownWhitelistClassClassName);
			VerifyDiagnostic(file);
		}

		[TestMethod]
		public void AvoidStaticClassesShouldWhitelistWildCardClassTest()
		{
			var file = CreateFunction("static", "IAmSooooooNotWhitelisted", KnownWildcardClassName);
			VerifySuccessfulCompilation(file);
			var file2 = CreateFunction("static", "IAmSooooooNotWhitelisted", AnotherKnownWildcardClassName);
			VerifySuccessfulCompilation(file2);
		}

		[TestMethod]
		public void AvoidStaticClassesShouldWhitelistExtensionClasses()
		{
			var noDiagnostic = CreateFunction("static", isExtension: true, hasNonExtensionMethods: false);
			VerifySuccessfulCompilation(noDiagnostic);
			var methodHavingDiagnostic = CreateFunction("static", isExtension: true);
			VerifyDiagnostic(methodHavingDiagnostic);
		}

		[TestMethod]
		public void AvoidNoStaticClassesTest()
		{
			var file = CreateFunction("");
			VerifySuccessfulCompilation(file);
		}


		private void VerifyDiagnostic(string file)
		{
			VerifyDiagnostic(file, new DiagnosticResult()
			{
				Id = AvoidStaticClassesAnalyzer.Rule.Id,
				Message = new Regex(".+"),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 3, -1),
				}
			});
		}
	}
}
