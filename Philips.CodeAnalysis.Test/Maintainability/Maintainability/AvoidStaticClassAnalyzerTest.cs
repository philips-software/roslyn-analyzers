﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	/// <summary>
	/// 
	/// </summary>
	[TestClass]
	public class AvoidStaticClassAnalyzerTest : CodeFixVerifier
	{
		public const string KnownWhitelistClassNamespace = "Philips.Monitoring.Common";
		public const string KnownWhitelistClassClassName = "SerializationHelper";
		public const string KnownWildcardClassName = "AssemblyInitialize";
		public const string AnotherKnownWildcardClassName = "Program";
		private const string AllowedStaticTypes = @"AllowedClass
AllowedStruct
AllowedEnumeration";

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidStaticClassesAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new AvoidStaticClassesCodeFixProvider();
		}

		protected override ImmutableArray<(string name, string content)> GetAdditionalTexts()
		{
			return base.GetAdditionalTexts().Add((AvoidStaticClassesAnalyzer.AllowedFileName, AllowedStaticTypes));
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
		[TestCategory(TestDefinitions.UnitTests)]
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
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidStaticClassesViolatingFieldTest()
		{
			string testClass = $@"
			namespace MyNamespace {{
			public static class TestClass {{
				{CreateField("const", "F1")}
				{CreateField("", "ViolatingField")}
			}}}}";

			Verify(testClass);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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
		[TestCategory(TestDefinitions.UnitTests)]
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

			Verify(testClass);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
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

			Verify(testClass);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidStaticClassesTest()
		{
			var file = CreateFunction("static");
			Verify(file);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidStaticClassesShouldNotWhitelistWhenNamespaceUnmatchedTest()
		{
			var file = CreateFunction("static", "IAmSooooooNotWhitelisted", KnownWhitelistClassClassName);
			Verify(file);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidStaticClassesShouldWhitelistWildCardClassTest()
		{
			var file = CreateFunction("static", "IAmSooooooNotWhitelisted", KnownWildcardClassName);
			VerifySuccessfulCompilation(file);
			var file2 = CreateFunction("static", "IAmSooooooNotWhitelisted", AnotherKnownWildcardClassName);
			VerifySuccessfulCompilation(file2);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidStaticClassesShouldWhitelistExtensionClasses()
		{
			var noDiagnostic = CreateFunction("static", isExtension: true, hasNonExtensionMethods: false);
			VerifySuccessfulCompilation(noDiagnostic);
			var methodHavingDiagnostic = CreateFunction("static", isExtension: true);
			Verify(methodHavingDiagnostic);
			await VerifyFix(methodHavingDiagnostic, methodHavingDiagnostic).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidNoStaticClassesTest()
		{
			var file = CreateFunction("");
			VerifySuccessfulCompilation(file);
		}


		private void Verify(string file)
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

	[TestClass]
	public class AvoidStaticClassAnalyzerTest2 : AvoidStaticClassAnalyzerTest
	{
		private readonly Mock<AvoidStaticClassesAnalyzer> _mock = new() { CallBase = true };

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return _mock.Object;
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidStaticClassesShouldWhitelistTest()
		{
			AllowedSymbols allowedSymbols = new(null);
			allowedSymbols.RegisterLine($"{KnownWhitelistClassNamespace}.{KnownWhitelistClassClassName}");
			_mock.Setup(c => c.CreateCompilationAnalyzer(It.IsAny<AllowedSymbols>(), It.IsAny<bool>())).Returns(new AvoidStaticClassesCompilationAnalyzer(allowedSymbols, false));
			var file = CreateFunction("static", KnownWhitelistClassNamespace, KnownWhitelistClassClassName);
			VerifySuccessfulCompilation(file);
		}
	}
}
