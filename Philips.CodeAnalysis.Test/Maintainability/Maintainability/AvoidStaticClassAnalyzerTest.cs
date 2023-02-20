// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

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
		public async Task AvoidStaticClassesOnlyConstFieldTestAsync()
		{
			string testClass = $@"
			namespace MyNamespace {{
			public static class TestClass {{
				{CreateField("const", "F1")}
			}}}}";

			await VerifySuccessfulCompilation(testClass).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidStaticClassesViolatingFieldTest()
		{
			string testClass = $@"
			namespace MyNamespace {{
			public static class TestClass {{
				{CreateField("const", "F1")}
				{CreateField("", "ViolatingField")}
			}}}}";

			await Verify(testClass);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidStaticClassesMixFieldTestAsync()
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

			await VerifySuccessfulCompilation(testClass).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidStaticClassesMixViolationTest()
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

			await Verify(testClass);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidStaticClassesRogueMethodTest()
		{
			string testClass = $@"
			namespace MyNamespace {{
			public static class TestClass {{
				{CreateField("const", "F1")}
				{CreateField("const", "F2")}
				{CreateField("const", "F3")}
				public static void Foo();
			}}}}";

			await Verify(testClass);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidStaticClassesTest()
		{
			var file = CreateFunction("static");
			await Verify(file);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidStaticClassesShouldNotWhitelistWhenNamespaceUnmatchedTest()
		{
			var file = CreateFunction("static", "IAmSooooooNotWhitelisted", KnownWhitelistClassClassName);
			await Verify(file);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidStaticClassesShouldWhitelistWildCardClassTestAsync()
		{
			var file = CreateFunction("static", "IAmSooooooNotWhitelisted", KnownWildcardClassName);
			await VerifySuccessfulCompilation(file).ConfigureAwait(false);
			var file2 = CreateFunction("static", "IAmSooooooNotWhitelisted", AnotherKnownWildcardClassName);
			await VerifySuccessfulCompilation(file2).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidStaticClassesShouldWhitelistExtensionClasses()
		{
			var noDiagnostic = CreateFunction("static", isExtension: true, hasNonExtensionMethods: false);
			await VerifySuccessfulCompilation(noDiagnostic).ConfigureAwait(false);
			var methodHavingDiagnostic = CreateFunction("static", isExtension: true);
			await Verify(methodHavingDiagnostic);
			await VerifyFix(methodHavingDiagnostic, methodHavingDiagnostic).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidNoStaticClassesTestAsync()
		{
			var file = CreateFunction("");
			await VerifySuccessfulCompilation(file).ConfigureAwait(false);
		}


		private async Task Verify(string file)
		{
			await VerifyDiagnostic(file, DiagnosticId.AvoidStaticClasses, line: 3, column: -1).ConfigureAwait(false);
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
		public async Task AvoidStaticClassesShouldWhitelistTestAsync()
		{
			AllowedSymbols allowedSymbols = new(null);
			allowedSymbols.RegisterLine($"{KnownWhitelistClassNamespace}.{KnownWhitelistClassClassName}");
			_ = _mock.Setup(c => c.CreateCompilationAnalyzer(It.IsAny<AllowedSymbols>(), It.IsAny<bool>())).Returns(new AvoidStaticClassesCompilationAnalyzer(allowedSymbols, false));
			var file = CreateFunction("static", KnownWhitelistClassNamespace, KnownWhitelistClassClassName);
			await VerifySuccessfulCompilation(file).ConfigureAwait(false);
		}
	}
}
